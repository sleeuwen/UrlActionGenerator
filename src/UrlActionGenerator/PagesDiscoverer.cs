using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    public static class PagesDiscoverer
    {
        public static IEnumerable<PageAreaDescriptor> DiscoverAreaPages(Compilation compilation, IEnumerable<AdditionalText> additionalFiles)
        {
            var pages = DiscoverRazorPages(additionalFiles, compilation);

            foreach (var page in pages)
            {
                SourceGenerator.Log(compilation, "area:" + page.Area + ", page:"  + page.Page + ", route:" + page.Route + ", model:" + page.Model);
            }

            var usingsByDirectory = GatherImplicitUsings(pages);

            foreach (var group in pages.GroupBy(page => page.Area))
            {
                var area = new PageAreaDescriptor(group.Key);

                foreach (var page in group.OrderBy(page => page))
                {
                    var model = GetPageModel(page, compilation, usingsByDirectory);

                    var modelParameters = DiscoverModelParameters(model, compilation).ToList();
                    var routeParameters = DiscoverRouteParameters(page.Route).ToList();

                    foreach (var (pageHandler, method) in DiscoverMethods(model, compilation))
                    {
                        var methodParameters = DiscoverMethodParameters(method);

                        var folder = GetAreaFolder(page, area);
                        folder.Pages.Add(new PageDescriptor(
                            area,
                            page.Page,
                            pageHandler,
                            routeParameters.Concat(methodParameters).Concat(modelParameters).ToList()));
                    }
                }

                SourceGenerator.Log(compilation, "Found: '" + area.Name + "', " + area.Pages.Count + " pages and " + area.Folders.Count + " folders");
                yield return area;
            }
        }

        private static List<PageData> DiscoverRazorPages(IEnumerable<AdditionalText> additionalFiles, Compilation compilation)
        {
            var cshtmlFiles = additionalFiles
                .Where(file => file.Path.EndsWith(".cshtml"))
                .ToList();

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
                .Where(file =>
                {
                    var text = file.GetText();
                    var content = text.ToString();

                    return content.Contains("@page");
                })
                .Select(file => new PageData(file))
                .ToList();

            return pages;
        }

        private static Dictionary<string, List<string>> GatherImplicitUsings(List<PageData> pages)
        {
            return pages
                .Where(page => Path.GetFileName(page.Page) == "_ViewStart.cshtml" || Path.GetFileName(page.Page) == "_ViewImports.cshtml")
                .GroupBy(page => Path.GetDirectoryName(page.Page))
                .ToDictionary(g => g.Key, g => g.SelectMany(file =>
                {
                    var content = file.AdditionalText.GetText().ToString();

                    var usings = new List<string>();
                    foreach (Match match in Regex.Matches(content, @"(^|\s)@using [\w\.]+\b", RegexOptions.IgnoreCase))
                    {
                        usings.Add(match.Value.Substring(7));
                    }

                    return usings;
                }).Distinct().ToList());
        }

        private static INamedTypeSymbol GetPageModel(PageData page, Compilation compilation, Dictionary<string, List<string>> usingsByDirectory)
        {
            if (page.Model == null)
                return null;

            var explicitUsings = Regex.Matches(page.AdditionalText.GetText().ToString(), @"(^|\s)@using [\w\.]+\b", RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(match => match.Value.Substring(7));

            var allPageModelNames = explicitUsings
                .Concat(EnumerateUpPath(page.AdditionalText.Path).SelectMany(path =>
                {
                    return usingsByDirectory.TryGetValue(path, out var usings)
                        ? usings
                        : Enumerable.Empty<string>();
                }))
                .Append(page.Model);

            return allPageModelNames
                .Select(metadataName => compilation.GetTypeByMetadataName(metadataName))
                .FirstOrDefault(type => type != null);
        }

        private static IPagesFoldersDescriptor GetAreaFolder(PageData page, PageAreaDescriptor area)
        {
            var path = Path.GetDirectoryName(page.Page);
            var folders = path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

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
                var match = Regex.Match(constraint.Groups[1].Value, @"^([^:]+)((?::[^:]+?)*)(\?)?$");

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
                parameterName = char.ToLower(parameterName[0]) + parameterName.Substring(1);

                yield return new ParameterDescriptor(
                    parameterName,
                    GetParameterType(member.Type),
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
                if (string.IsNullOrWhiteSpace(pageHandler))
                    pageHandler = null;
                if (pageHandler?.EndsWith("Async") ?? false)
                    pageHandler = pageHandler.Substring(0, pageHandler.Length - 5);

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
                    GetParameterType(param.Type),
                    param.HasExplicitDefaultValue,
                    param.HasExplicitDefaultValue ? param.ExplicitDefaultValue : null);
            }
        }

        public static string GetFullNamespacedTypeName(INamespaceOrTypeSymbol typeSymbol)
        {
            var fullName = new System.Text.StringBuilder();

            while (typeSymbol != null)
            {
                if (!string.IsNullOrEmpty(typeSymbol.Name))
                {
                    if (fullName.Length > 0)
                        fullName.Insert(0, '.');
                    fullName.Insert(0, typeSymbol.Name);
                }
                typeSymbol = typeSymbol.ContainingSymbol as INamespaceOrTypeSymbol;
            }

            return fullName.ToString();
        }

        public static string GetParameterType(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
                return $"{GetParameterType(arrayType.ElementType)}[]";

            var rootType = GetTypeName(type);

            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                rootType += "<";
                rootType += string.Join(", ", namedType.TypeArguments.Select(GetParameterType));
                rootType += ">";
            }

            return rootType;
        }

        public static string GetTypeName(ITypeSymbol type) => (GetFullNamespacedTypeName(type)) switch
        {
            "System.String" => "string",
            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Char" => "char",
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.UInt16" => "ushort",
            "System.UInt32" => "uint",
            "System.UInt64" => "ulong",
            "System.Boolean" => "bool",
            "System.Decimal" => "decimal",
            "System.Double" => "double",
            "System.Float" => "float",
            "System.Object" => "object",
            var t => t,
        };

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

        private class PageData : IComparable<PageData>
        {
            private readonly string[] PathParts;

            internal AdditionalText AdditionalText { get; }

            private string _sourceText;
            private string SourceText => _sourceText ??= AdditionalText.GetText().ToString();

            public PageData(AdditionalText additionalText)
            {
                PathParts = additionalText.Path.Split('/', '\\');
                AdditionalText = additionalText;
            }

            public string Area
            {
                get
                {
                    var areasIdx = Array.IndexOf(PathParts, "Areas");
                    if (areasIdx < 0) return null;
                    if (PathParts.Length < areasIdx + 3) return null;
                    if (PathParts[areasIdx + 2] != "Pages") return null;

                    return PathParts[areasIdx + 1];
                }
            }

            public string Page
            {
                get
                {
                    var pagesIdx = Array.IndexOf(PathParts, "Pages");
                    if (pagesIdx < 0) return null;
                    if (PathParts.Length < pagesIdx + 2) return null;

                    var page = "/" + string.Join("/", PathParts.Skip(pagesIdx + 1));
                    return page.Substring(0, page.Length - 7);
                }
            }

            public string Route
            {
                get
                {
                    var match = Regex.Match(SourceText, @"^\s*@page (""[^""]+"")", RegexOptions.Multiline);
                    if (!match.Success) return null;
                    return match.Groups[1].Value;
                }
            }

            public string Model
            {
                get
                {
                    var match = Regex.Match(SourceText, @"^\s*@model ([\w\.]+)", RegexOptions.Multiline);
                    if (!match.Success) return null;
                    return match.Groups[1].Value;
                }
            }

            public int CompareTo(PageData other)
            {
                var i = 0;
                for (; other.PathParts.Length > i + 1 && PathParts.Length > i + 1; i++)
                {
                    var compare = string.Compare(PathParts[i], other.PathParts[i], StringComparison.Ordinal);
                    if (compare != 0) return compare;
                }

                if (PathParts.Length > other.PathParts.Length) return -1;
                if (PathParts.Length < other.PathParts.Length) return 1;

                return string.Compare(PathParts[i], other.PathParts[i], StringComparison.Ordinal);
            }
        }

        private class FileSystemAdditionalText : AdditionalText
        {
            private readonly string _basePath;

            public FileSystemAdditionalText(string path, string basePath)
            {
                Path = path;
                _basePath = basePath;
            }

            public override string Path { get; }

            public override SourceText? GetText(CancellationToken cancellationToken = default)
            {
                return SourceText.From(File.OpenRead(System.IO.Path.Combine(_basePath, Path.TrimStart('/', '\\'))));
            }
        }
    }
}
