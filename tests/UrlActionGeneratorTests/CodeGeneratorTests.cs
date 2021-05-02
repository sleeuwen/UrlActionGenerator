using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public partial class CodeGeneratorTests
    {
        [Theory]
        [InlineData("str", "\"str\"")]
        [InlineData(true, "true")]
        [InlineData(false, "false")]
        [InlineData(short.MaxValue, "32767")]
        [InlineData(int.MaxValue, "2147483647")]
        [InlineData(long.MaxValue, "9223372036854775807")]
        [InlineData(float.MaxValue, "3.4028235E+38")]
        [InlineData(double.MaxValue, "1.7976931348623157E+308")]
        [InlineData(null, "default")]
        public void ScalarValueTests(object value, string expected)
        {
            var result = CodeGenerator.ScalarValue(value);

            Assert.Equal(expected, result);
        }
    }
}
