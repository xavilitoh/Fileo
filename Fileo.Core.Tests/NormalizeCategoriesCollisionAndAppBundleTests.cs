using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace Fileo.Core.Tests
{
    public class NormalizeCategoriesCollisionAndAppBundleTests : IDisposable
    {
        private readonly string _tempDir;

        public NormalizeCategoriesCollisionAndAppBundleTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "normalize_collisions_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void NormalizeCategories_Flatten_CollisionProducesDistinctNames()
        {
            // Arrange
            var src = Path.Combine(_tempDir, "src_collisions");
            Directory.CreateDirectory(src);
            var cat = Path.Combine(src, "Photos");
            Directory.CreateDirectory(cat);
            var sub1 = Path.Combine(cat, "a");
            var sub2 = Path.Combine(cat, "b");
            Directory.CreateDirectory(sub1);
            Directory.CreateDirectory(sub2);

            var file1 = Path.Combine(sub1, "pic.jpg");
            var file2 = Path.Combine(sub2, "pic.jpg");
            File.WriteAllText(file1, "1");
            File.WriteAllText(file2, "2");

            var categories = new List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)>
            {
                ("Photos", p => p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase), false, true),
            };

            var proc = new Fileo.Core.CategoryProcessor();

            // Act
            proc.NormalizeCategories(src, categories, dryRun: false, progress: null);

            // Assert: two files with distinct names exist in category root
            var files = Directory.GetFiles(cat, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToList();
            Assert.Equal(2, files.Count);
            Assert.All(files, f => Assert.EndsWith(".jpg", f, StringComparison.OrdinalIgnoreCase));
            Assert.NotEqual(files[0], files[1]);
        }

        [Fact]
        public void IsInsideAppBundle_IgnoresAppOutsideCategoryRoot()
        {
            var src = Path.Combine(_tempDir, "rootA");
            var other = Path.Combine(_tempDir, "rootB");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(other);

            var appOutside = Path.Combine(other, "MyApp.app");
            Directory.CreateDirectory(appOutside);
            var exe = Path.Combine(appOutside, "Contents", "exe");
            Directory.CreateDirectory(Path.Combine(appOutside, "Contents"));
            File.WriteAllText(exe, "x");

            var method = typeof(Fileo.Core.CategoryProcessor).GetMethod("IsInsideAppBundle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { exe, src });
            Assert.False(result);
        }

        [Fact]
        public void IsInsideAppBundle_IsCaseInsensitiveForAppExtension()
        {
            var src = Path.Combine(_tempDir, "srcCase");
            Directory.CreateDirectory(src);
            var cat = Path.Combine(src, "Apps");
            Directory.CreateDirectory(cat);
            var appBundle = Path.Combine(cat, "Cool.APP");
            Directory.CreateDirectory(appBundle);
            var cont = Path.Combine(appBundle, "Contents");
            Directory.CreateDirectory(cont);
            var exe = Path.Combine(cont, "run");
            File.WriteAllText(exe, "x");

            var method = typeof(Fileo.Core.CategoryProcessor).GetMethod("IsInsideAppBundle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = (bool)method.Invoke(null, new object[] { exe, cat });
            Assert.True(result);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
