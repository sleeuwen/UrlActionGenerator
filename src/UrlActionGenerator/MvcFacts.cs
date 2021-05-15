using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UrlActionGenerator.Extensions;

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

            if (type.GetAttributes(inherit: true).Any(attr => attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.NonControllerAttribute"))
            {
                return false;
            }

            var hasControllerSuffix = type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
            var hasControllerAttribute = type.GetAttributes(inherit: true).Any(attr => attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.ControllerAttribute");

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

            if (method.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (method.GetDeclaringType().SpecialType == SpecialType.System_Object)
            {
                return false;
            }

            if (IsIDisposableDispose(method, disposableDispose))
            {
                return false;
            }

            if (method.GetAttributes(inherit: true).Any(attr => attr.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.NonActionAttribute"))
            {
                return false;
            }

            return true;
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
    }
}
