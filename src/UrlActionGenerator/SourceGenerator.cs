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
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxReceiver = context.SyntaxReceiver as MySyntaxReceiver;

            using var sourceWriter = new StringWriter();
            using var writer = new IndentedTextWriter(sourceWriter, "    ");

            // MVC
            var areas = MvcDiscoverer.DiscoverAreaControllerActions(context.Compilation, syntaxReceiver.PossibleControllers).ToList();
            if (areas.Any())
            {
                CodeGenerator.WriteUrlActions(writer, areas);
            }

            // Razor Pages
            var pages = PagesDiscoverer.DiscoverAreaPages(context.Compilation, context.AdditionalFiles).ToList();

            foreach (var page in pages.SelectMany(p => p.Pages))
                File.AppendAllText("/home/stephan/Urls.txt", $"{context.Compilation.AssemblyName}\t{page.Area.Name}:{page.Name}\n");

            if (pages.Any())
            {
                CodeGenerator.WriteUrlPages(writer, pages);
            }

            context.AddSource("UrlSourceGenerator.g.cs", SourceText.From(sourceWriter.ToString(), Encoding.UTF8));

            File.AppendAllText("/home/stephan/Urls.cs", sourceWriter.ToString());
        }
    }
}
