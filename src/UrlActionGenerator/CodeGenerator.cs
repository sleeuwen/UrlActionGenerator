using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UrlActionGenerator.Descriptors;

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
            writer.WriteLine("}\n");
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
                    writer.Write(ScalarValue(parameter.DefaultValue));
                }
            }
        }

        internal static void WriteRouteValues(IndentedTextWriter writer, IEnumerable<ParameterDescriptor> parameters, IEnumerable<KeyValuePair<string, object>> extras)
        {
            writer.WriteLine("var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {");
            writer.Indent++;

            foreach (var kv in extras)
            {
                writer.WriteLine($"new System.Collections.Generic.KeyValuePair<string, object>(\"{kv.Key}\", {ScalarValue(kv.Value ?? "")}),");
            }

            foreach (var parameter in parameters)
            {
                if (parameter.IsNullable && (!parameter.HasDefaultValue || parameter.DefaultValue == null))
                    writer.Write($"{parameter.Name} == null ? default : ");

                writer.WriteLine($"new System.Collections.Generic.KeyValuePair<string, object>({ScalarValue(parameter.Name)}, @{parameter.Name}),");
            }

            writer.Indent--;
            writer.WriteLine("});");
        }

        internal static string ScalarValue(object value) => value switch
        {
            null => "default",
            string str => $"\"{str.Replace("\"", "\\\"")}\"",
            bool b => b.ToString().ToLowerInvariant(),
            byte by => by.ToString(CultureInfo.InvariantCulture),
            sbyte sby => sby.ToString(CultureInfo.InvariantCulture),
            short s => s.ToString(CultureInfo.InvariantCulture),
            ushort us => us.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(CultureInfo.InvariantCulture),
            uint ui => ui.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            ulong ul => ul.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            var val => val.ToString(),
        };
    }
}
