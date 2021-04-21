using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    internal static class PagesFacts
    {
        public static bool IsRazorPage(AdditionalText file)
        {
            var text = file.GetText();
            var content = text.ToString();

            return content.Contains("@page");
        }

        public static bool IsImplicitlyIncludedFile(PageData page)
        {
            return string.Equals(page.PageName, "_ViewStart", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(page.PageName, "_ViewImports", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly Regex _usingRe = new Regex(@"(?:^|\s)@using ([\w\.]+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static List<string> ExtractUsings(PageData page)
        {
            var content = page.AdditionalText.GetText().ToString();

            var usings = new List<string>();
            foreach (Match match in _usingRe.Matches(content))
            {
                usings.Add(match.Groups[1].Value);
            }

            return usings;
        }
    }
}
