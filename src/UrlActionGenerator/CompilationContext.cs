using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    public class CompilationContext
    {
        public CompilationContext(Compilation compilation)
        {
            Compilation = compilation;
        }

        public Compilation Compilation { get; }

        private INamedTypeSymbol _bindPropertyAttribute;
        public INamedTypeSymbol BindPropertyAttribute => _bindPropertyAttribute ??= Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.BindPropertyAttribute");

        private INamedTypeSymbol _fromQueryAttribute;
        public INamedTypeSymbol FromQueryAttribute => _fromQueryAttribute ??= Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromQueryAttribute");

        private INamedTypeSymbol _fromRouteAttribute;
        public INamedTypeSymbol FromRouteAttribute => _fromRouteAttribute ??= Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromRouteAttribute");
    }
}
