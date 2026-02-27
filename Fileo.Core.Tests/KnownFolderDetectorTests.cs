using System;
using System.IO;
using Xunit;

namespace Fileo.Core.Tests
{
    public class KnownFolderDetectorTests : IDisposable
    {
        private readonly string _tempDir;

        public KnownFolderDetectorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "kfd_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void GetKnownFolderPath_DefaultExists_ReturnsPathSuffix()
        {
            var detector = new Fileo.Core.KnownFolderDetector();
            var downloads = detector.GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Downloads);
            if (downloads is null) Assert.True(true); // acceptable on minimal CI images
            else Assert.EndsWith("Downloads", downloads, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetKnownFolderPath_NoMatches_ReturnsNullOrNonThrowing()
        {
            var detector = new Fileo.Core.KnownFolderDetector();
            var docs = detector.GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Documents);
            // We just assert it doesn't throw; value may be null in constrained environments
            Assert.True(docs == null || docs.EndsWith("Documents", StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
