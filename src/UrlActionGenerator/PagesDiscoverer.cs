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

            var allAreas = pages.Select(page => DiscoverAreaPages(page, usingsByDirectory, compilation));

            var grouped = CombineAreas(allAreas);

            return grouped;
        }

        public static IEnumerable<PageAreaDescriptor> CombineAreas(IEnumerable<PageAreaDescriptor> areas)
        {
            var areasByName = new Dictionary<string, PageAreaDescriptor>();
            var foldersByAreaAndName = new Dictionary<string, Dictionary<string, IPagesFoldersDescriptor>>();

            foreach (var area in areas)
            {
                if (!areasByName.TryGetValue(area.Name ?? "", out var currentArea))
                {
                    currentArea = new PageAreaDescriptor(area.Name);
                    areasByName[area.Name ?? ""] = currentArea;
                }

                if (!foldersByAreaAndName.TryGetValue(area.Name ?? "", out var foldersByName))
                {
                    foldersByName = new Dictionary<string, IPagesFoldersDescriptor>();
                    foldersByAreaAndName[area.Name ?? ""] = foldersByName;
                }

                AddPages(area, currentArea, foldersByName);
            }

            return areasByName.Values.ToList();

            static void AddPages(IPagesFoldersDescriptor current, IPagesFoldersDescriptor parent, IDictionary<string, IPagesFoldersDescriptor> foldersByName)
            {
                foreach (var page in current.Pages)
                    parent.Pages.Add(page);

                foreach (var folder in current.Folders)
                {
                    if (!foldersByName.TryGetValue(folder.Path, out var parentFolder))
                    {
                        var area = parent switch
                        {
                            PageAreaDescriptor areaDescriptor => areaDescriptor,
                            PageFolderDescriptor folderDescriptor => folderDescriptor.Area,
                        };

                        parentFolder = new PageFolderDescriptor(area, folder.Name, folder.Path);
                        parent.Folders.Add((PageFolderDescriptor)parentFolder);
                        foldersByName[folder.Path] = parentFolder;
                    }

                    AddPages(folder, parentFolder, foldersByName);
                }
            }
        }

        internal static PageAreaDescriptor DiscoverAreaPages(RazorPageItem page, Dictionary<string, List<string>> usingsByDirectory, Compilation compilation)
        {
            var area = new PageAreaDescriptor(page.Area);

            var model = GetPageModel(page, compilation, usingsByDirectory);

            var modelParameters = RouteDiscoverer.DiscoverModelParameters(model, compilation).ToList();
            var modelParameterNames = modelParameters.Select(param => param.Name).ToList();

            var routeParameters = RouteDiscoverer.DiscoverRouteParameters(page.Route).ToList();
            routeParameters = routeParameters.ExceptBy(modelParameterNames, param => param.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var (pageHandler, method) in DiscoverMethods(model))
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

            return area;
        }

        private static List<RazorPageItem> DiscoverRazorPages(IEnumerable<AdditionalText> additionalFiles, Compilation compilation, AnalyzerConfigOptions configOptions)
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
                .Select(file => new RazorPageItem(file))
                .ToList();

            return pages;
        }

        internal static Dictionary<string, List<string>> GatherImplicitUsings(IList<RazorPageItem> pages)
        {
            return pages
                .Where(PagesFacts.IsImplicitlyIncludedFile)
                .GroupBy(page => page.Folder)
                .ToDictionary(g => g.Key, g => g.SelectMany(PagesFacts.ExtractUsings).Distinct().ToList());
        }

        private static INamedTypeSymbol GetPageModel(RazorPageItem razorPage, Compilation compilation, Dictionary<string, List<string>> usingsByDirectory)
        {
            if (razorPage.Model == null)
                return null;

            // First try to get the page model based on the usings in this file
            var explicitUsings = PagesFacts.ExtractUsings(razorPage);
            var pageModel = FindPageModel(compilation, razorPage.Model, explicitUsings);
            if (pageModel != null)
                return pageModel;

            // Walk up the path and try to get the page model based on usings in the implicitly included files
            foreach (var path in EnumerateUpPath(razorPage.AdditionalText.Path))
            {
                if (usingsByDirectory.TryGetValue(path, out var usings))
                {
                    pageModel = FindPageModel(compilation, razorPage.Model, usings);
                    if (pageModel != null)
                        return pageModel;
                }
            }

            // Last try to get the page model based on what is in @model without a using
            return FindPageModel(compilation, razorPage.Model, new List<string> { "" });

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
        internal static IEnumerable<(string PageHandler, IMethodSymbol Method)> DiscoverMethods(INamedTypeSymbol model)
        {
            if (model == null)
            {
                yield return (null, null); // no model, but there is a .cshtml with @page
                yield break;
            }

            var seenDefaultMethod = false;
            foreach (var method in model.GetMembers().OfType<IMethodSymbol>())
            {
                if (!PagesFacts.IsPageMethod(method))
                    continue;

                var match = _methodNameRegex.Match(method.Name);
                if (!match.Success) continue;

                var pageHandler = match.Groups[1].Value;
                if (pageHandler?.EndsWith("Async") ?? false)
                    pageHandler = pageHandler.Substring(0, pageHandler.Length - 5);
                if (string.IsNullOrWhiteSpace(pageHandler))
                    pageHandler = null;

                if (pageHandler == null && method.Name.StartsWith("OnGet"))
                    seenDefaultMethod = true;

                yield return (pageHandler, method);
            }

            if (!seenDefaultMethod)
                yield return (null, null);
        }
    }
}
