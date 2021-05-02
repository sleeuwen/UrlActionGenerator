using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal static class Diagnostics
    {
        public static readonly DiagnosticDescriptor MvcCodeGenException = new DiagnosticDescriptor(
            id: "UAG001",
            title: "An exception occurred while generating the MVC strongly typed actions.",
            messageFormat: "An exception occurred while generating the MVC strongly typed actions: '{0}'. Please raise an issue in https://github.com/sleeuwen/UrlActionGenerator and include the following stacktrace:\n{1}.",
            category: "UrlActionGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RazorPagesCodeGenException = new DiagnosticDescriptor(
            id: "UAG002",
            title: "An exception occurred while generating the Razor Pages strongly typed actions.",
            messageFormat: "An exception occurred while generating the Razor Pages strongly typed actions: '{0}'. Please raise an issue in https://github.com/sleeuwen/UrlActionGenerator and include the following stacktrace:\n{1}.",
            category: "UrlActionGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
