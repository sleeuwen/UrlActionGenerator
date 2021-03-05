using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class CodeGeneratorTests
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
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
    }
}
", _code.ToString());
        }

        [Fact]
        public void GenerateUrlActions_SingleArea()
        {
            var areas = new List<AreaDescriptor>();
            areas.Add(new AreaDescriptor(""));

            // Act
            CodeGenerator.WriteUrlActions(_writer, areas);

            // Assert
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
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
", _code.ToString());
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
            Assert.Equal(@"namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
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
", _code.ToString());
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

", _code.ToString());
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

", _code.ToString());
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

", _code.ToString());
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

", _code.ToString());
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
", _code.ToString());
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
        => urlHelper.Action(""Index"", ""Home"", new { area = """" });
}
", _code.ToString());
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
        => urlHelper.Action(""Index"", ""Home"", new { area = """" });

    public string Contact()
        => urlHelper.Action(""Contact"", ""Home"", new { area = """" });
}
", _code.ToString());
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
    => urlHelper.Action(""Index"", ""Home"", new { area = """" });
", _code.ToString());
        }

        [Fact]
        public void WriteAction_WithParameters()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            var action = new ActionDescriptor(controller, "List");
            action.Parameters.Add(new ParameterDescriptor("search", "string", false, null));
            action.Parameters.Add(new ParameterDescriptor("page", "int", false, null));

            // Act
            CodeGenerator.WriteAction(_writer, action);

            // Assert
            Assert.Equal(@"public string List(string @search, int @page)
    => urlHelper.Action(""List"", ""Home"", new { area = """", @search, @page });
", _code.ToString());
        }

        [Fact]
        public void WriteAction_WithParametersDefaultValue()
        {
            var area = new AreaDescriptor("");
            var controller = new ControllerDescriptor(area, "Home");
            var action = new ActionDescriptor(controller, "List");
            action.Parameters.Add(new ParameterDescriptor("search", "string", false, null));
            action.Parameters.Add(new ParameterDescriptor("page", "int", true, 1));

            // Act
            CodeGenerator.WriteAction(_writer, action);

            // Assert
            Assert.Equal(@"public string List(string @search, int @page = 1)
    => urlHelper.Action(""List"", ""Home"", new { area = """", @search, @page });
", _code.ToString());
        }
    }
}
