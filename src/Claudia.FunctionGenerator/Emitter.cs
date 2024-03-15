using Microsoft.CodeAnalysis;
using System.Text;
using System.Xml.Linq;

namespace Claudia.FunctionGenerator;

public class Emitter
{
    SourceProductionContext context;
    ParseResult[] result;
    StringBuilder sb;

    public Emitter(SourceProductionContext context, ParseResult[] result)
    {
        this.context = context;
        this.result = result;
        this.sb = new StringBuilder(1024);
    }

    internal void Emit()
    {
        // generate per class
        foreach (var parseResult in result)
        {
            sb.Clear();

            var keyword = parseResult.TypeSyntax.Keyword.ToString();
            var typeName = parseResult.TypeSyntax.Identifier.ToString();
            var staticKey = parseResult.TypeSyntax.Modifiers.Any(x => x.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword)) ? "static " : "";

            sb.AppendLine($"{staticKey}partial {keyword} {typeName}");
            sb.AppendLine("{");
            sb.AppendLine("");

            EmitCore(parseResult);

            sb.AppendLine("}");

            AddSource(context, parseResult.TypeSymbol, sb.ToString());
        }
    }

    void EmitCore(ParseResult parseResult)
    {
        var toolsAll = string.Join(Environment.NewLine, parseResult.Methods.Select(x => "{" + x.Name + "}"));

        sb.AppendLine($$"""
    public const string SystemPrompt = @$"
In this environment you have access to a set of tools you can use to answer the user's question. If your solution involves the use of multiple tools, please include multiple <invoke>s within a single <function_calls> tag. Each step-by-step answer or tag is not required. Only a single <function_calls> tag should be returned at the beginning.

You may call them like this:
<function_calls>
    <invoke>
        <tool_name>$TOOL_NAME</tool_name>
        <parameters>
            <$PARAMETER_NAME>$PARAMETER_VALUE</$PARAMETER_NAME>
            ...
        </parameters>
    </invoke>
    <invoke>
        <tool_name>$TOOL_NAME</tool_name>
        <parameters>
            <$PARAMETER_NAME>$PARAMETER_VALUE</$PARAMETER_NAME>
            ...
        </parameters>
    </invoke>
    ...
</function_calls>

Here are the tools available:

{PromptXml.ToolsAll}

Again, including multiple <function_calls> tags in the reply is prohibited.
";

    public static class PromptXml
    {
        public const string ToolsAll = @$"
<tools>
{{toolsAll}}
</tools>
";
""");

        foreach (var method in parseResult.Methods)
        {
            EmitToolDescription(method);
        }

        sb.AppendLine("    }"); // close PromptXml

        sb.AppendLine();
        EmitInvoke(parseResult);
    }

    void EmitToolDescription(Method method)
    {
        var docComment = method.Syntax.GetDocumentationCommentTriviaSyntax()!;

        var description = docComment.GetSummary().Replace("\"", "'");

        var parameters = new List<XElement>();
        foreach (var p in docComment.GetParams())
        {
            var paramDescription = p.Description.Replace("\"", "'");

            // type retrieve from method symbol
            var name = p.Name;
            var paramType = method.Symbol.Parameters.First(x => x.Name == name).Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            parameters.Add(new XElement("parameter",
                new XElement("name", name),
                new XElement("type", paramType),
                new XElement("description", paramDescription)));
        }

        var tool = new XElement("tool_description",
            new XElement("tool_name", method.Name),
            new XElement("description", description),
            new XElement("parameters", parameters));

        sb.AppendLine($$"""
        public const string {{method.Name}} = @"
{{tool.ToString()}}
";
""");
    }

    void EmitInvoke(ParseResult parseResult)
    {
        var parameterParseString = new StringBuilder();
        foreach (var method in parseResult.Methods)
        {
            var returnType = method.Symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isTask = (returnType.StartsWith("global::System.Threading.Tasks.Task") || returnType.StartsWith("global::System.Threading.Tasks.ValueTask"));

            parameterParseString.AppendLine($"                case \"{method.Name}\":");
            parameterParseString.AppendLine("                    {");
            parameterParseString.AppendLine("                        var parameters = item.Element(\"parameters\")!;");
            parameterParseString.AppendLine();
            var i = 0;
            var parameterNames = new StringBuilder();
            foreach (var p in method.Symbol.Parameters)
            {
                parameterNames.Append((i != 0) ? $", _{i}" : $"_{i}");
                parameterParseString.AppendLine($"                        var _{i++} = ({p.Type.ToDisplayString()})parameters.Element(\"{p.Name}\")!;");
            }
            parameterParseString.AppendLine();
            if (isTask)
            {
                parameterParseString.AppendLine($"                        BuildResult(sb, \"{method.Name}\", await {method.Name}({parameterNames}).ConfigureAwait(false));");
            }
            else
            {
                parameterParseString.AppendLine($"                        BuildResult(sb, \"{method.Name}\", {method.Name}({parameterNames}));");
            }
            parameterParseString.AppendLine("                        break;");
            parameterParseString.AppendLine("                    }");
        }

        var code = $$""""
#pragma warning disable CS1998
    public static async ValueTask<string?> InvokeAsync(MessageResponse message)
    {
        var content = message.Content.FirstOrDefault(x => x.Text != null);
        if (content == null) return null;

        var text = content.Text;
        var tagStart = text .IndexOf("<function_calls>");
        if (tagStart == -1) return null;

        var functionCalls = text.Substring(tagStart) + "</function_calls>";
        var xmlResult = XElement.Parse(functionCalls);

        var sb = new StringBuilder();
        sb.AppendLine(functionCalls);
        sb.AppendLine("<function_results>");

        foreach (var item in xmlResult.Elements("invoke"))
        {
            var name = (string)item.Element("tool_name")!;
            switch (name)
            {
{{parameterParseString}}
                default:
                    break;
            }
        }

        sb.Append("</function_results>"); // final assistant content cannot end with trailing whitespace

        return sb.ToString();

        static void BuildResult<T>(StringBuilder sb, string toolName, T result)
        {
            sb.AppendLine(@$"    <result>
        <tool_name>{toolName}</tool_name>
        <stdout>{result}</stdout>
    </result>");
        }
    }
#pragma warning restore CS1998
"""";

        sb.AppendLine(code);
    }

    static void AddSource(SourceProductionContext context, ISymbol targetSymbol, string code, string fileExtension = ".g.cs")
    {
        var fullType = targetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
          .Replace("global::", "")
          .Replace("<", "_")
          .Replace(">", "_");

        var sb = new StringBuilder();

        sb.AppendLine("""
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108
#pragma warning disable CS0162
#pragma warning disable CS0164
#pragma warning disable CS0219
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631
#pragma warning disable CS8765
#pragma warning disable CS9074
#pragma warning disable CA1050

using Claudia;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
""");

        var ns = targetSymbol.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"namespace {ns} {{");
        }
        sb.AppendLine();

        sb.AppendLine(code);

        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"}}");
        }

        var sourceCode = sb.ToString();
        context.AddSource($"{fullType}{fileExtension}", sourceCode);
    }
}