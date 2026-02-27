using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

namespace Fileo.Core.Tests
{
    public class NormalizeCategoriesTests : IDisposable
    {
        private readonly string _tempDir;

        public NormalizeCategoriesTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "normalize_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void NormalizeCategories_MovesFileToTargetCategory()
        {
            // Arrange
            var src = Path.Combine(_tempDir, "src");
            Directory.CreateDirectory(src);
            var imagesDir = Path.Combine(src, "Images");
            var docsDir = Path.Combine(src, "Documents");
            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(docsDir);

            var fileInImages = Path.Combine(imagesDir, "readme.txt");
            File.WriteAllText(fileInImages, "hello");

            var categories = new List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)>
            {
                ("Images", p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase), false, false),
                ("Documents", p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase), false, false)
            };

            var proc = new Fileo.Core.CategoryProcessor();

            // Act
            proc.NormalizeCategories(src, categories, dryRun: false, progress: null);

            // Assert: file should now be in Documents
            var destPath = Path.Combine(docsDir, "readme.txt");
            Assert.True(File.Exists(destPath), "File should have been moved to Documents");
            Assert.False(File.Exists(fileInImages), "Original file should no longer exist in Images");
        }

        [Fact]
        public void IsInsideAppBundle_ReturnsTrueForAppPath()
        {
            var src = Path.Combine(_tempDir, "src2");
            Directory.CreateDirectory(src);
            var catDir = Path.Combine(src, "Apps");
            Directory.CreateDirectory(catDir);
            var appBundle = Path.Combine(catDir, "MyApp.app");
            Directory.CreateDirectory(appBundle);
            var contents = Path.Combine(appBundle, "Contents");
            Directory.CreateDirectory(contents);
            var exe = Path.Combine(contents, "executable");
            File.WriteAllText(exe, "x");

            // Use reflection to call internal static method since it's private in class scope
            var method = typeof(Fileo.Core.CategoryProcessor).GetMethod("IsInsideAppBundle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { exe, catDir });
            Assert.True(result);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
