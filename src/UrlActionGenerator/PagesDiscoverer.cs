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

            var disposableDispose = (IMethodSymbol)compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers(nameof(IDisposable.Dispose)).First();

            foreach (var group in pages.GroupBy(page => page.Area))
            {
                var area = new PageAreaDescriptor(group.Key);

                foreach (var page in group)
                {
                    var model = GetPageModel(page, compilation, usingsByDirectory);

                    var modelParameters = RouteDiscoverer.DiscoverModelParameters(model, compilation).ToList();
                    var modelParameterNames = modelParameters.Select(param => param.Name).ToList();

                    var routeParameters = RouteDiscoverer.DiscoverRouteParameters(page.Route).ToList();
                    routeParameters = routeParameters.ExceptBy(modelParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase).ToList();

                    foreach (var (pageHandler, method) in DiscoverMethods(model, disposableDispose))
                    {
                        var methodParameters = RouteDiscoverer.DiscoverMethodParameters(method).ToList();
                        var methodParameterNames = methodParameters.Select(param => param.Name).ToList();

                        var folder = area.GetFolder(page.Folder);
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

        private static IEnumerable<string> EnumerateUpPath(string path)
        {
            while (!string.IsNullOrEmpty(path))
            {
                yield return path;
                path = Path.GetDirectoryName(path);
            }
        }

        private static readonly Regex _methodNameRegex = new Regex(@"^On(?:Get|Put|Post|Delete|Head|Options|Trace|Patch|Connect)(.+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static IEnumerable<(string PageHandler, IMethodSymbol Method)> DiscoverMethods(INamedTypeSymbol model, IMethodSymbol disposableDispose)
        {
            if (model == null)
            {
                yield return (null, null); // no model, but there is a .cshtml with @page
                yield break;
            }

            foreach (var method in model.GetMembers().OfType<IMethodSymbol>())
            {
                if (!PagesFacts.IsPageMethod(method, disposableDispose))
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
    }
}
