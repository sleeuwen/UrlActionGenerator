using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace UrlActionGenerator
{
    public static class CodeGenerator
    {
        public static void WriteUrlActions(IndentedTextWriter writer, List<AreaDescriptor> areas)
        {
            writer.WriteLine("namespace Microsoft.AspNetCore.Mvc");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine("public static class UrlHelperExtensions");
            writer.WriteLine("{");
            writer.Indent++;


            foreach (var area in areas)
            {
                WriteAreaActions(writer, area);
            }

            writer.Indent--;
            writer.WriteLine("}");
            writer.Indent--;
            writer.WriteLine("}");
        }

        public static void WriteAreaActions(IndentedTextWriter writer, AreaDescriptor area)
        {
            writer.WriteLine($"public static {area.Name}UrlActions {area.Name}Actions(this IUrlHelper urlHelper)");
            writer.Indent++;
            writer.WriteLine($"=> new {area.Name}UrlActions(urlHelper);");
            writer.Indent--;
            writer.WriteLineNoTabs("");

            writer.WriteLine($"public class {area.Name}UrlActions");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine("private readonly IUrlHelper urlHelper;");
            writer.WriteLine($"public {area.Name}UrlActions(IUrlHelper urlHelper)");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("this.urlHelper = urlHelper;");
            writer.Indent--;
            writer.WriteLine("}\n");

            var first = true;
            foreach (var controller in area.Controllers)
            {
                if (!first)
                    writer.WriteLineNoTabs("");
                first = false;

                WriteControllerActions(writer, controller);
            }

            writer.Indent--;
            writer.WriteLine("}\n");
        }

        public static void WriteControllerActions(IndentedTextWriter writer, ControllerDescriptor controller)
        {
            writer.WriteLine($"public {controller.Name}ControllerActions {controller.Name}");
            writer.Indent++;
            writer.WriteLine($"=> new {controller.Name}ControllerActions(urlHelper);");
            writer.Indent--;
            writer.WriteLineNoTabs("");

            writer.WriteLine($"public class {controller.Name}ControllerActions");
            writer.WriteLine("{");
            writer.Indent++;

            writer.WriteLine("private readonly IUrlHelper urlHelper;");
            writer.WriteLine($"public {controller.Name}ControllerActions(IUrlHelper urlHelper)");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine("this.urlHelper = urlHelper;");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLineNoTabs("");

            var first = true;
            foreach (var action in controller.Actions)
            {
                if (!first)
                    writer.WriteLineNoTabs("");
                first = false;

                WriteAction(writer, action);
            }

            writer.Indent--;
            writer.WriteLine("}");
        }

        public static void WriteAction(IndentedTextWriter writer, ActionDescriptor action)
        {
            writer.Write($"public string {action.Name}(");

            var first = true;
            foreach (var parameter in action.Parameters)
            {
                if (!first)
                    writer.Write(", ");
                first = false;

                writer.Write(parameter.Type);
                writer.Write(" @");
                writer.Write(parameter.Name);

                if (parameter.HasDefaultValue)
                {
                    writer.Write(" = ");
                    writer.Write(parameter.DefaultValue?.ToString() ?? "null");
                }
            }

            writer.WriteLine(")");
            writer.Indent++;

            writer.Write($@"=> urlHelper.Action(""{action.Name}"", ""{action.Controller.Name}"", new {{ area = ""{action.Controller.Area.Name}""");

            foreach (var parameter in action.Parameters)
            {
                writer.Write($", @{parameter.Name}");
            }

            writer.WriteLine(" });");
            writer.Indent--;
        }
    }
}
