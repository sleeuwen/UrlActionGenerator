using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
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
}
