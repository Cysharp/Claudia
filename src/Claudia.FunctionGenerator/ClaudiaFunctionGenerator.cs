using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;

namespace Claudia.FunctionGenerator;

[Generator(LanguageNames.CSharp)]
public partial class ClaudiaFunctionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Claudia.ClaudiaFunctionAttribute",
            static (node, token) => node is MethodDeclarationSyntax,
            static (context, token) => context);

        context.RegisterSourceOutput(source.Collect(), Execute);
    }

    static void Execute(SourceProductionContext context, ImmutableArray<GeneratorAttributeSyntaxContext> sources)
    {
        if (sources.Length == 0) return;

        var result = new Parser(context, sources).Parse();
        if (result.Length != 0)
        {
            var emitter = new Emitter(context, result);
            emitter.Emit();
        }
    }
}
