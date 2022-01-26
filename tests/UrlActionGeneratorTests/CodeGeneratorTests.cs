using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using UrlActionGenerator.Descriptors;
using Xunit;
using CodeGenerator = UrlActionGenerator.CodeGenerator;

namespace UrlActionGeneratorTests
{
    public partial class CodeGeneratorTests
    {
        public static object[][] ScalarValueTestData => new[]
        {
            new object[] { "str", "\"str\"" },
            new object[] { true, "true" },
            new object[] { false, "false" },
            new object[] { byte.MaxValue, "255" },
            new object[] { sbyte.MaxValue, "127" },
            new object[] { short.MaxValue, "32767" },
            new object[] { ushort.MaxValue, "65535" },
            new object[] { int.MaxValue, "2147483647" },
            new object[] { uint.MaxValue, "4294967295" },
            new object[] { long.MaxValue, "9223372036854775807" },
            new object[] { ulong.MaxValue, "18446744073709551615" },
            new object[] { float.MaxValue, "3.4028235E+38" },
            new object[] { double.MaxValue, "1.7976931348623157E+308" },
            new object[] { decimal.MaxValue, "79228162514264337593543950335" },
            new object[] { null, "default" },
            new object[] { new Uri("https://google.com/"), "default" },
        };

        [Theory]
        [MemberData(nameof(ScalarValueTestData))]
        public void ScalarValueTests(object value, string expected)
        {
            var result = CodeGenerator.ScalarValue(value);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void WriteRouteValues_EmptyDictionary()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, Array.Empty<ParameterDescriptor>(), Array.Empty<KeyValuePair<string, object>>());

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
});
".TrimStart());
        }

        [Fact]
        public void WriteRouteValues_Extras()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, Array.Empty<ParameterDescriptor>(), new[]
            {
                new KeyValuePair<string, object>("area", ""),
                new KeyValuePair<string, object>("controller", "Home"),
            });

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
    new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
    new System.Collections.Generic.KeyValuePair<string, object>(""controller"", ""Home""),
});
".TrimStart());
        }

        [Fact]
        public void WriteRouteValues_Parameters()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, new[]
            {
                new ParameterDescriptor("param", "string", false, null),
            }, Array.Empty<KeyValuePair<string, object>>());

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
    new System.Collections.Generic.KeyValuePair<string, object>(""param"", @param),
});
".TrimStart());
        }

        [Fact]
        public void WriteRouteValues_ParametersDefaultValue()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, new[]
            {
                new ParameterDescriptor("param", "string", true, "str"),
            }, Array.Empty<KeyValuePair<string, object>>());

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
    new System.Collections.Generic.KeyValuePair<string, object>(""param"", @param),
});
".TrimStart());
        }

        [Fact]
        public void WriteRouteValues_ParametersNullable()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, new[]
            {
                new ParameterDescriptor("param", "string?", true, null),
            }, Array.Empty<KeyValuePair<string, object>>());

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
    @param == null ? default : new System.Collections.Generic.KeyValuePair<string, object>(""param"", @param),
});
".TrimStart());
        }

        [Fact]
        public void WriteRouteValues_ExtrasAndParameters()
        {
            var stringWriter = new StringWriter();
            var writer = new IndentedTextWriter(stringWriter);

            // Act
            CodeGenerator.WriteRouteValues(writer, new[]
            {
                new ParameterDescriptor("param", "string?", true, null),
            }, new[]
            {
                new KeyValuePair<string, object>("area", ""),
            });

            // Assert
            var routeValuesCode = stringWriter.ToString();
            routeValuesCode.Should().Be(@"
var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
    new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
    @param == null ? default : new System.Collections.Generic.KeyValuePair<string, object>(""param"", @param),
});
".TrimStart());
        }
    }
}
