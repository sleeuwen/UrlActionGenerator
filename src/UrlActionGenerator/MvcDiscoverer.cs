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

        public static IList<INamedTypeSymbol> DiscoverControllers(IEnumerable<ISymbol> symbols)
        {
            return symbols
                .OfType<INamedTypeSymbol>()
                .Where(MvcFacts.IsController)
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
            return GetAttributes(symbol, true)
                .Where(IsAreaAttribute)
                .Select(attr => (string)attr.ConstructorArguments.Single().Value)
                .SingleOrDefault() ?? "";

            static bool IsAreaAttribute(AttributeData attribute)
                => GetFullNamespacedTypeName(attribute.AttributeClass) == "Microsoft.AspNetCore.Mvc.AreaAttribute";
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

        public static string GetFullNamespacedTypeName(INamespaceOrTypeSymbol typeSymbol)
        {
            var fullName = new System.Text.StringBuilder();

            while (typeSymbol != null)
            {
                if (!string.IsNullOrEmpty(typeSymbol.Name))
                {
                    if (fullName.Length > 0)
                        fullName.Insert(0, '.');
                    fullName.Insert(0, typeSymbol.Name);
                }
                typeSymbol = typeSymbol.ContainingSymbol as INamespaceOrTypeSymbol;
            }

            return fullName.ToString();
        }

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

        public static string GetTypeName(ITypeSymbol type) => (GetFullNamespacedTypeName(type)) switch
        {
            "System.String" => "string",
            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Char" => "char",
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.UInt16" => "ushort",
            "System.UInt32" => "uint",
            "System.UInt64" => "ulong",
            "System.Boolean" => "bool",
            "System.Decimal" => "decimal",
            "System.Double" => "double",
            "System.Float" => "float",
            "System.Object" => "object",
            var t => t,
        };
    }
}
