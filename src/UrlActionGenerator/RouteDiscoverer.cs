using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            var parameterDescriptions = DiscoverMethodParameterDescriptions(methodSymbol);

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

                var paramDescription = parameterDescriptions.TryGetValue(param.Name, out var description)
                    ? description
                    : null;

                yield return new ParameterDescriptor(
                    param.Name,
                    param.Type.GetTypeName(),
                    param.HasExplicitDefaultValue,
                    param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null,
                    paramDescription);
            }
        }

        private static Dictionary<string, string> DiscoverMethodParameterDescriptions(IMethodSymbol methodSymbol)
        {
            var parameterDescriptions = new Dictionary<string, string>();

            foreach (var location in methodSymbol.Locations)
            {
                var methodDeclaration = location.SourceTree.GetRoot().FindNode(location.SourceSpan);
                if (!methodDeclaration.HasLeadingTrivia)
                    continue;

                var trivia = methodDeclaration.GetLeadingTrivia();

                foreach (var (paramName, comment) in ExtractCommentTriviaParamComments(trivia))
                {
                    parameterDescriptions[paramName] = comment;
                }

                foreach (var (paramName, comment) in ExtractStructuredTriviaParamComments(trivia))
                {
                    parameterDescriptions[paramName] = comment;
                }
            }

            return parameterDescriptions;
        }

        private static IEnumerable<(string Name, string Comment)> ExtractStructuredTriviaParamComments(SyntaxTriviaList triviaList)
        {
            var structuredTrivia = triviaList.Where(x => x.HasStructure);
            if (!structuredTrivia.Any())
                yield break;

            foreach (var trivia in structuredTrivia)
            {
                var paramElements = trivia.GetStructure()
                    ?.ChildNodes().OfType<XmlElementSyntax>()
                    .Where(x => x.StartTag.Name.ToString() == "param") ?? Enumerable.Empty<XmlElementSyntax>();

                foreach (var paramElement in paramElements)
                {
                    var nameAttribute = paramElement.StartTag.Attributes.FirstOrDefault(x => x.Name.ToString() == "name") as XmlNameAttributeSyntax;

                    var paramName = nameAttribute.Identifier.ToString();
                    var comment = paramElement.Content.FirstOrDefault()?.ToString();

                    if (!string.IsNullOrWhiteSpace(paramName))
                    {
                        yield return (paramName, comment);
                    }
                }
            }
        }

        private static IEnumerable<(string Name, string Comment)> ExtractCommentTriviaParamComments(SyntaxTriviaList triviaList)
        {
            var commentTrivia = triviaList.Where(x => x.IsKind(SyntaxKind.SingleLineCommentTrivia));
            if (!commentTrivia.Any())
                yield break;

            var commentString = string.Join("\n", commentTrivia.Select(x => x.ToString().TrimStart('/').Trim()));
            if (!commentString.Contains("<param"))
                yield break;

            var doc = new XmlDocument();
            doc.LoadXml($"<root>{commentString}</root>");

            var paramNodes = doc.SelectNodes("/root/param[@name]");
            foreach (XmlElement paramNode in paramNodes)
            {
                var nameAttribute = paramNode.Attributes.OfType<XmlAttribute>().First(x => x.Name == "name");

                var paramName = nameAttribute.Value;
                var comment = paramNode.InnerText;

                if (!string.IsNullOrWhiteSpace(paramName))
                    yield return (paramName, comment);
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
                    null,
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
                    member.Type.GetTypeName(),
                    true,
                    null,
                    null); // TODO: Member summary xml doc
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
