using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator.Descriptors
{
    public class ActionDescriptor
    {
        public ActionDescriptor(ControllerDescriptor controller, string actionName, IMethodSymbol symbol)
        {
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            Name = actionName ?? throw new ArgumentNullException(nameof(actionName));
            Parameters = new List<ParameterDescriptor>();
            Symbol = symbol;
        }

        public ControllerDescriptor Controller { get; }

        public string Name { get; }

        public List<ParameterDescriptor> Parameters { get; }

        public IMethodSymbol Symbol { get; set; }
    }
}
