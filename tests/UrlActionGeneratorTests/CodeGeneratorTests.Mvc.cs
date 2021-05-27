using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using UrlActionGenerator;
using UrlActionGenerator.Descriptors;
using Xunit;

namespace UrlActionGeneratorTests
{
    public partial class CodeGeneratorTests
    {
        private readonly StringWriter _code;
        private readonly IndentedTextWriter _writer;

        public CodeGeneratorTests()
        {
            _code = new StringWriter();
            _writer = new IndentedTextWriter(_code);
        }

        [Fact]
        public void GenerateUrlActions_NoAreas()
        {
            var areas = new List<AreaDescriptor>();

            // Act
            CodeGenerator.WriteUrlActions(_writer, areas);

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
        public void GenerateUrlActions_SingleArea()
        {
            var areas = new List<AreaDescriptor>();
            areas.Add(new AreaDescriptor(""));

            // Act
            CodeGenerator.WriteUrlActions(_writer, areas);

            // Assert
            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
    {
        public static UrlActions Actions(this IUrlHelper urlHelper)
            => new UrlActions(urlHelper);

        public class UrlActions
        {
            private readonly IUrlHelper urlHelper;
            public UrlActions(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

    }
}
", _code.ToString(), false, true);
        }

        [Fact]
        public void GenerateUrlActions_MultiArea()
        {
            var areas = new List<AreaDescriptor>();
            areas.Add(new AreaDescriptor(""));
            areas.Add(new AreaDescriptor("Admin"));

            // Act
            CodeGenerator.WriteUrlActions(_writer, areas);

            // Assert
            Assert.Equal(@"// <auto-generated />
namespace Microsoft.AspNetCore.Mvc
{
    public static partial class UrlActionGenerator_UrlHelperExtensions
    {
        public static UrlActions Actions(this IUrlHelper urlHelper)
            => new UrlActions(urlHelper);

        public class UrlActions
        {
            private readonly IUrlHelper urlHelper;
            public UrlActions(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

        public static AdminUrlActions AdminActions(this IUrlHelper urlHelper)
            => new AdminUrlActions(urlHelper);

        public class AdminUrlActions
        {
            private readonly IUrlHelper urlHelper;
            public AdminUrlActions(IUrlHelper urlHelper)
            {
                this.urlHelper = urlHelper;
            }

        }

    }
}
", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaActions_DefaultArea()
        {
            var area = new AreaDescriptor("");

            // Act
            CodeGenerator.WriteAreaActions(_writer, area);

            // Assert
            Assert.Equal(@"public static UrlActions Actions(this IUrlHelper urlHelper)
    => new UrlActions(urlHelper);

public class UrlActions
{
    private readonly IUrlHelper urlHelper;
    public UrlActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaActions_CustomArea()
        {
            var area = new AreaDescriptor("Custom");

            // Act
            CodeGenerator.WriteAreaActions(_writer, area);

            // Assert
            Assert.Equal(@"public static CustomUrlActions CustomActions(this IUrlHelper urlHelper)
    => new CustomUrlActions(urlHelper);

public class CustomUrlActions
{
    private readonly IUrlHelper urlHelper;
    public CustomUrlActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaActions_SingleController()
        {
            var area = new AreaDescriptor("Custom");
            area.Controllers.Add(new ControllerDescriptor(area, "Home"));

            // Act
            CodeGenerator.WriteAreaActions(_writer, area);

            // Assert
            Assert.Equal(@"public static CustomUrlActions CustomActions(this IUrlHelper urlHelper)
    => new CustomUrlActions(urlHelper);

public class CustomUrlActions
{
    private readonly IUrlHelper urlHelper;
    public CustomUrlActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public HomeControllerActions Home
        => new HomeControllerActions(urlHelper);

    public class HomeControllerActions
    {
        private readonly IUrlHelper urlHelper;
        public HomeControllerActions(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAreaActions_MultiController()
        {
            var area = new AreaDescriptor("Custom");
            area.Controllers.Add(new ControllerDescriptor(area, "Home"));
            area.Controllers.Add(new ControllerDescriptor(area, "Contact"));

            // Act
            CodeGenerator.WriteAreaActions(_writer, area);

            // Assert
            Assert.Equal(@"public static CustomUrlActions CustomActions(this IUrlHelper urlHelper)
    => new CustomUrlActions(urlHelper);

public class CustomUrlActions
{
    private readonly IUrlHelper urlHelper;
    public CustomUrlActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public HomeControllerActions Home
        => new HomeControllerActions(urlHelper);

    public class HomeControllerActions
    {
        private readonly IUrlHelper urlHelper;
        public HomeControllerActions(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

    }

    public ContactControllerActions Contact
        => new ContactControllerActions(urlHelper);

    public class ContactControllerActions
    {
        private readonly IUrlHelper urlHelper;
        public ContactControllerActions(IUrlHelper urlHelper)
        {
            this.urlHelper = urlHelper;
        }

    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteControllerActions_NoAction()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");

            // Act
            CodeGenerator.WriteControllerActions(_writer, controller);

            // Assert
            Assert.Equal(@"public HomeControllerActions Home
    => new HomeControllerActions(urlHelper);

public class HomeControllerActions
{
    private readonly IUrlHelper urlHelper;
    public HomeControllerActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteControllerActions_SingleAction()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            controller.Actions.Add(new ActionDescriptor(controller, "Index"));

            // Act
            CodeGenerator.WriteControllerActions(_writer, controller);

            // Assert
            Assert.Equal(@"public HomeControllerActions Home
    => new HomeControllerActions(urlHelper);

public class HomeControllerActions
{
    private readonly IUrlHelper urlHelper;
    public HomeControllerActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public string Index()
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
        });
        return urlHelper.Action(""Index"", ""Home"", __routeValues);
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteControllerActions_MultiAction()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            controller.Actions.Add(new ActionDescriptor(controller, "Index"));
            controller.Actions.Add(new ActionDescriptor(controller, "Contact"));

            // Act
            CodeGenerator.WriteControllerActions(_writer, controller);

            // Assert
            Assert.Equal(@"public HomeControllerActions Home
    => new HomeControllerActions(urlHelper);

public class HomeControllerActions
{
    private readonly IUrlHelper urlHelper;
    public HomeControllerActions(IUrlHelper urlHelper)
    {
        this.urlHelper = urlHelper;
    }

    public string Index()
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
        });
        return urlHelper.Action(""Index"", ""Home"", __routeValues);
    }

    public string Contact()
    {
        var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
            new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
        });
        return urlHelper.Action(""Contact"", ""Home"", __routeValues);
    }

}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAction_Simple()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            var action = new ActionDescriptor(controller, "Index");

            // Act
            CodeGenerator.WriteAction(_writer, action);

            // Assert
            Assert.Equal(@"public string Index()
{
    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
    });
    return urlHelper.Action(""Index"", ""Home"", __routeValues);
}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAction_WithParameters()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            var action = new ActionDescriptor(controller, "List");
            action.Parameters.Add(new ParameterDescriptor("search", "string", false, null, null));
            action.Parameters.Add(new ParameterDescriptor("page", "int", false, null, null));

            // Act
            CodeGenerator.WriteAction(_writer, action);

            // Assert
            Assert.Equal(@"public string List(string @search, int @page)
{
    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
        new System.Collections.Generic.KeyValuePair<string, object>(""search"", @search),
        new System.Collections.Generic.KeyValuePair<string, object>(""page"", @page),
    });
    return urlHelper.Action(""List"", ""Home"", __routeValues);
}

", _code.ToString(), false, true);
        }

        [Fact]
        public void WriteAction_WithParametersDefaultValue()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            var action = new ActionDescriptor(controller, "List");
            action.Parameters.Add(new ParameterDescriptor("search", "string", true, "term\"", null));
            action.Parameters.Add(new ParameterDescriptor("page", "int", true, 1, null));

            // Act
            CodeGenerator.WriteAction(_writer, action);

            // Assert
            Assert.Equal(@"public string List(string @search = ""term\"""", int @page = 1)
{
    var __routeValues = Microsoft.AspNetCore.Routing.RouteValueDictionary.FromArray(new System.Collections.Generic.KeyValuePair<string, object>[] {
        new System.Collections.Generic.KeyValuePair<string, object>(""area"", """"),
        new System.Collections.Generic.KeyValuePair<string, object>(""search"", @search),
        new System.Collections.Generic.KeyValuePair<string, object>(""page"", @page),
    });
    return urlHelper.Action(""List"", ""Home"", __routeValues);
}

", _code.ToString(), false, true);
        }
    }
}
