using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public class PageAreaDescriptor : IPagesFoldersDescriptor
    {
        public PageAreaDescriptor(string name)
        {
            Name = name;
            Pages = new List<PageDescriptor>();
            Folders = new List<PageFolderDescriptor>();
        }

        public string Name { get; }

        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }
}
