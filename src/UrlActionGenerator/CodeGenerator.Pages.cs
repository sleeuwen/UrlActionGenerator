using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace UrlActionGenerator
{
    public static partial class CodeGenerator
    {
        public static void WriteUrlPages(IndentedTextWriter writer, List<PageAreaDescriptor> areas)
        {
            writer.WriteLine("namespace Microsoft.AspNetCore.Mvc");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine("public static partial class UrlHelperExtensions");
            writer.WriteLine("{");
            writer.Indent++;

            foreach (var area in areas)
            {
                WriteAreaPages(writer, area);
            }

            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
        }

        public static void WriteAreaPages(IndentedTextWriter writer, PageAreaDescriptor area)
        {
            writer.WriteLine($"public static {area.Name}UrlPages {area.Name}Pages(this IUrlHelper urlHelper)");
            writer.Indent++;
            writer.WriteLine($"=> new {area.Name}UrlPages(urlHelper);");
            writer.Indent--;
            writer.WriteLineNoTabs("");

            writer.WriteLine($"public class {area.Name}UrlPages");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine("private readonly IUrlHelper urlHelper;");
            writer.WriteLine($"public {area.Name}UrlPages(IUrlHelper urlHelper)");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("this.urlHelper = urlHelper;");
            writer.Indent--;
            writer.WriteLine("}\n");

            var first = true;
            foreach (var page in area.Pages)
            {
                if (!first)
                    writer.WriteLineNoTabs("");
                first = false;

                WritePages(writer, page);
            }

            writer.Indent--;
            writer.WriteLine("}\n");
        }

        public static void WritePages(IndentedTextWriter writer, PageDescriptor page)
        {
            var folders = page.Name.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            int i;
            for (i = 0; i < folders.Length - 1; i++)
            {
                writer.WriteLine($"public {folders[i]}PagesFolder {folders[i]}");
                writer.Indent++;
                writer.WriteLine($"=> new {folders[i]}PagesFolder(urlHelper);");
                writer.Indent--;
                writer.WriteLineNoTabs("");

                writer.WriteLine($"public static partial class {folders[i]}PagesFolder");
                writer.WriteLine("{");
                writer.Indent++;

                writer.WriteLine("private readonly IUrlHelper urlHelper;");
                writer.WriteLine($"public {folders[i]}PagesFolder(IUrlHelper urlHelper)");
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine("this.urlHelper = urlHelper;");
                writer.Indent--;
                writer.WriteLine("}\n");
            }

            writer.WriteLine($"public string {folders[i]}()");
            writer.Indent++;
            writer.WriteLine($"=> urlHelper.Page(\"{page.Name}\", new {{ area = \"{page.Area.Name}\" }});");
            writer.Indent--;

            for (; i > 0; i--)
            {
                writer.Indent--;
                writer.WriteLine("}");
            }
        }
    }
}
