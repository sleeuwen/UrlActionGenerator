using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public class PageFolderDescriptor : IPagesFoldersDescriptor
    {
        public PageFolderDescriptor(PageAreaDescriptor area, string name)
        {
            Area = area;
            Name = name;
            Pages = new List<PageDescriptor>();
            Folders = new List<PageFolderDescriptor>();
        }

        public PageAreaDescriptor Area { get; }

        public string Name { get; }

        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }
}
