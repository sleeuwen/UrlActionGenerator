using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    public static class MvcDiscoverer
    {
        public static IEnumerable<AreaDescriptor> DiscoverAreaControllerActions(Compilation compilation)
        {
            var allClasses = compilation.SyntaxTrees
                .SelectMany(st => st.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                .Select(st => compilation.GetSemanticModel(st.SyntaxTree).GetDeclaredSymbol(st));

            var controllerTypes = DiscoverControllers(allClasses);

            var controllersByArea = controllerTypes.GroupBy(GetAreaName);

            foreach (var areaControllers in controllersByArea)
            {
                var area = new AreaDescriptor(areaControllers.Key);

                foreach (var controllerSymbol in areaControllers)
                {
                    var controller = DiscoverControllerActions(controllerSymbol, area);

                    if (controller.Actions.Any())
                        area.Controllers.Add(controller);
                }

                if (area.Controllers.Any())
                    yield return area;
            }
        }

        public static ControllerDescriptor DiscoverControllerActions(ITypeSymbol controllerSymbol, AreaDescriptor area)
        {
            var controllerName = Regex.Replace(controllerSymbol.Name, "Controller$", "");
            var controller = new ControllerDescriptor(area, controllerName);

            foreach (var actionSymbol in DiscoverActions(controllerSymbol))
            {
                var actionName = Regex.Replace(actionSymbol.Name, "Async$", "");
                var action = new ActionDescriptor(controller, actionName);

                foreach (var parameterSymbol in actionSymbol.Parameters)
                {
                    action.Parameters.Add(new ParameterDescriptor(
                        parameterSymbol.Name,
                        GetParameterType(parameterSymbol.Type),
                        parameterSymbol.HasExplicitDefaultValue,
                        parameterSymbol.HasExplicitDefaultValue ? parameterSymbol.ExplicitDefaultValue : null));
                }

                controller.Actions.Add(action);
            }

            return controller;
        }

        public static IList<ITypeSymbol> DiscoverControllers(IEnumerable<ISymbol> symbols)
        {
            return symbols
                .OfType<ITypeSymbol>()
                .Where(IsAController)
                .ToList();

            static bool IsAController(ITypeSymbol typeSymbol)
                => GetAttributes(typeSymbol, true).Any(attr => attr.AttributeClass?.Name == "ControllerAttribute" && GetFullNamespace(attr.AttributeClass) == "Microsoft.AspNetCore.Mvc");
        }

        public static IList<IMethodSymbol> DiscoverActions(ITypeSymbol controllerSymbol)
        {
            return controllerSymbol.GetMembers().OfType<IMethodSymbol>()
                .Where(IsValidAction)
                .ToList();

            static bool IsValidAction(IMethodSymbol methodSymbol)
                => !methodSymbol.IsAbstract
                   && methodSymbol.Name != ".ctor"
                   && !methodSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "NonActionAttribute" && GetFullNamespace(attr.AttributeClass) == "Microsoft.AspNetCore.Mvc");
        }

        public static string GetAreaName(ITypeSymbol symbol)
        {
            return GetAttributes(symbol, true)
                .Where(IsAreaAttribute)
                .Select(attr => (string)attr.ConstructorArguments.Single().Value)
                .SingleOrDefault() ?? "";

            static bool IsAreaAttribute(AttributeData attribute)
                => attribute.AttributeClass?.Name == "AreaAttribute" && GetFullNamespace(attribute.AttributeClass) == "Microsoft.AspNetCore.Mvc";
        }

        public static IEnumerable<AttributeData> GetAttributes(ITypeSymbol? typeSymbol, bool recursive = false)
        {
            do
            {
                if (typeSymbol == null)
                    yield break;

                foreach (var attributeData in typeSymbol.GetAttributes().Where(attributeData => attributeData != null))
                    yield return attributeData;

                typeSymbol = typeSymbol.BaseType;
            } while (recursive);
        }

        public static string GetFullNamespace(INamespaceOrTypeSymbol typeSymbol) => typeSymbol switch
        {
            { IsNamespace: false } => GetFullNamespace(typeSymbol.ContainingNamespace),
            { Name: var name } => (GetFullNamespace(typeSymbol.ContainingNamespace) + "." + name).TrimStart('.'),
            _ => "",
        };

        public static string GetParameterType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
                return $"{GetParameterType(arrayType.ElementType)}[]";

            var rootType = GetTypeName(type);

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                rootType += "<";
                rootType += string.Join(", ", namedType.TypeArguments.Select(GetParameterType));
                rootType += ">";
            }

            return rootType;
        }

        public static string GetTypeName(ITypeSymbol type) => (GetFullNamespace(type) + "." + type.Name) switch
        {
            "System.String" => "string",
            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.UInt16" => "ushort",
            "System.UInt32" => "uint",
            "System.UInt64" => "ulong",
            "System.Boolean" => "bool",
            "System.Decimal" => "decimal",
            var t => t,
        };
    }
}
