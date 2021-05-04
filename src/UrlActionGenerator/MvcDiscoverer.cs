using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator.Descriptors;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    public static class MvcDiscoverer
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

        public static ControllerDescriptor DiscoverControllerActions(ITypeSymbol controllerSymbol, AreaDescriptor area, IMethodSymbol disposableDispose)
        {
            var controllerName = Regex.Replace(controllerSymbol.Name, "Controller$", "");
            var controller = new ControllerDescriptor(area, controllerName);

            foreach (var actionSymbol in DiscoverActions(controllerSymbol, disposableDispose))
            {
                var actionName = Regex.Replace(actionSymbol.Name, "Async$", "");
                var action = new ActionDescriptor(controller, actionName);
                action.Parameters.AddRange(RouteDiscoverer.DiscoverMethodParameters(actionSymbol));

                controller.Actions.Add(action);
            }

            return controller;
        }

        public static IList<INamedTypeSymbol> DiscoverControllers(IEnumerable<ISymbol> symbols)
        {
            return symbols
                .OfType<INamedTypeSymbol>()
                .Where(MvcFacts.IsController)
                .Distinct() // partial classes register as duplicate symbols
                .ToList();
        }

        public static IList<IMethodSymbol> DiscoverActions(ITypeSymbol controllerSymbol, IMethodSymbol disposableDispose)
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
