using System;

namespace Fileo.Core.Interfaces
{
    public interface IFileMover
    {
        void MoveFile(string source, string dest);
        void MoveDirectory(string source, string dest);
        bool FileExists(string path);
        bool DirectoryExists(string path);
    }
}
