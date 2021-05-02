using System;
using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public class ActionDescriptor
    {
        public ActionDescriptor(ControllerDescriptor controller, string actionName)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Name = actionName ?? throw new ArgumentNullException(nameof(actionName));
            Parameters = new List<ParameterDescriptor>();
        }

        public ControllerDescriptor Controller { get; }

        public string Name { get; }

        public List<ParameterDescriptor> Parameters { get; }
    }
}
