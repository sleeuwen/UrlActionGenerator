using System.Collections.Generic;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public partial class CodeGeneratorTests
    {
        [Fact]
        public void GenerateUrlPages_NoAreas()
        {
            var pages = new List<PageAreaDescriptor>();

            // Act
            CodeGenerator.WriteUrlPages(_writer, pages);

            // Assert
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
    {
    }
}
", _code.ToString(), false, true);
        }

        [Fact]
        public void GenerateUrlPages_SingleArea()
        {
            var pages = new List<PageAreaDescriptor>();
            pages.Add(new PageAreaDescriptor(""));

            // Act
            CodeGenerator.WriteUrlPages(_writer, pages);

            // Assert
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
    {
        public static UrlPages Pages(this IUrlHelper urlHelper)
            => new UrlPages(urlHelper);

        public class UrlPages
        {
            private readonly IUrlHelper urlHelper;
            public UrlPages(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

    }
}
", _code.ToString(), false, true);
        }

        [Fact]
        public void GenerateUrlPages_MultiArea()
        {
            var pages = new List<PageAreaDescriptor>();
            pages.Add(new PageAreaDescriptor(""));
            pages.Add(new PageAreaDescriptor("Admin"));

            // Act
            CodeGenerator.WriteUrlPages(_writer, pages);

            // Assert
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlHelperExtensions
    {
        public static UrlPages Pages(this IUrlHelper urlHelper)
            => new UrlPages(urlHelper);

        public class UrlPages
        {
            private readonly IUrlHelper urlHelper;
            public UrlPages(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

        public static AdminUrlPages AdminPages(this IUrlHelper urlHelper)
            => new AdminUrlPages(urlHelper);

        public class AdminUrlPages
        {
            private readonly IUrlHelper urlHelper;
            public AdminUrlPages(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

    }
}
", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaPages_SinglePage()
        {
            var area = new PageAreaDescriptor("");
            area.Pages.Add(new PageDescriptor { Area = area, Name = "/Index" });

            // Act
            CodeGenerator.WriteAreaPages(_writer, area);

            // Assert
            Assert.Equal(@"public static UrlPages Pages(this IUrlHelper urlHelper)
    => new UrlPages(urlHelper);

public class UrlPages
{
    private readonly IUrlHelper urlHelper;
    public UrlPages(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public string Index()
        => urlHelper.Page(""/Index"", new { area = """" });
}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaPages_MultiPage()
        {
            var area = new PageAreaDescriptor("");
            area.Pages.Add(new PageDescriptor { Area = area, Name = "/Index" });
            area.Pages.Add(new PageDescriptor { Area = area, Name = "/Privacy" });

            // Act
            CodeGenerator.WriteAreaPages(_writer, area);

            // Assert
            Assert.Equal(@"public static UrlPages Pages(this IUrlHelper urlHelper)
    => new UrlPages(urlHelper);

public class UrlPages
{
    private readonly IUrlHelper urlHelper;
    public UrlPages(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public string Index()
        => urlHelper.Page(""/Index"", new { area = """" });

    public string Privacy()
        => urlHelper.Page(""/Privacy"", new { area = """" });
}

", _code.ToString(), false, true);
        }
    }
}
