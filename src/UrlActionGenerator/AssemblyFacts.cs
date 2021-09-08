using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    internal static class AssemblyFacts
    {
        private const string ProvideApplicationPartFactoryAttribute = "Microsoft.AspNetCore.Mvc.ApplicationParts.ProvideApplicationPartFactoryAttribute";
        private const string globalProvideApplicationPartFactoryAttribute = "global::" + ProvideApplicationPartFactoryAttribute;
        private const string CompiledRazorAssemblyApplicationPartFactory = "Microsoft.AspNetCore.Mvc.ApplicationParts.CompiledRazorAssemblyApplicationPartFactory, Microsoft.AspNetCore.Mvc.Razor";

        public static bool IsRazorViewsAssembly(string? assemblyName, List<AttributeSyntax> assemblyAttributes, Compilation compilation)
        {
            if (assemblyName?.EndsWith(".Views") != true)
                return false;

            if (!assemblyAttributes.Any())
                return false;

            foreach (var group in assemblyAttributes.GroupBy(x => x.SyntaxTree))
            {
                var semanticModel = compilation.GetSemanticModel(group.Key);

                foreach (var attribute in group)
                {
                    var attributeName = attribute.Name.ToString();
                    if (attributeName != ProvideApplicationPartFactoryAttribute && attributeName != globalProvideApplicationPartFactoryAttribute)
                        continue;

                    if (attribute.ArgumentList?.Arguments.Count != 1)
                        continue;

                    var factoryType = semanticModel.GetConstantValue(attribute.ArgumentList.Arguments.First().Expression).Value;
                    if (factoryType as string == CompiledRazorAssemblyApplicationPartFactory)
                        return true;
                }
            }

            return false;
        }
    }
}
