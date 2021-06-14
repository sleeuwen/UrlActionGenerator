using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using UrlActionGenerator.Descriptors;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    public static class RouteDiscoverer
    {
        public static IEnumerable<ParameterDescriptor> DiscoverMethodParameters(IMethodSymbol methodSymbol)
        {
            if (methodSymbol is null)
                yield break;

            foreach (var param in methodSymbol.Parameters)
            {
                if (param.Type.GetFullNamespacedName() == "Microsoft.AspNetCore.Http.IFormFile") // TODO: IEnumerable<IFormFile>
                    continue;

                var parameterAttributes = param.Type.GetAttributes(inherit: true);
                if (parameterAttributes.Any(attr =>
                    attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.FromFormAttribute"
                    || attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.FromBodyAttribute"
                    || attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.FromHeaderAttribute"))
                {
                    continue;
                }

                yield return new ParameterDescriptor(
                    param.Name,
                    param.Type,
                    param.HasExplicitDefaultValue,
                    param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null);
            }
        }

        public static IEnumerable<ParameterDescriptor> DiscoverRouteParameters(string route, Compilation compilation)
        {
            if (string.IsNullOrEmpty(route))
                yield break;

            var matches = Regex.Matches(route, @"{([^}]+)}");

            foreach (Match constraint in matches)
            {
                var match = Regex.Match(constraint.Groups[1].Value, @"^([^:?]+)((?::[^:?]+)*)(\?)?$");

                var name = match.Groups[1].Value;
                var constraints = match.Groups[2].Value.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                var nullable = match.Groups[3].Success;

                var parameterTypeString = GetConstraintParameterType(constraints);
                var parameterType = compilation.GetTypeByMetadataName(parameterTypeString);

                if (nullable)
                    parameterType = compilation.GetTypeByMetadataName("System.Nullable`1").Construct(parameterType);

                yield return new ParameterDescriptor(
                    name,
                    parameterType,
                    false,
                    null);
            }
        }

        public static IEnumerable<ParameterDescriptor> DiscoverModelParameters(INamedTypeSymbol model, Compilation compilation)
        {
            if (model == null) yield break;

            var bindPropertyAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.BindPropertyAttribute");
            var fromQueryAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromQueryAttribute");
            var fromRouteAttribute = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromRouteAttribute");

            foreach (var member in model.GetMembers().OfType<IPropertySymbol>())
            {
                var attribute = member.GetAttributes().FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, bindPropertyAttribute) ||
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, fromQueryAttribute) ||
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, fromRouteAttribute));

                if (attribute == null)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, bindPropertyAttribute))
                {
                    var supportsGet = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "SupportsGet").Value;
                    if (supportsGet.Kind == TypedConstantKind.Error || ((bool)supportsGet.Value) != true)
                        continue;
                }

                var nameArgument = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "Name").Value;
                var parameterName = nameArgument.Kind == TypedConstantKind.Primitive
                    ? (string)nameArgument.Value
                    : member.Name;

                yield return new ParameterDescriptor(
                    parameterName,
                    member.Type,
                    true,
                    null);
            }
        }

        internal static string GetConstraintParameterType(string[] constraints)
        {
            var stringFunctions = new[] { "minlength(", "maxlength(", "length(", "regex(" };
            var intFunctions = new[] { "range(", "min(", "max(" };

            if (constraints.Length == 0 || (constraints.Length == 1 && constraints[0] == "")) return "string";

            var type = (string)null;
            foreach (var constraint in constraints)
            {
                if (constraint == "alpha")
                    return "System.String";
                if (stringFunctions.Any(fun => constraint.StartsWith(fun)))
                    return "System.String";

                if (intFunctions.Any(fun => constraint.StartsWith(fun)))
                    type = "System.Int32";

                if (constraint is "guid")
                    return "System.Guid";
                if (constraint is "datetime")
                    return "System.DateTime";

                if (constraint is "int" or "long" or "float" or "double" or "decimal" or "bool")
                {
                    return constraint switch
                    {
                        "int" => "System.Int32",
                        "long" => "System.Int64",
                        "float" => "System.Single",
                        "double" => "System.Double",
                        "Decimal" => "System.Decimal",
                        "bool" => "System.Boolean",
                    };
                }
            }

            return type ?? "System.String";
        }

        public static string DiscoverControllerName(INamedTypeSymbol controllerSymbol)
        {
            var controllerName = controllerSymbol.Name;
            if (controllerName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

            return controllerName;
        }

        public static string DiscoverActionName(IMethodSymbol methodSymbol)
        {
            var actionNameAttribute = methodSymbol.GetAttributes(inherit: true)
                .SingleOrDefault(attr => attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.ActionNameAttribute");
            if (actionNameAttribute != null)
                return (string)actionNameAttribute.ConstructorArguments.Single().Value;

            var actionName = methodSymbol.Name;
            if (actionName.EndsWith("Async", StringComparison.OrdinalIgnoreCase))
                actionName = actionName.Substring(0, actionName.Length - "Async".Length);

            return actionName;
        }
    }
}
