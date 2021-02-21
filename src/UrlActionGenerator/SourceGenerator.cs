using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    [Generator]
    public class SourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(context.Compilation).ToList();

            using var sourceWriter = new StringWriter();
            using var writer = new IndentedTextWriter(sourceWriter, "    ");

            CodeGenerator.WriteUrlActions(writer, areas);

            context.AddSource("UrlSourceGenerator.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));
        }
    }
}
