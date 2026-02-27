using System;
using System.IO;
using Xunit;

namespace Fileo.Core.Tests
{
    public class FileUtilsIsArchiveTests : IDisposable
    {
        private readonly string _tempDir;

        public FileUtilsIsArchiveTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "fileutils_archives_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Theory]
        [InlineData("archive.zip", new string[] { ".zip" }, true)]
        [InlineData("archive.tar.gz", new string[] { ".zip" }, true)]
        [InlineData("archive.TAR.GZ", new string[] { ".zip" }, true)]
        [InlineData("archive.tar.bz2", new string[] { ".bz2" }, true)]
        [InlineData("archive.tar.xz", new string[] { ".xz" }, true)]
        [InlineData("file.txt", new string[] { ".zip", ".tar.gz" }, false)]
        [InlineData("bundle.gz", new string[] { ".gz" }, true)]
        [InlineData("compound.name.tar.gz", new string[] { ".gz" }, true)]
        public void IsArchive_VariousExtensions_BehavesAsExpected(string name, string[] extList, bool expected)
        {
            var path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "x");
            var result = Fileo.Core.FileUtils.IsArchive(path, extList);
            Assert.Equal(expected, result);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
