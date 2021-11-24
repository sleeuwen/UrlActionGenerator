using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator.Descriptors;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    internal static class MvcDiscoverer
    {
        public static IEnumerable<AreaDescriptor> DiscoverAreaControllerActions(Compilation compilation, List<TypeDeclarationSyntax> possibleControllers)
        {
            var allClasses = possibleControllers
                .Select(st => compilation.GetSemanticModel(st.SyntaxTree).GetDeclaredSymbol(st));

            var disposableDispose = (IMethodSymbol)compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers(nameof(IDisposable.Dispose)).First();

            var controllerTypes = DiscoverControllers(allClasses);

            var allAreas = controllerTypes.Select(DiscoverAreaControllerActions);

            var combined = CombineAreas(allAreas);

            return combined;

            // var controllersByArea = controllerTypes.GroupBy(GetAreaName);
            //
            // foreach (var areaControllers in controllersByArea)
            // {
            //     var area = new AreaDescriptor(areaControllers.Key);
            //
            //     foreach (var controllerSymbol in areaControllers)
            //     {
            //         var controller = DiscoverControllerActions(controllerSymbol, area, disposableDispose);
            //
            //         if (controller.Actions.Any())
            //             area.Controllers.Add(controller);
            //     }
            //
            //     if (area.Controllers.Any())
            //         yield return area;
            // }
        }

        public static AreaDescriptor DiscoverAreaControllerActions(INamedTypeSymbol typeSymbol)
        {
            var area = new AreaDescriptor(GetAreaName(typeSymbol));

            var controller = DiscoverControllerActions(typeSymbol, area, null);
            if (controller.Actions.Count > 0)
                area.Controllers.Add(controller);

            return area;
        }

        public static IEnumerable<AreaDescriptor> CombineAreas(IEnumerable<AreaDescriptor> areas)
        {
            var areasByName = new Dictionary<string, AreaDescriptor>();
            var controllersByName = new Dictionary<(string, string), ControllerDescriptor>();

            foreach (var area in areas)
            {
                if (!areasByName.TryGetValue(area.Name, out var currentArea))
                {
                    currentArea = new AreaDescriptor(area.Name);
                    areasByName[area.Name] = currentArea;
                }

                foreach (var controller in area.Controllers)
                {
                    if (!controllersByName.TryGetValue((area.Name, controller.Name), out var currentController))
                    {
                        currentController = new ControllerDescriptor(area, controller.Name);
                        controllersByName[(area.Name, controller.Name)] = currentController;
                    }

                    foreach (var action in controller.Actions)
                    {
                        currentController.Actions.Add(action);
                    }
                }
            }

            return areasByName.Values.ToList();
        }

        public static ControllerDescriptor DiscoverControllerActions(INamedTypeSymbol controllerSymbol, AreaDescriptor area, IMethodSymbol disposableDispose)
        {
            var controllerName = RouteDiscoverer.DiscoverControllerName(controllerSymbol);
            var controller = new ControllerDescriptor(area, controllerName);

            foreach (var actionSymbol in DiscoverActions(controllerSymbol, disposableDispose))
            {
                var actionName = RouteDiscoverer.DiscoverActionName(actionSymbol);
                var action = new ActionDescriptor(controller, actionName);

                foreach (var parameter in RouteDiscoverer.DiscoverMethodParameters(actionSymbol))
                {
                    action.Parameters.Add(parameter);
                }

                controller.Actions.Add(action);
            }

            return controller;
        }

        public static List<INamedTypeSymbol> DiscoverControllers(IEnumerable<ISymbol> symbols)
        {
            return symbols
                .OfType<INamedTypeSymbol>()
                .Where(MvcFacts.IsController)
                .Distinct(SymbolEqualityComparer.Default) // partial classes register as duplicate symbols
                .Cast<INamedTypeSymbol>()
                .ToList();
        }

        public static List<IMethodSymbol> DiscoverActions(ITypeSymbol controllerSymbol, IMethodSymbol disposableDispose)
        {
            return controllerSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(method => MvcFacts.IsControllerAction(method, disposableDispose))
                .ToList();
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
