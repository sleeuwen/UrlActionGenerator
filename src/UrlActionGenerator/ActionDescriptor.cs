using System.Collections.Generic;

namespace UrlActionGenerator
{
    public class AreaDescriptor
    {
        public AreaDescriptor(string name)
        {
            Name = name;
            Controllers = new List<ControllerDescriptor>();
        }

        public string Name { get; }

        public List<ControllerDescriptor> Controllers { get; }
    }

    public class ControllerDescriptor
    {
        public ControllerDescriptor(AreaDescriptor area, string controllerName)
        {
            Area = area;
            Name = controllerName;
            Actions = new List<ActionDescriptor>();
        }

        public AreaDescriptor Area { get; }

        public string Name { get; }

        public List<ActionDescriptor> Actions { get; }
    }

    public class ActionDescriptor
    {
        public ActionDescriptor(ControllerDescriptor controller, string actionName)
        {
            Controller = controller;
            Name = actionName;
            Parameters = new List<ParameterDescriptor>();
        }

        public ControllerDescriptor Controller { get; }

        public string Name { get; }

        public List<ParameterDescriptor> Parameters { get; }
    }

    public record ParameterDescriptor
    {
        public ParameterDescriptor(string name, string type, bool hasDefaultValue, object defaultValue)
        {
            Name = name;
            Type = type;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public string Type { get; }

        public bool HasDefaultValue { get; }

        public object DefaultValue { get; }
    }

    public class PageAreaDescriptor
    {
        public PageAreaDescriptor(string name)
        {
            Name = name;
            Pages = new List<PageDescriptor>();
        }

        public string Name { get; set; }

        public List<PageDescriptor> Pages { get; set; }
    }

    public class PageDescriptor
    {
        public PageAreaDescriptor Area { get; set; }

        public string Name { get; set; }
    }
}
