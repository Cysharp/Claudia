using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

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

                //var (attr, setLogLevel) = GetAttribute(source);
                //var msg = attr.Message;

                //// parse and verify
                //if (!MessageParser.TryParseFormat(attr.Message, out var segments))
                //{
                //    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MessageTemplateParseFailed, (source.TargetNode as MethodDeclarationSyntax)!.Identifier.GetLocation(), method.Name));
                //    continue;
                //}

                //var (parameters, foundLogLevel) = GetMethodParameters(method, setLogLevel);

                //// Set LinkedParameters
                //foreach (var p in parameters.Where(x => x.IsParameter))
                //{
                //    p.LinkedMessageSegment = segments
                //        .Where(x => x.Kind == MessageSegmentKind.NameParameter)
                //        .FirstOrDefault(x => x.NameParameter.Equals(p.Symbol.Name, StringComparison.OrdinalIgnoreCase));
                //}

                //var methodDecl = new LogMethodDeclaration(
                //    Attribute: attr,
                //    TargetMethod: (IMethodSymbol)source.TargetSymbol,
                //    TargetSyntax: (MethodDeclarationSyntax)source.TargetNode,
                //    MessageSegments: segments,
                //    MethodParameters: parameters);

                //if (!Verify(methodDecl, foundLogLevel, targetType, symbol))
                //{
                //    continue;
                //}

                //logMethods.Add(methodDecl);

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
