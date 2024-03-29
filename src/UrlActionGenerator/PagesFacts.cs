using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    internal static class PagesFacts
    {
        public static bool IsPageMethod(IMethodSymbol method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

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

            if (!method.Name.StartsWith("OnDelete", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnGet", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnHead", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnOptions", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnPatch", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnPost", StringComparison.OrdinalIgnoreCase)
                && !method.Name.StartsWith("OnPut", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (var attribute in method.GetAttributes(inherit: true))
            {
                if (attribute.AttributeClass.GetFullNamespacedName() == "Microsoft.AspNetCore.Mvc.RazorPages.NonHandlerAttribute")
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsRazorPage(AdditionalText file)
        {
            var text = file.GetText();
            var content = text.ToString();

            return content.Contains("@page");
        }

        public static bool IsImplicitlyIncludedFile(RazorPageItem razorPage)
        {
            return string.Equals(razorPage.PageName, "_ViewStart", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(razorPage.PageName, "_ViewImports", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly Regex _usingRe = new Regex(@"(?:^|\s)@using ([\w\.]+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static List<string> ExtractUsings(RazorPageItem razorPage)
        {
            var content = razorPage.AdditionalText.GetText().ToString();

            var usings = new List<string>();
            foreach (Match match in _usingRe.Matches(content))
            {
                usings.Add(match.Groups[1].Value);
            }

            return usings;
        }
    }
}
