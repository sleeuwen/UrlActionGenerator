using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator
{
    public static class PagesDiscoverer
    {
        public static IEnumerable<PageAreaDescriptor> DiscoverAreaPages(Compilation compilation, IEnumerable<AdditionalText> additionalFiles)
        {
            var pages = additionalFiles
                .Where(file => file.Path.EndsWith(".cshtml"))
                .Where(file =>
                {
                    var text = file.GetText();
                    var content = text.ToString();

                    return content.Contains("@page");
                })
                .Select(file => new PageData(file))
                .ToList();

            foreach (var group in pages.GroupBy(page => page.Area))
            {
                var area = new PageAreaDescriptor(group.Key);

                foreach (var page in group.OrderBy(page => page))
                {
                    area.Pages.Add(new PageDescriptor
                    {
                        Area = area,
                        Name = page.Page,
                    });
                }

                yield return area;
            }
        }

        private class PageData : IComparable<PageData>
        {
            private readonly string[] PathParts;

            private AdditionalText AdditionalText { get; }

            public PageData(AdditionalText additionalText)
            {
                PathParts = additionalText.Path.Split('/');
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
    }
}
