using System;
using System.IO;
using System.Linq;
using Moq;
using Xunit;
using Fileo.Core.Interfaces;

namespace Fileo.Core.Tests
{
    public class CategoryProcessorTests : IDisposable
    {
        private readonly string _tempDir;

        public CategoryProcessorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "catproc_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void ProcessCategory_SimpleFileMove_CallsMoveFile()
        {
            var src = Path.Combine(_tempDir, "src");
            Directory.CreateDirectory(src);
            var file = Path.Combine(src, "a.txt");
            File.WriteAllText(file, "x");

            var mover = new Mock<IFileMover>();
            mover.Setup(m => m.DirectoryExists(It.IsAny<string>())).Returns(false);

            var logger = new Mock<IFileLogger>();

            var proc = new Fileo.Core.CategoryProcessor(mover.Object, logger.Object);
            int moved = proc.ProcessCategory(src, "TextFiles", p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(1, moved);
            mover.Verify(m => m.MoveFile(It.Is<string>(s => s == file), It.Is<string>(d => d.Contains("TextFiles") && d.EndsWith("a.txt"))), Times.Once);
            logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<LogLevel>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void ProcessCategory_DryRun_DoesNotCallMover_LogsDryRun()
        {
            var src = Path.Combine(_tempDir, "src2");
            Directory.CreateDirectory(src);
            var file = Path.Combine(src, "b.txt");
            File.WriteAllText(file, "x");

            var mover = new Mock<IFileMover>();
            var logger = new Mock<IFileLogger>();

            var proc = new Fileo.Core.CategoryProcessor(mover.Object, logger.Object);
            int moved = proc.ProcessCategory(src, "TextFiles", p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase), dryRun: true);

            Assert.Equal(1, moved);
            mover.Verify(m => m.MoveFile(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            logger.Verify(l => l.Log(It.Is<string>(s => s.Contains("-> TextFiles/")), LogLevel.DryRun, "TextFiles"), Times.Once);
        }

        [Fact]
        public void ProcessCategory_IncludeDirsAndFlatten_MovesDirectoryAndFlattens()
        {
            var src = Path.Combine(_tempDir, "src3");
            Directory.CreateDirectory(src);
            var dirA = Path.Combine(src, "dirA");
            Directory.CreateDirectory(dirA);
            var nested = Path.Combine(dirA, "nested.txt");
            File.WriteAllText(nested, "x");

            var logger = new Mock<IFileLogger>();

            // Use real mover so filesystem changes occur for flatten
            var proc = new Fileo.Core.CategoryProcessor(null, logger.Object);
            int moved = proc.ProcessCategory(src, "Dirs", p => Directory.Exists(p) || p.EndsWith(".txt"), includeDirs: true, flatten: true);

            // both the directory move and the flattened file should count
            Assert.True(moved >= 2);
            logger.Verify(l => l.Log(It.IsAny<string>(), It.IsAny<LogLevel>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public void ProcessCategory_MoveError_LogsAndContinues()
        {
            var src = Path.Combine(_tempDir, "src4");
            Directory.CreateDirectory(src);
            var file1 = Path.Combine(src, "c1.txt");
            var file2 = Path.Combine(src, "c2.txt");
            File.WriteAllText(file1, "x");
            File.WriteAllText(file2, "y");

            var mover = new Mock<IFileMover>();
            mover.Setup(m => m.DirectoryExists(It.IsAny<string>())).Returns(false);
            mover.Setup(m => m.MoveFile(It.Is<string>(s => s.EndsWith("c1.txt")), It.IsAny<string>())).Throws(new Exception("boom"));

            var logger = new Mock<IFileLogger>();

            var proc = new Fileo.Core.CategoryProcessor(mover.Object, logger.Object);
            int moved = proc.ProcessCategory(src, "TextFiles", p => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(1, moved);
            logger.Verify(l => l.Log(It.Is<string>(s => s.Contains("Error moviendo")), LogLevel.Error, "TextFiles"), Times.AtLeastOnce);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
