using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UrlActionGenerator.Descriptors;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    internal static class MvcDiscoverer
    {
        public static IEnumerable<AreaDescriptor> DiscoverAreaControllerActions(MySyntaxReceiver syntaxReceiver)
        {
            var controllersByArea = syntaxReceiver.Controllers.GroupBy(GetAreaName);

            foreach (var areaControllers in controllersByArea)
            {
                var area = new AreaDescriptor(areaControllers.Key);

                foreach (var controllerSymbol in areaControllers)
                {
                    if (syntaxReceiver.ControllerActions.TryGetValue(controllerSymbol, out var actions))
                    {
                        var controller = GetControllerDescriptor(area, controllerSymbol, actions);
                        area.Controllers.Add(controller);
                    }
                }

                if (area.Controllers.Any())
                    yield return area;
            }
        }

        private static ControllerDescriptor GetControllerDescriptor(AreaDescriptor area, INamedTypeSymbol controllerSymbol, IEnumerable<IMethodSymbol> actions)
        {
            var controllerName = RouteDiscoverer.DiscoverControllerName(controllerSymbol);
            var controller = new ControllerDescriptor(area, controllerName);

            foreach (var actionSymbol in actions)
            {
                var actionName = RouteDiscoverer.DiscoverActionName(actionSymbol);
                var action = new ActionDescriptor(controller, actionName);

                foreach (var parameter in RouteDiscoverer.DiscoverMethodParameters(actionSymbol))
                    action.Parameters.Add(parameter);

                controller.Actions.Add(action);
            }

            return controller;
        }

        public static string GetAreaName(ITypeSymbol symbol)
        {
            return symbol.GetAttributes(inherit: true)
                .Where(IsAreaAttribute)
                .Select(attr => (string)attr.ConstructorArguments.Single().Value)
                .SingleOrDefault() ?? "";

            static bool IsAreaAttribute(AttributeData attribute)
                => attribute.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.AreaAttribute";
        }
    }
}
