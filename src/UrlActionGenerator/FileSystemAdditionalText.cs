using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UrlActionGenerator
{
    /// <summary>
    /// An implementation of the <see cref="AdditionalText"/> that get's it's contents from a file on disk.
    /// </summary>
    /// <remarks>This is only available as a workaround because .cshtml files are not included in the compilation</remarks>
    internal class FileSystemAdditionalText : AdditionalText
    {
        private readonly string _basePath;

        public FileSystemAdditionalText(string path, string basePath)
        {
            Path = path;
            _basePath = basePath;
        }

        public override string Path { get; }

        public override SourceText? GetText(CancellationToken cancellationToken = default)
        {
            using var fileStream = File.OpenRead(System.IO.Path.Combine(_basePath, Path.TrimStart('/', '\\')));
            return SourceText.From(fileStream);
        }
    }
}
