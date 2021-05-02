using System.Collections.Generic;

namespace UrlActionGenerator.Descriptors
{
    public interface IPagesFoldersDescriptor
    {
        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }
    }
}
