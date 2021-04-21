using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace UrlActionGenerator
{
    public static partial class CodeGenerator
    {
        internal static void WriteLines(this IndentedTextWriter writer, string lines)
        {
            foreach (var line in lines.TrimStart('\n', '\r').Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                    writer.WriteLineNoTabs("");
                else
                    writer.WriteLine(line.TrimEnd());
            }
        }

        internal static void WriteAreaClassStart(IndentedTextWriter writer, string className, string methodName)
        {
            writer.WriteLines($@"
public static {className} {methodName}(this IUrlHelper urlHelper)
    => new {className}(urlHelper);

public class {className}
{{
    private readonly IUrlHelper urlHelper;
    public {className}(IUrlHelper urlHelper)
    {{
        this.urlHelper = urlHelper;
    }}
");

            writer.Indent += 1;
        }

        internal static void WriteAreaClassEnd(IndentedTextWriter writer)
        {
            writer.Indent -= 1;
            writer.WriteLine("}\n");
        }

        internal static void WriteHelperClassStart(IndentedTextWriter writer, string className, string methodName)
        {
            writer.WriteLines($@"
public {className} {methodName}
    => new {className}(urlHelper);

public class {className}
{{
    private readonly IUrlHelper urlHelper;
    public {className}(IUrlHelper urlHelper)
    {{
        this.urlHelper = urlHelper;
    }}
");

            writer.Indent += 1;
        }

        internal static void WriteHelperClassEnd(IndentedTextWriter writer)
        {
            writer.Indent -= 1;
            writer.WriteLine("}");
        }

        internal static void WriteMethodParameters(TextWriter writer, IEnumerable<ParameterDescriptor> parameters)
        {
            var first = true;
            foreach (var parameter in parameters)
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
                    writer.Write(parameter.DefaultValue switch
                    {
                        null => "default",
                        string str => $"\"{str.Replace("\"", "\\\"")}\"",
                        var val => val.ToString(),
                    });
                }
            }
        }

        internal static void WriteRouteParameters(TextWriter writer, IEnumerable<ParameterDescriptor> parameters)
        {
            foreach (var parameter in parameters)
            {
                writer.Write($", @{parameter.Name}");
            }
        }
    }
}
