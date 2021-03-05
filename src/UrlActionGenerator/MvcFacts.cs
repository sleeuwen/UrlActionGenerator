using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UrlActionGenerator
{
    internal static class MvcFacts
    {
        public static bool CanBeController(TypeDeclarationSyntax typeSyntax)
        {
            _ = typeSyntax ?? throw new ArgumentNullException(nameof(typeSyntax));

            if (typeSyntax is not ClassDeclarationSyntax classSyntax)
                return false;

            var isAbstract = classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AbstractKeyword));
            if (isAbstract)
                return false;

            var isPublic = classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));
            if (!isPublic)
                return false;

            if (classSyntax.TypeParameterList?.Parameters.Count > 0)
                return false;

            return true;
        }

        public static bool IsController(INamedTypeSymbol type)
        {
            type = type ?? throw new ArgumentNullException(nameof(type));

            if (type.TypeKind != TypeKind.Class)
            {
                return false;
            }

            if (type.IsAbstract)
            {
                return false;
            }

            // We only consider public top-level classes as controllers.
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            if (type.ContainingType != null)
            {
                return false;
            }

            if (type.IsGenericType || type.IsUnboundGenericType)
            {
                return false;
            }

            if (type.HasAttribute("Microsoft.AspNetCore.Mvc.NonControllerAttribute", inherit: true))
            {
                return false;
            }

            var hasControllerSuffix = type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
            var hasControllerAttribute = type.HasAttribute("Microsoft.AspNetCore.Mvc.ControllerAttribute", inherit: true);

            if (!hasControllerSuffix && !hasControllerAttribute)
            {
                return false;
            }

            return true;
        }

        public static bool IsControllerAction(IMethodSymbol method, IMethodSymbol disposableDispose)
        {
            method = method ?? throw new ArgumentNullException(nameof(method));

            if (method.MethodKind != MethodKind.Ordinary)
            {
                return false;
            }

            if (method.HasAttribute("Microsoft.AspNetCore.Mvc.NonActionAttribute", inherit: true))
            {
                return false;
            }

            // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (GetDeclaringType(method).SpecialType == SpecialType.System_Object)
            {
                return false;
            }

            if (IsIDisposableDispose(method, disposableDispose))
            {
                return false;
            }

            if (method.IsStatic)
            {
                return false;
            }

            if (method.IsAbstract)
            {
                return false;
            }

            if (method.IsGenericMethod)
            {
                return false;
            }

            return method.DeclaredAccessibility == Accessibility.Public;
        }

        private static bool HasAttribute(this ITypeSymbol typeSymbol, string attribute, bool inherit)
            => GetAttributes(typeSymbol, attribute, inherit).Any();

        private static bool HasAttribute(this IMethodSymbol methodSymbol, string attribute, bool inherit)
            => GetAttributes(methodSymbol, attribute, inherit).Any();

        private static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string attribute)
        {
            foreach (var declaredAttribute in symbol.GetAttributes())
            {
                var fullName = GetFullName(declaredAttribute.AttributeClass);

                if (fullName == attribute)
                {
                    yield return declaredAttribute;
                }
            }
        }

        private static IEnumerable<AttributeData> GetAttributes(this IMethodSymbol methodSymbol, string attribute, bool inherit)
        {
            attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

            IMethodSymbol? current = methodSymbol;
            while (current != null)
            {
                foreach (var attributeData in GetAttributes(current, attribute))
                {
                    yield return attributeData;
                }

                if (!inherit)
                {
                    break;
                }

                current = current.IsOverride ? current.OverriddenMethod : null;
            }
        }

        private static IEnumerable<AttributeData> GetAttributes(this ITypeSymbol typeSymbol, string attribute, bool inherit)
        {
            typeSymbol = typeSymbol ?? throw new ArgumentNullException(nameof(typeSymbol));
            attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

            foreach (var type in GetTypeHierarchy(typeSymbol))
            {
                foreach (var attributeData in GetAttributes(type, attribute))
                {
                    yield return attributeData;
                }

                if (!inherit)
                {
                    break;
                }
            }
        }

        private static IEnumerable<ITypeSymbol> GetTypeHierarchy(this ITypeSymbol? typeSymbol)
        {
            while (typeSymbol != null)
            {
                yield return typeSymbol;

                typeSymbol = typeSymbol.BaseType;
            }
        }

        private static bool IsAssignableFrom(this ITypeSymbol source, ITypeSymbol target)
        {
            source = source ?? throw new ArgumentNullException(nameof(source));
            target = target ?? throw new ArgumentNullException(nameof(target));

            if (SymbolEqualityComparer.Default.Equals(source, target))
            {
                return true;
            }

            if (source.TypeKind == TypeKind.Interface)
            {
                foreach (var @interface in target.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(source, @interface))
                    {
                        return true;
                    }
                }

                return false;
            }

            foreach (var type in target.GetTypeHierarchy())
            {
                if (SymbolEqualityComparer.Default.Equals(source, type))
                {
                    return true;
                }
            }

            return false;
        }


        private static INamedTypeSymbol GetDeclaringType(IMethodSymbol method)
        {
            while (method.IsOverride)
            {
                if (method.OverriddenMethod == null)
                {
                    throw new ArgumentNullException(nameof(method.OverriddenMethod));
                }

                method = method.OverriddenMethod;
            }

            return method.ContainingType;
        }

        private static bool IsIDisposableDispose(IMethodSymbol method, IMethodSymbol disposableDispose)
        {
            if (method.Name != disposableDispose.Name)
            {
                return false;
            }

            if (!method.ReturnsVoid)
            {
                return false;
            }

            if (method.Parameters.Length != disposableDispose.Parameters.Length)
            {
                return false;
            }

            // Explicit implementation
            for (var i = 0; i < method.ExplicitInterfaceImplementations.Length; i++)
            {
                if (method.ExplicitInterfaceImplementations[i].ContainingType.SpecialType == SpecialType.System_IDisposable)
                {
                    return true;
                }
            }

            var implementedMethod = method.ContainingType.FindImplementationForInterfaceMember(disposableDispose);
            return SymbolEqualityComparer.Default.Equals(implementedMethod, method);
        }

        private static string GetFullName(INamespaceOrTypeSymbol typeSymbol) => typeSymbol switch
        {
            { IsNamespace: false } => GetFullName(typeSymbol.ContainingNamespace),
            { Name: var name } => (GetFullName(typeSymbol.ContainingNamespace) + "." + name).TrimStart('.'),
            _ => "",
        };
    }
}
