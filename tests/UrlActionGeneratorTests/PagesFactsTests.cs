using FluentAssertions;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class PagesFactsTests
    {
        [Fact]
        public void IsRazorPage_ReturnsTrue()
        {
            var file = new InMemoryAdditionalText("", "@page");

            // Act
            var result = PagesFacts.IsRazorPage(file);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsRazorPage_ReturnsFalse()
        {
            var file = new InMemoryAdditionalText("", "");

            // Act
            var result = PagesFacts.IsRazorPage(file);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("/Pages/Index.cshtml", false)]
        [InlineData("/Pages/Nested/Folder/Index.cshtml", false)]
        [InlineData("/Pages/_ViewStart.cshtml", true)]
        [InlineData("/Pages/_ViewImports.cshtml", true)]
        [InlineData("/Pages/Nested/Folder/_ViewStart.cshtml", true)]
        [InlineData("/Pages/Nested/Folder/_ViewImports.cshtml", true)]
        public void IsImplicitlyIncludedFile(string path, bool expected)
        {
            var file = new InMemoryAdditionalText(path, "");
            var pageData = new PageData(file);

            // Act
            var result = PagesFacts.IsImplicitlyIncludedFile(pageData);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractUsings_ReturnsEmpty_WhenNoUsings()
        {
            var file = new InMemoryAdditionalText("", "");
            var pageData = new PageData(file);

            // Act
            var result = PagesFacts.ExtractUsings(pageData);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ExtractUsings_ReturnsUsings()
        {
            var file = new InMemoryAdditionalText("", @"@using System
@using System.Text");
            var pageData = new PageData(file);

            // Act
            var result = PagesFacts.ExtractUsings(pageData);

            // Assert
            result.Should().BeEquivalentTo(new[]
            {
                "System",
                "System.Text",
            });
        }
    }
}
