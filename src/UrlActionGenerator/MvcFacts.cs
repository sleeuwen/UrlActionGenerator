using System;
using System.Collections.Generic;
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

            var isPublic = false;
            foreach (var modifier in classSyntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.AbstractKeyword))
                    return false;

                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                    isPublic = true;
            }

            if (!isPublic)
                return false;

            if (classSyntax.TypeParameterList?.Parameters.Count > 0)
                return false;

            if (classSyntax.AttributeLists.Count == 0 && classSyntax.BaseList?.Types.Count is null or 0)
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

            var hasControllerAttribute = false;

            foreach (var attribute in type.GetAttributes(inherit: true))
            {
                var fullName = attribute.AttributeClass.GetFullNamespacedName();
                if (fullName == "Microsoft.AspNetCore.Mvc.NonControllerAttribute")
                    return false;

                if (fullName == "Microsoft.AspNetCore.Mvc.ControllerAttribute")
                    hasControllerAttribute = true;
            }

            var hasControllerSuffix = type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
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

            foreach (var attribute in method.GetAttributes(inherit: true))
            {
                if (attribute.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.NonActionAttribute")
                    return false;
            }

            return true;
        }

        private static bool IsIDisposableDispose(IMethodSymbol method, IMethodSymbol disposableDispose)
        {
            if (disposableDispose is null)
                return false;

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
