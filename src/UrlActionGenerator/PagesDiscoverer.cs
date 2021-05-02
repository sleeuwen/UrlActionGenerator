using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using UrlActionGenerator.Descriptors;
using UrlActionGenerator.Extensions;

namespace UrlActionGenerator
{
    public static class PagesDiscoverer
    {
        public static IEnumerable<PageAreaDescriptor> DiscoverAreaPages(Compilation compilation, IEnumerable<AdditionalText> additionalFiles, AnalyzerConfigOptions configOptions)
        {
            var pages = DiscoverRazorPages(additionalFiles, compilation, configOptions);

            foreach (var page in pages)
            {
                SourceGenerator.Log(compilation, "area:" + page.Area + ", page:"  + page.Page + ", route:" + page.Route + ", model:" + page.Model);
            }

            var usingsByDirectory = GatherImplicitUsings(pages);

            foreach (var group in pages.GroupBy(page => page.Area))
            {
                var area = new PageAreaDescriptor(group.Key);

                foreach (var page in group)
                {
                    var model = GetPageModel(page, compilation, usingsByDirectory);

                    var modelParameters = DiscoverModelParameters(model, compilation).ToList();
                    var modelParameterNames = modelParameters.Select(param => param.Name).ToList();

                    var routeParameters = DiscoverRouteParameters(page.Route).ExceptBy(modelParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase).ToList();

                    foreach (var (pageHandler, method) in DiscoverMethods(model, compilation))
                    {
                        var methodParameters = DiscoverMethodParameters(method).ToList();
                        var methodParameterNames = methodParameters.Select(param => param.Name).ToList();

                        var folder = GetAreaFolder(page, area);
                        folder.Pages.Add(new PageDescriptor(
                            area,
                            page.Page,
                            pageHandler,
                            routeParameters.ExceptBy(methodParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase)
                                .Concat(methodParameters)
                                .Concat(modelParameters.ExceptBy(methodParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase))
                                .ToList()));
                    }
                }

                SourceGenerator.Log(compilation, "Found: '" + area.Name + "', " + area.Pages.Count + " pages and " + area.Folders.Count + " folders");
                yield return area;
            }
        }

        private static List<PageData> DiscoverRazorPages(IEnumerable<AdditionalText> additionalFiles, Compilation compilation, AnalyzerConfigOptions configOptions)
        {
            // Discover .cshtml files in AdditionalFiles.
            var cshtmlFiles = additionalFiles
                .Where(file => file.Path.EndsWith(".cshtml"))
                .ToList();

            // If no .cshtml in AdditionalFiles, try get MSBuildProjectDirectory and look in there.
            if (!cshtmlFiles.Any() && configOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory) && !string.IsNullOrEmpty(projectDirectory))
            {
                cshtmlFiles = Directory.EnumerateFiles(projectDirectory, "*.cshtml", SearchOption.AllDirectories)
                    .Select(file => (AdditionalText)new FileSystemAdditionalText(file.Substring(projectDirectory.Length), projectDirectory))
                    .ToList();
            }

            // If still no .cshtml files and SourceReferenceResolver contains a BaseDirectory, look in there.
            if (!cshtmlFiles.Any() && compilation.Options.SourceReferenceResolver is SourceFileResolver resolver)
            {
                var baseDirectory = resolver.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    cshtmlFiles = Directory.EnumerateFiles(baseDirectory, "*.cshtml", SearchOption.AllDirectories)
                        .Select(file => (AdditionalText)new FileSystemAdditionalText(file.Substring(baseDirectory.Length), baseDirectory))
                        .ToList();
                }
            }

            var pages = cshtmlFiles
                .Where(PagesFacts.IsRazorPage)
                .Select(file => new PageData(file))
                .ToList();

            return pages;
        }

        private static Dictionary<string, List<string>> GatherImplicitUsings(List<PageData> pages)
        {
            return pages
                .Where(PagesFacts.IsImplicitlyIncludedFile)
                .GroupBy(page => page.Folder)
                .ToDictionary(g => g.Key, g => g.SelectMany(PagesFacts.ExtractUsings).Distinct().ToList());
        }

        private static INamedTypeSymbol GetPageModel(PageData page, Compilation compilation, Dictionary<string, List<string>> usingsByDirectory)
        {
            if (page.Model == null)
                return null;

            // First try to get the page model based on the usings in this file
            var explicitUsings = PagesFacts.ExtractUsings(page);
            var pageModel = FindPageModel(compilation, page.Model, explicitUsings);
            if (pageModel != null)
                return pageModel;

            // Walk up the path and try to get the page model based on usings in the implicitly included files
            foreach (var path in EnumerateUpPath(page.AdditionalText.Path))
            {
                if (usingsByDirectory.TryGetValue(path, out var usings))
                {
                    pageModel = FindPageModel(compilation, page.Model, usings);
                    if (pageModel != null)
                        return pageModel;
                }
            }

            // Last try to get the page model based on what is in @model without a using
            return FindPageModel(compilation, page.Model, new List<string> { "" });

            static INamedTypeSymbol FindPageModel(Compilation compilation, string model, IEnumerable<string> usings)
            {
                return usings
                    .Select(@using => compilation.GetTypeByMetadataName($"{@using}{model}"))
                    .FirstOrDefault(symbol => symbol != null);
            }
        }

        private static IPagesFoldersDescriptor GetAreaFolder(PageData page, PageAreaDescriptor area)
        {
            var folders = page.Folder.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            IPagesFoldersDescriptor currentFolder = area;
            foreach (var folderName in folders)
            {
                var folder = currentFolder.Folders.FirstOrDefault(f => f.Name == folderName);
                if (folder == null)
                {
                    folder = new PageFolderDescriptor(area, folderName);
                    currentFolder.Folders.Add(folder);
                }

                currentFolder = folder;
            }

            return currentFolder;
        }

        private static IEnumerable<string> EnumerateUpPath(string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                yield return path;
                path = Path.GetDirectoryName(path);
            }
        }

        internal static IEnumerable<ParameterDescriptor> DiscoverRouteParameters(string route)
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
        internal static IEnumerable<ParameterDescriptor> DiscoverModelParameters(INamedTypeSymbol model, Compilation compilation)
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

        private static readonly Regex _methodNameRegex = new Regex(@"^On(?:Get|Put|Post|Delete|Head|Options|Trace|Patch|Connect)(.+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static IEnumerable<(string PageHandler, IMethodSymbol Method)> DiscoverMethods(INamedTypeSymbol model, Compilation compilation)
        {
            if (model == null)
            {
                yield return (null, null);
                yield break;
            }

            foreach (var method in model.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.IsGenericMethod)
                    continue;

                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                if (method.DeclaredAccessibility != Accessibility.Public)
                    continue;

                var match = _methodNameRegex.Match(method.Name);
                if (!match.Success) continue;

                var pageHandler = match.Groups[1].Value;
                if (pageHandler?.EndsWith("Async") ?? false)
                    pageHandler = pageHandler.Substring(0, pageHandler.Length - 5);
                if (string.IsNullOrWhiteSpace(pageHandler))
                    pageHandler = null;

                yield return (pageHandler, method);
            }
        }

        internal static IEnumerable<ParameterDescriptor> DiscoverMethodParameters(IMethodSymbol method)
        {
            if (method == null) yield break;

            foreach (var param in method.Parameters)
            {
                yield return new ParameterDescriptor(
                    param.Name,
                    param.Type.GetTypeName(),
                    param.HasExplicitDefaultValue,
                    param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null);
            }
        }

        private static string GetConstraintParameterType(string[] constraints)
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
