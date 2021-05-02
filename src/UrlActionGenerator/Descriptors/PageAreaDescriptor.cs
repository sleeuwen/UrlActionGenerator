using System.Linq;

namespace UrlActionGenerator.Descriptors
{
    public class PageAreaDescriptor : IPagesFoldersDescriptor
    {
        public PageAreaDescriptor(string name)
        {
            Name = name;
            Pages = new KeyedCollection<PageDescriptor>(page => new { page.Name, page.PageHandler, Parameters = string.Join(",", page.Parameters.Select(param => param.Type.TrimEnd('?'))) });
            Folders = new KeyedCollection<PageFolderDescriptor>(folder => folder.Name);
        }

        public string Name { get; }

        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }
    }
}
