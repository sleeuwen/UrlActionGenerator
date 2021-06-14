using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator.Descriptors
{
    public class ControllerDescriptor
    {
        public ControllerDescriptor(AreaDescriptor area, string controllerName, INamedTypeSymbol symbol)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Name = controllerName ?? throw new ArgumentNullException(nameof(controllerName));
            Actions = new KeyedCollection<ActionDescriptor>(action => new { action.Name, Parameters = string.Join(",", action.Parameters.Select(param => param.Type.TrimEnd('?'))) });
            Symbol = symbol;
        }

        public AreaDescriptor Area { get; }

        public string Name { get; }

        public KeyedCollection<ActionDescriptor> Actions { get; }

        public INamedTypeSymbol Symbol { get; }
    }
}
