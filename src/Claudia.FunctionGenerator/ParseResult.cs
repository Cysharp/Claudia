using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Claudia.FunctionGenerator;

public record ParseResult
{
    public required TypeDeclarationSyntax TypeSyntax { get; set; }
    public required INamedTypeSymbol TypeSymbol { get; set; }
    public required Method[] Methods { get; set; }
}

public record class Method
{
    public required MethodDeclarationSyntax Syntax { get; set; }
    public required IMethodSymbol Symbol { get; set; }

    public string Name => Symbol.Name;
}
