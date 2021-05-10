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

            var controllersByArea = controllerTypes.GroupBy(GetAreaName);

            foreach (var areaControllers in controllersByArea)
            {
                var area = new AreaDescriptor(areaControllers.Key);

                foreach (var controllerSymbol in areaControllers)
                {
                    var controller = DiscoverControllerActions(controllerSymbol, area, disposableDispose);

                    if (controller.Actions.Any())
                        area.Controllers.Add(controller);
                }

                if (area.Controllers.Any())
                    yield return area;
            }
        }

        private static readonly string[] RouteAttributeTypes = {
            "Microsoft.AspNetCore.Mvc.RouteAttribute", "Microsoft.AspNetCore.Mvc.HttpDeleteAttribute",
            "Microsoft.AspNetCore.Mvc.HttpGetAttribute", "Microsoft.AspNetCore.Mvc.HttpHeadAttribute",
            "Microsoft.AspNetCore.Mvc.HttpOptionsAttribute", "Microsoft.AspNetCore.Mvc.HttpPatchAttribute",
            "Microsoft.AspNetCore.Mvc.HttpPostAttribute", "Microsoft.AspNetCore.Mvc.HttpPutAttribute",
        };
        public static ControllerDescriptor DiscoverControllerActions(INamedTypeSymbol controllerSymbol, AreaDescriptor area, IMethodSymbol disposableDispose)
        {
            var controllerName = RouteDiscoverer.DiscoverControllerName(controllerSymbol);
            var controller = new ControllerDescriptor(area, controllerName);

            var controllerRouteParameters = controllerSymbol.GetAttributes(inherit: true)
                .Where(attr => attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.RouteAttribute")
                .Select(attr => (string)attr.ConstructorArguments.Single().Value)
                .ToList();

            foreach (var actionSymbol in DiscoverActions(controllerSymbol, disposableDispose))
            {
                var actionName = RouteDiscoverer.DiscoverActionName(actionSymbol);
                var action = new ActionDescriptor(controller, actionName);

                var routes = actionSymbol.GetAttributes(inherit: true)
                    .Where(attr => RouteAttributeTypes.Contains(attr.AttributeClass.GetFullNamespacedName()) && attr.ConstructorArguments.Length == 1)
                    .Select(attr => (string)attr.ConstructorArguments.Single().Value)
                    .ToList();

                var routeParameters = new KeyedCollection<ParameterDescriptor>(param => param.Name);
                foreach (var parameter in routes.SelectMany(RouteDiscoverer.DiscoverRouteParameters))
                    routeParameters.Add(parameter);
                foreach (var parameter in controllerRouteParameters.SelectMany(RouteDiscoverer.DiscoverRouteParameters))
                    routeParameters.Add(parameter);

                var methodParameters = RouteDiscoverer.DiscoverMethodParameters(actionSymbol).ToList();
                var methodParameterNames = methodParameters.Select(param => param.Name).ToList();

                action.Parameters.AddRange(routeParameters.ExceptBy(methodParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase));
                action.Parameters.AddRange(methodParameters);

                controller.Actions.Add(action);
            }

            return controller;
        }

        public static List<INamedTypeSymbol> DiscoverControllers(IEnumerable<ISymbol> symbols)
        {
            return symbols
                .OfType<INamedTypeSymbol>()
                .Where(MvcFacts.IsController)
                .Distinct() // partial classes register as duplicate symbols
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
                .SingleOrDefault();

            static bool IsAreaAttribute(AttributeData attribute)
                => attribute.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.AreaAttribute";
        }
    }
}
