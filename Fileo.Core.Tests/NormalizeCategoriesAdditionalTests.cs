using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

namespace Fileo.Core.Tests
{
    public class NormalizeCategoriesAdditionalTests : IDisposable
    {
        private readonly string _tempDir;

        public NormalizeCategoriesAdditionalTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "normalize_additional_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void NormalizeCategories_FlattenTrue_MovesFilesFromSubdirectoriesToCategoryRoot()
        {
            // Arrange
            var src = Path.Combine(_tempDir, "src_flatten");
            Directory.CreateDirectory(src);
            var catDir = Path.Combine(src, "Docs");
            Directory.CreateDirectory(catDir);
            var sub = Path.Combine(catDir, "nested");
            Directory.CreateDirectory(sub);

            var nestedFile = Path.Combine(sub, "notes.txt");
            File.WriteAllText(nestedFile, "content");

            var categories = new List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)>
            {
                ("Docs", p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase), false, true),
            };

            var proc = new Fileo.Core.CategoryProcessor();

            // Act
            proc.NormalizeCategories(src, categories, dryRun: false, progress: null);

            // Assert: file should be moved to category root
            var expected = Path.Combine(catDir, "notes.txt");
            Assert.True(File.Exists(expected), "File should be moved to category root when flatten=true");
            Assert.False(File.Exists(nestedFile), "Original nested file should not exist anymore");
        }

        [Fact]
        public void NormalizeCategories_DryRun_DoesNotMoveFiles()
        {
            // Arrange
            var src = Path.Combine(_tempDir, "src_dryrun");
            Directory.CreateDirectory(src);
            var catA = Path.Combine(src, "A");
            var catB = Path.Combine(src, "B");
            Directory.CreateDirectory(catA);
            Directory.CreateDirectory(catB);

            var fileInA = Path.Combine(catA, "image.jpg");
            File.WriteAllText(fileInA, "img");

            var categories = new List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)>
            {
                ("A", p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase), false, false),
                ("B", p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase), false, false),
            };

            var proc = new Fileo.Core.CategoryProcessor();

            // Act (dry run)
            proc.NormalizeCategories(src, categories, dryRun: true, progress: null);

            // Assert: file should remain in original location
            Assert.True(File.Exists(fileInA), "File should not be moved during dry run");
            var expectedInB = Path.Combine(catB, "image.jpg");
            Assert.False(File.Exists(expectedInB), "Dry run should not create files in target category");
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
