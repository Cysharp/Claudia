using Microsoft.CodeAnalysis;
using System.Xml.Linq;

namespace Claudia.FunctionGenerator;

public class Emitter
{
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

            var typeDocComment = type.GetDocumentationCommentXml();

            var name = type.Name;
            var description = ((string)XElement.Parse(typeDocComment).Element("summary")).Trim();


            foreach (var method in item)
            {

                var docComment = method.MethodSymbol.GetDocumentationCommentXml();



                //new XElement("parameter",
                //    new XElement(

            }




            new XElement("tool_description",
                new XElement("tool_name", name),
                new XElement("description", description),
                new XElement("parameters", null));

        }









        throw new NotImplementedException();
    }
}