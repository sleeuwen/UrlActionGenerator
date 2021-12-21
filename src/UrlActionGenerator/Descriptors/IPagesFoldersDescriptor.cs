namespace UrlActionGenerator.Descriptors
{
    public interface IPagesFoldersDescriptor
    {
        public string Path { get; }

        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }
    }
}
