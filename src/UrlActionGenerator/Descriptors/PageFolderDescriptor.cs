using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UrlActionGenerator.Descriptors
{
    public class PageFolderDescriptor : IPagesFoldersDescriptor
    {
        public PageFolderDescriptor(PageAreaDescriptor area, string name)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Pages = new KeyedCollection<PageDescriptor>(page => new { page.Name, page.PageHandler, Parameters = string.Join(",", page.Parameters.Select(param => param.Type.ToDisplayParts(SymbolDisplayFormat.FullyQualifiedFormat))) });
            Folders = new KeyedCollection<PageFolderDescriptor>(folder => folder.Name);
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }
    }
}
