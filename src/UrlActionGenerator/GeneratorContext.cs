using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal class GeneratorContext
    {
        private Dictionary<string, INamedTypeSymbol?> _typeSymbolCache = new();

        public GeneratorContext(Compilation compilation, ImmutableArray<ITypeSymbol> excludedTypes)
        {
            Compilation = compilation;
            ExcludedTypes = excludedTypes;

            IsViewsAssembly = compilation.AssemblyName?.EndsWith(".Views") == true;

            var members = compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers("Dispose");
            Debug.Assert(members.Length == 1, "Expected a single Dispose member on IDisposable");

            DisposableDispose = (IMethodSymbol)members[0];
        }

        public GeneratorContext(GeneratorContext context, ImmutableDictionary<string, ImmutableArray<string>> implicitlyImportedUsings)
        {
            Compilation = context.Compilation;
            ExcludedTypes = context.ExcludedTypes;
            IsViewsAssembly = context.IsViewsAssembly;
            DisposableDispose = context.DisposableDispose;
            ImplicitlyImportedUsings = implicitlyImportedUsings;
        }

        public Compilation Compilation { get; }
        public ImmutableArray<ITypeSymbol> ExcludedTypes { get; }

        public bool IsViewsAssembly { get; }
        public IMethodSymbol DisposableDispose { get; }

        public ImmutableDictionary<string, ImmutableArray<string>> ImplicitlyImportedUsings { get; set; }

        public INamedTypeSymbol? GetTypeByMetadataName(string fullyQualifiedMetadataName)
        {
            if (!_typeSymbolCache.TryGetValue(fullyQualifiedMetadataName, out var symbol))
            {
                symbol = Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
                _typeSymbolCache[fullyQualifiedMetadataName] = symbol;
            }

            return symbol;
        }
    }
}
