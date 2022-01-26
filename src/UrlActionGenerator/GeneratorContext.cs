using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal class GeneratorContext
    {
        private Dictionary<string, INamedTypeSymbol?> _typeSymbolCache = new();

        public GeneratorContext(Compilation compilation, ImmutableArray<ITypeSymbol> excludedParameterTypes)
        {
            Compilation = compilation;
            ExcludedParameterTypes = excludedParameterTypes.AddRange(new[]
            {
                compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.IFormFile"),
                compilation.GetTypeByMetadataName("System.Threading.CancellationToken"),
            });

            IsViewsAssembly = compilation.AssemblyName?.EndsWith(".Views") == true;

            var members = compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers("Dispose");
            Debug.Assert(members.Length == 1, "Expected a single Dispose member on IDisposable");

            DisposableDispose = (IMethodSymbol)members[0];
        }

        protected GeneratorContext(GeneratorContext context)
        {
            Compilation = context.Compilation;
            ExcludedParameterTypes = context.ExcludedParameterTypes;
            IsViewsAssembly = context.IsViewsAssembly;
            DisposableDispose = context.DisposableDispose;
        }

        public Compilation Compilation { get; }
        public ImmutableArray<ITypeSymbol> ExcludedParameterTypes { get; }

        public bool IsViewsAssembly { get; }
        public IMethodSymbol DisposableDispose { get; }

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

    internal class GeneratorPagesContext : GeneratorContext
    {
        public GeneratorPagesContext(GeneratorContext context, ImmutableDictionary<string, ImmutableArray<string>> implicitlyImportedUsings)
            : base(context)
        {
            ImplicitlyImportedUsings = implicitlyImportedUsings;
        }

        public ImmutableDictionary<string, ImmutableArray<string>> ImplicitlyImportedUsings { get; set; }
    }
}
