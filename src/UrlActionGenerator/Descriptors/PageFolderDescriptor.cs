using System;
using System.Linq;

namespace UrlActionGenerator.Descriptors
{
    public class PageFolderDescriptor : IPagesFoldersDescriptor
    {
        public PageFolderDescriptor(PageAreaDescriptor area, string name, string path)
        {
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Pages = new KeyedCollection<PageDescriptor>(page => new { page.Name, page.PageHandler, Parameters = string.Join(",", page.Parameters.Select(param => param.Type.TrimEnd('?'))) });
            Folders = new KeyedCollection<PageFolderDescriptor>(folder => folder.Name);
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public string Path { get; }

        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }
    }
}
