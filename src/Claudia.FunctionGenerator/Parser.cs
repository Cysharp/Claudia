using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Claudia.FunctionGenerator;

public class Parser
{
    private SourceProductionContext context;
    private ImmutableArray<GeneratorAttributeSyntaxContext> sources;

    public Parser(SourceProductionContext context, ImmutableArray<GeneratorAttributeSyntaxContext> sources)
    {
        this.context = context;
        this.sources = sources;
    }

    internal ParseResult[] Parse()
    {
        var list = new List<ParseResult>();
        var methods = new List<Method>();

        // grouping by type(TypeDeclarationSyntax)
        foreach (var item in sources.GroupBy(x => x.TargetNode.Parent))
        {
            if (item.Key == null) continue;
            var targetType = (TypeDeclarationSyntax)item.Key;
            var symbol = item.First().SemanticModel.GetDeclaredSymbol(targetType);
            if (symbol == null) continue;

            // verify is partial
            if (!IsPartial(targetType))
            {
                // TODO:
                // context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, targetType.Identifier.GetLocation(), symbol.Name));
                continue;
            }

            // nested is not allowed
            if (IsNested(targetType))
            {
                // TODO:
                //context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.NestedNotAllow, targetType.Identifier.GetLocation(), symbol.Name));
                continue;
            }

            // verify is generis type
            if (symbol.TypeParameters.Length > 0)
            {
                // TODO:
                //context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.GenericTypeNotSupported, targetType.Identifier.GetLocation(), symbol.Name));
                continue;
            }

            // TODO:verify documentation somment of summary.


            methods.Clear();
            foreach (var source in item)
            {
                // source.TargetNode
                var method = (IMethodSymbol)source.TargetSymbol;

                // TODO:verify not static
                // TODO:verify documentation somment of summary and parameters.

                methods.Add(new Method { Symbol = method, Syntax = (MethodDeclarationSyntax)source.TargetNode });
            }

            list.Add(new ParseResult
            {
                TypeSyntax = targetType,
                TypeSymbol = symbol,
                Methods = methods.ToArray()
            });
        }

        return list.ToArray();
    }

    static bool IsPartial(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    static bool IsNested(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Parent is TypeDeclarationSyntax;
    }
}
