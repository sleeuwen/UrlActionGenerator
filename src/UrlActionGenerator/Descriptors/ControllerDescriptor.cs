using System;
using System.Linq;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace UrlActionGenerator.Descriptors
{
    public class ControllerDescriptor
    {
        public ControllerDescriptor(AreaDescriptor area, string controllerName)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Name = controllerName ?? throw new ArgumentNullException(nameof(controllerName));
            Actions = new KeyedCollection<ActionDescriptor>(action => new { action.Name, Parameters = string.Join(",", action.Parameters.Select(param => param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))) });
        }

        public AreaDescriptor Area { get; }

        public string Name { get; }

        public KeyedCollection<ActionDescriptor> Actions { get; }
    }
}
