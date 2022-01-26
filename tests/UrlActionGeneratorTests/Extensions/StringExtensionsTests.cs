using UrlActionGenerator.Extensions;
using Xunit;

namespace UrlActionGeneratorTests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("PageNumber", "pageNumber")]
        [InlineData("pageSize", "pageSize")]
        public void ToCamelCase(string value, string expected)
        {
            var result = value.ToCamelCase();

            Assert.Equal(expected, result);
        }
    }
}
