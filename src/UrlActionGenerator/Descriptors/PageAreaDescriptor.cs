using System;
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

        public string Path { get; } = "/";

        public KeyedCollection<PageDescriptor> Pages { get; }

        public KeyedCollection<PageFolderDescriptor> Folders { get; }

        public IPagesFoldersDescriptor GetFolder(string folderPath)
        {
            var folders = folderPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            IPagesFoldersDescriptor currentFolder = this;
            foreach (var folderName in folders)
            {
                var folder = currentFolder.Folders.FirstOrDefault(f => f.Name == folderName);
                if (folder == null)
                {
                    folder = new PageFolderDescriptor(this, folderName, $"{currentFolder.Path}/{folderName}");
                    currentFolder.Folders.Add(folder);
                }

                currentFolder = folder;
            }

            return currentFolder;
        }
    }
}
