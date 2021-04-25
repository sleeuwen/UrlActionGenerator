using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public interface IPagesFoldersDescriptor
    {
        public List<PageDescriptor> Pages { get; }

        public List<PageFolderDescriptor> Folders { get; }
    }
}
