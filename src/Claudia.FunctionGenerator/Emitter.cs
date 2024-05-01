using Microsoft.CodeAnalysis;
using System.Text;
using System.Xml.Linq;
using static Claudia.FunctionGenerator.Emitter;

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

            // new, beta: https://docs.anthropic.com/claude/docs/tool-use
            EmitToolCallingCore(parseResult);

            sb.AppendLine();

            // legacy
            EmitXmlCallingCore(parseResult);

            sb.AppendLine("}");

            AddSource(context, parseResult.TypeSymbol, sb.ToString());
        }
    }

    void EmitToolCallingCore(ParseResult parseResult)
    {
        EmitTools(parseResult);
        EmitToolInvoke(parseResult);
    }

    void EmitXmlCallingCore(ParseResult parseResult)
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

    void EmitToolInvoke(ParseResult parseResult)
    {
        var methodInvoke = BuildToolLocalMethodInvoke(parseResult.Methods);

        var code = $$""""
#pragma warning disable CS1998
    public static async ValueTask<Contents> InvokeToolAsync(MessageResponse message)
    {
        var result = new Contents();

        foreach (var item in message.Content)
        {
            if (item.Type != ContentTypes.ToolUse) continue;

            switch (item.ToolUseName)
            {
{{methodInvoke}}
                default:
                    break;
            }
        }

        return result;

        static T GetValueOrDefault<T>(Content content, string name, T defaultValue)
        {
            if (content.ToolUseInput!.TryGetValue(name, out var stringValue))
            {
                if (typeof(T) == typeof(Boolean))
                {
                    var v = bool.Parse(stringValue);
                    return Unsafe.As<bool, T>(ref v);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    var v = SByte.Parse(stringValue);
                    return Unsafe.As<SByte, T>(ref v);
                }
                else if (typeof(T) == typeof(Byte))
                {
                    var v = Byte.Parse(stringValue);
                    return Unsafe.As<Byte, T>(ref v);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    var v = Int16.Parse(stringValue);
                    return Unsafe.As<Int16, T>(ref v);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    var v = UInt16.Parse(stringValue);
                    return Unsafe.As<UInt16, T>(ref v);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    var v = Int32.Parse(stringValue);
                    return Unsafe.As<Int32, T>(ref v);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    var v = UInt32.Parse(stringValue);
                    return Unsafe.As<UInt32, T>(ref v);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    var v = Int64.Parse(stringValue);
                    return Unsafe.As<Int64, T>(ref v);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    var v = UInt64.Parse(stringValue);
                    return Unsafe.As<UInt64, T>(ref v);
                }
                else if (typeof(T) == typeof(Decimal))
                {
                    var v = Decimal.Parse(stringValue);
                    return Unsafe.As<Decimal, T>(ref v);
                }
                else if (typeof(T) == typeof(Single))
                {
                    var v = Single.Parse(stringValue);
                    return Unsafe.As<Single, T>(ref v);
                }
                else if (typeof(T) == typeof(Double))
                {
                    var v = Double.Parse(stringValue);
                    return Unsafe.As<Double, T>(ref v);
                }
                else if (typeof(T) == typeof(String))
                {
                    return (T)(object)stringValue;
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    var v = DateTime.Parse(stringValue);
                    return Unsafe.As<DateTime, T>(ref v);
                }
                else if (typeof(T) == typeof(DateTimeOffset))
                {
                    var v = DateTimeOffset.Parse(stringValue);
                    return Unsafe.As<DateTimeOffset, T>(ref v);
                }
                else if (typeof(T) == typeof(Guid))
                {
                    var v = Guid.Parse(stringValue);
                    return Unsafe.As<Guid, T>(ref v);
                }
                else if (typeof(T) == typeof(TimeSpan))
                {
                    var v = TimeSpan.Parse(stringValue);
                    return Unsafe.As<TimeSpan, T>(ref v);
                }
                else
                {
                    if (typeof(T).IsEnum)
                    {
                        return (T)Enum.Parse(typeof(T), stringValue);
                    }
                    throw new NotSupportedException();
                }
            }
            else
            {
                return defaultValue;
            }
        }
    }
#pragma warning restore CS1998
"""";

        sb.AppendLine(code);
    }

    string BuildToolLocalMethodInvoke(Method[] methods)
    {
        var sb = new StringBuilder();

        foreach (var method in methods)
        {
            var returnType = method.Symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isTask = (returnType.StartsWith("global::System.Threading.Tasks.Task") || returnType.StartsWith("global::System.Threading.Tasks.ValueTask"));

            var i = 0;
            var parameterParseString = new StringBuilder();
            var parameterNames = new StringBuilder();
            foreach (var p in method.Symbol.Parameters)
            {
                var defaultValue = "default!";
                if (p.HasExplicitDefaultValue && p.ExplicitDefaultValue != null)
                {
                    if (p.Type.TypeKind == TypeKind.Enum)
                    {
                        defaultValue = $"({p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){p.ExplicitDefaultValue}";
                    }
                    else
                    {
                        defaultValue = p.ExplicitDefaultValue.ToString();
                    }
                }
                var parameterType = p.Type.ToDisplayString();
                parameterNames.Append((i != 0) ? $", _{i}" : $"_{i}");
                parameterParseString.AppendLine($"                    var _{i} = GetValueOrDefault<{parameterType}>(item, \"{p.Name}\", {defaultValue});");
                i++;
            }

            var methodCall = $"{method.Name}({parameterNames})";
            if (isTask)
            {
                methodCall = $"(await {methodCall})";
            }

            sb.AppendLine($$"""
                case "{{method.Name}}":
                {
{{parameterParseString}}
                    string? _callResult;
                    bool? _isError = null;
                    try
                    {
                        _callResult = {{methodCall}}.ToString();
                    }
                    catch (Exception ex)
                    {
                        _callResult = ex.Message;
                        _isError = true;
                    }

                    result.Add(new Content
                    {
                        Type = ContentTypes.ToolResult,
                        ToolResultId = item.ToolUseId,
                        ToolResultContent = _callResult,
                        ToolResultIsError = _isError
                    });

                    break;
                }
""");
        }


        return sb.ToString();
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
                if (p.Type.TypeKind == TypeKind.Enum)
                {
                    parameterParseString.AppendLine($"                        var _{i++} = Enum.Parse<{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>((string)parameters.Element(\"{p.Name}\")!);");
                }
                else
                {
                    parameterParseString.AppendLine($"                        var _{i++} = ({p.Type.ToDisplayString()})parameters.Element(\"{p.Name}\")!;");
                }
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

    void EmitTools(ParseResult parseResult)
    {
        var allTools = string.Join(", ", parseResult.Methods.Select(x => $"Tools.{x.Name}"));
        sb.AppendLine($$"""
    public static readonly Tool[] AllTools = new[] { {{allTools}} };

    public static class Tools
    {
""");

        // Emit Tool
        foreach (var method in parseResult.Methods)
        {
            var docComment = method.Syntax.GetDocumentationCommentTriviaSyntax()!;
            var description = RemoveStringNewLine(docComment.GetSummary().Replace("\"", "'"));

            // property
            var inputSchema = new StringBuilder();
            if (method.Symbol.Parameters.Length != 0)
            {
                var propBuilder = new StringBuilder();
                var paramRequired = new List<string>();
                foreach (var p in docComment.GetParams())
                {
                    var paramDescription = RemoveStringNewLine(p.Description.Replace("\"", "'"));

                    // type retrieve from method symbol
                    var name = p.Name;
                    var pSymbol = method.Symbol.Parameters.First(x => x.Name == name);
                    var paramType = "string";

                    var enumMembers = "null";
                    if (pSymbol.Type.TypeKind == TypeKind.Enum)
                    {
                        enumMembers = string.Join(", ", pSymbol.Type.GetMembers().Where(x => x.Name != ".ctor").Select(x => "\"" + x.Name + "\""));
                        enumMembers = $"new [] {{ {enumMembers} }}";
                    }

                    // mapping jsonschema paramtype https://json-schema.org/understanding-json-schema/reference/type
                    switch (pSymbol.Type.SpecialType)
                    {
                        case SpecialType.System_Boolean:
                            paramType = "boolean";
                            break;
                        case SpecialType.System_SByte:
                        case SpecialType.System_Byte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Decimal:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                            paramType = "number";
                            break;
                        default:
                            break;
                    }

                    propBuilder.AppendLine($$"""
                    {
                        "{{name}}", new ToolProperty()
                        {
                            Type = "{{paramType}}",
                            Description = "{{paramDescription}}",
                            Enum = {{enumMembers}}
                        }
                    },
""");

                    if (!pSymbol.HasExplicitDefaultValue)
                    {
                        paramRequired.Add("\"" + name + "\"");
                    }
                }
                var required = string.Join(", ", paramRequired);
                if (required.Length != 0)
                {
                    required = "Required = new [] { " + required + " }";
                }

                inputSchema.AppendLine($$"""
            InputSchema = new InputSchema
            {
                Type = "object",
                Properties = new System.Collections.Generic.Dictionary<string, ToolProperty>
                {
{{propBuilder}}
                },
                {{required}}
            }
""");
            }
            else
            {
                inputSchema.AppendLine($$"""
            InputSchema = new InputSchema
            {
                Type = "object"
            }
""");
            }

            sb.AppendLine($$"""
        public static readonly Tool {{method.Name}} = new Tool
        {
            Name = "{{method.Name}}",
            Description = "{{description}}",
{{inputSchema}}
        };

""");
        }

        sb.AppendLine("    }"); // close Tools
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
using System.Runtime.CompilerServices;
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

    static string RemoveStringNewLine(string str)
    {
        var sb = new StringBuilder();
        var first = true;
        using var sr = new StringReader(str);
        string line = default!;
        while ((line = sr.ReadLine()) != null)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(" ");
            }
            sb.Append(line.Trim());
        }
        return sb.ToString();
    }
}