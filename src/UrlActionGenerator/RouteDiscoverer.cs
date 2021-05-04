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
                yield return new ParameterDescriptor(
                    param.Name,
                    param.Type.GetTypeName(),
                    param.HasExplicitDefaultValue,
                    param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null);
            }
        }

        public static IEnumerable<ParameterDescriptor> DiscoverRouteParameters(string route)
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

                yield return new ParameterDescriptor(
                    name,
                    GetConstraintParameterType(constraints) + (nullable ? "?" : ""),
                    false,
                    null);
            }
        }

        private static INamedTypeSymbol _bindPropertyAttribute;
        private static INamedTypeSymbol _fromQueryAttribute;
        public static IEnumerable<ParameterDescriptor> DiscoverModelParameters(INamedTypeSymbol model, Compilation compilation)
        {
            if (model == null) yield break;

            _bindPropertyAttribute ??= compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.BindPropertyAttribute");
            _fromQueryAttribute ??= compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.FromQueryAttribute");

            foreach (var member in model.GetMembers().OfType<IPropertySymbol>())
            {
                var attribute = member.GetAttributes().FirstOrDefault(attr =>
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _bindPropertyAttribute) ||
                    SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _fromQueryAttribute));

                if (attribute == null)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _bindPropertyAttribute))
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
                    member.Type.GetTypeName(),
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
                    return "string";
                if (stringFunctions.Any(fun => constraint.StartsWith(fun)))
                    return "string";

                if (intFunctions.Any(fun => constraint.StartsWith(fun)))
                    type = "int";

                if (constraint is "int" or "long" or "float" or "double" or "decimal" or "bool")
                    return constraint;
                if (constraint == "guid")
                    return "System.Guid";
                if (constraint == "datetime")
                    return "System.DateTime";
            }

            return type ?? "string";
        }
    }
}
