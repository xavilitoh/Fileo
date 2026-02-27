using System;
using System.IO;
using Xunit;

namespace Fileo.Core.Tests
{
    public class FileUtilsTests : IDisposable
    {
        private readonly string _tempDir;

        public FileUtilsTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fileo_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void ResolveCollision_NoCollision_ReturnsOriginal()
        {
            var dest = Fileo.Core.FileUtils.ResolveCollision(_tempDir, "a.txt");
            Assert.Equal(Path.Combine(_tempDir, "a.txt"), dest);
        }

        [Fact]
        public void ResolveCollision_WithCollisions_AppendsSuffix()
        {
            var pathA = Path.Combine(_tempDir, "a.txt");
            File.WriteAllText(pathA, "x");
            var next = Fileo.Core.FileUtils.ResolveCollision(_tempDir, "a.txt");
            Assert.Equal(Path.Combine(_tempDir, "a(1).txt"), next);

            File.WriteAllText(next, "y");
            var third = Fileo.Core.FileUtils.ResolveCollision(_tempDir, "a.txt");
            Assert.Equal(Path.Combine(_tempDir, "a(2).txt"), third);
        }

        [Fact]
        public void ResolveCollision_CompoundExtension_HandlesTarGz()
        {
            var name = "archive.tar.gz";
            var path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "x");
            var candidate = Fileo.Core.FileUtils.ResolveCollision(_tempDir, name);
            // Current implementation treats the extension as Path.GetExtension() (".gz"),
            // so the generated name becomes "archive.tar(1).gz".
            Assert.Equal(Path.Combine(_tempDir, "archive.tar(1).gz"), candidate);
        }

        [Theory]
        [InlineData("file.zip", new string[]{".zip"}, true)]
        [InlineData("file.tar.gz", new string[]{".zip"}, true)]
        [InlineData("file.txt", new string[]{".zip"}, false)]
        public void IsArchive_DetectsVarious(string fileName, string[] exts, bool expected)
        {
            var path = Path.Combine(_tempDir, fileName);
            File.WriteAllText(path, "x");
            var result = Fileo.Core.FileUtils.IsArchive(path, exts);
            Assert.Equal(expected, result);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
