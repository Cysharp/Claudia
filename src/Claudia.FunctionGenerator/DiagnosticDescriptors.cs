using Microsoft.CodeAnalysis;

namespace Claudia.FunctionGenerator;

internal static class DiagnosticDescriptors
{
    const string Category = "ClaudiaFunctionGenerator";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "CLFG001",
        title: "ClaudiaFunctionAttribute annotated declared type must be partial",
        messageFormat: "The ClaudiaFunctionAttribute annotated declared type '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedNotAllow = new(
        id: "CLFG002",
        title: "ClaudiaFunctionAttribute annotated declared type must not be nested type",
        messageFormat: "The ClaudiaFunctionAttribute annotated declared type '{0}' must be not nested type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericTypeNotSupported = new(
        id: "CLFG003",
        title: "Generic type is not supported",
        messageFormat: "The ClaudiaFunctionAttribute annotated declared type '{0}' is generic, define in generic is not supported",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBeStatic = new(
        id: "CLFG004",
        title: "ClaudiaFunctionAttribute annotated declared type must be static",
        messageFormat: "The ClaudiaFunctionAttribute annotated declared type '{0}' must be static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNeedsDocumentationCommentXml = new(
        id: "CLFG005",
        title: "Method needs documentation comment xml",
        messageFormat: "The '{0}' method has no documentation comment",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNeedsSummary = new(
        id: "CLFG006",
        title: "Method needs summary",
        messageFormat: "The '{0}' method needs summary, that is used as tool description",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParameterNeedsDescription = new(
        id: "CLFG007",
        title: "Parameter needs description",
        messageFormat: "The '{0}' method parameter '{1}' needs description",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AllParameterNeedsDescription = new(
        id: "CLFG008",
        title: "All parameter needs description",
        messageFormat: "The '{0}' method's all parameters requires description of documentation comment",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParameterTypeIsNotSupported = new(
        id: "CLFG009",
        title: "Parameter type is not supported",
        messageFormat: "The '{0}' method parameter '{1}' type '{2}' is not supported in function",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VoidReturnIsNotSupported = new(
        id: "CLFG010",
        title: "void return type is not supported",
        messageFormat: "The '{0}' method return void or Task or ValueTask there are not supported, allows T, Task<T> or ValueTask<T>",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
