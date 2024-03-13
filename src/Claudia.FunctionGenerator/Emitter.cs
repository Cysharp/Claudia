using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace Claudia.FunctionGenerator;

public class Emitter
{
    const string SystemPromptHead = """
In this environment you have access to a set of tools you can use to answer the user's question.

You may call them like this:
<function_calls>
    <invoke>
        <tool_name>$TOOL_NAME</tool_name>
        <parameters>
            <$PARAMETER_NAME>$PARAMETER_VALUE</$PARAMETER_NAME>
            ...
        </parameters>
    </invoke>
</function_calls>
""";

    private SourceProductionContext context;
    private ParseResult[] result;

    public Emitter(SourceProductionContext context, ParseResult[] result)
    {
        this.context = context;
        this.result = result;
    }

    internal void Emit()
    {
        foreach (var item in result.GroupBy(x => x.TypeSymbol, SymbolEqualityComparer.Default))
        {
            var type = item.Key!;

            var tools = new List<XElement>();

            foreach (var method in item)
            {
                var docComment = method.MethodSymbol.GetDocumentationCommentXml();
                var xml = XElement.Parse(docComment);

                var description = ((string)xml.Element("summary")).Trim();

                var parameters = new List<XElement>();
                foreach (var p in xml.Elements("param"))
                {
                    var paramDescription = ((string)p).Trim();

                    // type retrieve from method symbol
                    var name = p.Attribute("name").Value.Trim();
                    var paramType = method.MethodSymbol.Parameters.First(x => x.Name == name).Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

                    parameters.Add(new XElement("parameter",
                        new XElement("name", name),
                        new XElement("type", paramType),
                        new XElement("description", paramDescription)));
                }

                var tool = new XElement("tool_description",
                    new XElement("tool_name", method.MethodSymbol.Name),
                    new XElement("description", description),
                    new XElement("parameters", parameters));

                tools.Add(tool);
            }

            var finalXml = new XElement("tools", tools);





        }









    }
}