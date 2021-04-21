using FluentAssertions;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class PageDataTests
    {
        [Theory]
        [InlineData("", null)]
        [InlineData("/Pages/Index.cshtml", null)]
        [InlineData("/Pages/Nested/Feature/Folder/Index.cshtml", null)]
        [InlineData("/Areas/Index.cshtml", null)]
        [InlineData("/Areas/Pages/Index.cshtml", null)]
        [InlineData("/Areas/Admin/Pages/Index.cshtml", "Admin")]
        [InlineData("/Areas/Admin/Pages/Nested/Feature/Folder/Index.cshtml", "Admin")]
        public void Area_ReturnsTheCorrectArea(string path, string expected)
        {
            var additionalText = new InMemoryAdditionalText(path, "");
            var pageData = new PageData(additionalText);

            // Act
            var result = pageData.Area;

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("", null, null, null)]
        [InlineData("/Pages", null, null, null)]
        [InlineData("/Pages/Index.cshtml", "/Index", "/", "Index")]
        [InlineData("/Pages/Nested/Feature/Folder/Index.cshtml", "/Nested/Feature/Folder/Index", "/Nested/Feature/Folder", "Index")]
        [InlineData("/Areas/Index.cshtml", null, null, null)]
        [InlineData("/Areas/Pages/Index.cshtml", "/Index", "/", "Index")]
        [InlineData("/Areas/Admin/Pages/Index.cshtml", "/Index", "/", "Index")]
        [InlineData("/Areas/Admin/Pages/Nested/Feature/Folder/Index.cshtml", "/Nested/Feature/Folder/Index", "/Nested/Feature/Folder", "Index")]
        public void Page_ReturnsTheCorrectPage(string path, string expectedPage, string expectedFolder, string expectedPageName)
        {
            var additionalText = new InMemoryAdditionalText(path, "");
            var pageData = new PageData(additionalText);

            // Act
            var page = pageData.Page;
            var folder = pageData.Folder;
            var pageName = pageData.PageName;

            // Assert
            page.Should().Be(expectedPage);
            folder.Should().Be(expectedFolder);
            pageName.Should().Be(expectedPageName);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("@page", null)]
        [InlineData(@"@page ""/""", "/")]
        [InlineData(@"@page ""/{id}""", "/{id}")]
        [InlineData(@"@page ""/{id:int:min(3)}""", "/{id:int:min(3)}")]
        [InlineData(@"@page ""/page/{id:int:min(3)}/comments""", "/page/{id:int:min(3)}/comments")]
        [InlineData(@"@page ""/""; @using System", "/")]
        public void Route_ReturnsTheCorrectRoute(string source, string expected)
        {
            var additionalText = new InMemoryAdditionalText("Index.cshtml", source);
            var pageData = new PageData(additionalText);

            // Act
            var route = pageData.Route;

            // Assert
            route.Should().Be(expected);
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("@page", null)]
        [InlineData("@model ", null)]
        [InlineData("@model IndexModel", "IndexModel")]
        [InlineData("@model AspNetCoreSample.ViewModels.IndexModel", "AspNetCoreSample.ViewModels.IndexModel")]
        public void Model_ReturnsTheCorrectModel(string source, string expected)
        {
            var additionalText = new InMemoryAdditionalText("Index.cshtml", source);
            var pageData = new PageData(additionalText);

            // Act
            var model = pageData.Model;

            // Assert
            model.Should().Be(expected);
        }
    }
}
