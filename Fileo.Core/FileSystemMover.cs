using System;
using System.IO;
using Fileo.Core.Interfaces;

namespace Fileo.Core
{
    public class FileSystemMover : IFileMover
    {
        public void MoveFile(string source, string dest)
        {
            File.Move(source, dest);
        }

        public void MoveDirectory(string source, string dest)
        {
            Directory.Move(source, dest);
        }

        public bool FileExists(string path) => File.Exists(path);
        public bool DirectoryExists(string path) => Directory.Exists(path);
    }
}
