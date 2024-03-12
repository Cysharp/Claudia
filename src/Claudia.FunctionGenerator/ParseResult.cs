using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Claudia.FunctionGenerator;

public record ParseResult
{
    public required TypeDeclarationSyntax TypeSyntax { get; set; }
    public required INamedTypeSymbol TypeSymbol { get; set; }
    public required MethodDeclarationSyntax MethodSyntax { get; set; }
    public required IMethodSymbol MethodSymbol { get; set; }
}

