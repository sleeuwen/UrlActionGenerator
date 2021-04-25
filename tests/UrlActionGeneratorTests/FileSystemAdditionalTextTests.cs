using System.IO;
using FluentAssertions;
using UrlActionGenerator;
using Xunit;

namespace UrlActionGeneratorTests
{
    public class FileSystemAdditionalTextTests
    {
        [Fact]
        public void Path_ReturnsThePath()
        {
            var additionalText = new FileSystemAdditionalText("Path/To/File.cs", "/home/stephan");

            // Act
            var path = additionalText.Path;

            // Assert
            path.Should().Be("Path/To/File.cs");
        }

        [Fact]
        public void GetText_ReturnsTextFromFile()
        {
            var directory = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
            var file = Path.Join(Path.GetRandomFileName(), Path.GetRandomFileName()) + ".txt";

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Join(directory, file)));
                File.WriteAllText(Path.Join(directory, file), "This is the file");
                var additionalText = new FileSystemAdditionalText(file, directory);

                // Act
                var source = additionalText.GetText();

                // Assert
                source.Should().NotBeNull();
                source.ToString().Should().Be("This is the file");
            }
            finally
            {
                Directory.Delete(Path.Combine(Path.GetTempPath(), directory), recursive: true);
            }
        }
    }
}
