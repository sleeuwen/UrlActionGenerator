using System.Linq;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal static class AssemblyFacts
    {
        public static bool IsRazorViewsAssembly(IAssemblySymbol assemblySymbol)
        {
            if (assemblySymbol.Name.EndsWith(".Views") != true)
                return false;

            var attributes = assemblySymbol.GetAttributes();
            var applicationPartFactoryAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass?.ToString() == "Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute");
            if (applicationPartFactoryAttribute is null)
                return false;

            var factoryType = applicationPartFactoryAttribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (factoryType is null)
                return false;

            if (factoryType != "Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor")
                return false;

            return true;
        }
    }
}
