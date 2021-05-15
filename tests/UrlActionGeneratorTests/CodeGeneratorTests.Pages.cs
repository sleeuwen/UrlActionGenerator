using System.Collections.Generic;
using UrlActionGenerator;
using UrlActionGenerator.Descriptors;
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
            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
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
            area.Pages.Add(new PageDescriptor(area, "/Index", null, null));

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
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
            new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
        });
        return urlHelper.Page(""/Index"", __routeValues);
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaPages_MultiPage()
        {
            var area = new PageAreaDescriptor("");
            area.Pages.Add(new PageDescriptor(area, "/Index"));
            area.Pages.Add(new PageDescriptor(area, "/Privacy"));

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
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
            new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
        });
        return urlHelper.Page(""/Index"", __routeValues);
    }

    public string Privacy()
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
            new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
        });
        return urlHelper.Page(""/Privacy"", __routeValues);
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaPages_Folders()
        {
            var area = new PageAreaDescriptor("");
            var folder = new PageFolderDescriptor(area, "Home");
            area.Folders.Add(folder);
            folder.Pages.Add(new PageDescriptor(area, "/Index"));
            var folder2 = new PageFolderDescriptor(area, "Other");
            area.Folders.Add(folder2);
            folder2.Pages.Add(new PageDescriptor(area, "/Privacy"));

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

    public HomePagesFolder Home
        => new HomePagesFolder(urlHelper);

    public class HomePagesFolder
    {
        private readonly IUrlHelper urlHelper;
        public HomePagesFolder(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

        public string Index()
        {
            var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
            });
            return urlHelper.Page(""/Index"", __routeValues);
        }

    }

    public OtherPagesFolder Other
        => new OtherPagesFolder(urlHelper);

    public class OtherPagesFolder
    {
        private readonly IUrlHelper urlHelper;
        public OtherPagesFolder(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

        public string Privacy()
        {
            var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
                new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
                new System.Collections.Generic.KeyValuePair<string, object>(""handler"", """"),
            });
            return urlHelper.Page(""/Privacy"", __routeValues);
        }

    }

}

", _code.ToString(), false, true);
        }
    }
}
