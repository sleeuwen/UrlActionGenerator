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

    public interface IPagesFoldersDescriptor
    {
        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }

    public class PageAreaDescriptor : IPagesFoldersDescriptor
    {
        public PageAreaDescriptor(string name)
        {
            Name = name;
            Pages = new List<PageDescriptor>();
            Folders = new List<PageFolderDescriptor>();
        }

        public string Name { get; }

        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }

    public class PageFolderDescriptor : IPagesFoldersDescriptor
    {
        public PageFolderDescriptor(PageAreaDescriptor area, string name)
        {
            Area = area;
            Name = name;
            Pages = new List<PageDescriptor>();
            Folders = new List<PageFolderDescriptor>();
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }

    public class PageDescriptor
    {
        public PageDescriptor(PageAreaDescriptor area, string name, string pageHandler = null, List<ParameterDescriptor> parameters = null)
        {
            Area = area;
            Name = name;
            PageHandler = pageHandler;
            Parameters = parameters ?? new List<ParameterDescriptor>();
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public string PageHandler { get; }

        public List<ParameterDescriptor> Parameters { get; }
    }
}
