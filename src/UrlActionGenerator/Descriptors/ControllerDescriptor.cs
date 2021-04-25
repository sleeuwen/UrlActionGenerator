using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
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
}
