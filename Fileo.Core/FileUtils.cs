using System;
using System.IO;
using System.Linq;

namespace Fileo.Core
{
    public static class FileUtils
    {
        public static string ResolveCollision(string destDir, string fileName)
        {
            string dest = Path.Combine(destDir, fileName);
            if (!File.Exists(dest) && !Directory.Exists(dest)) return dest;

            string nameOnly = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(destDir, $"{nameOnly}({i}){ext}");
                i++;
            } while (File.Exists(candidate) || Directory.Exists(candidate));

            return candidate;
        }

        public static bool IsArchive(string path, string[] archiveExts)
        {
            var name = Path.GetFileName(path).ToLowerInvariant();
            if (name.EndsWith(".tar.gz") || name.EndsWith(".tar.bz2") || name.EndsWith(".tar.xz")) return true;
            var ext = Path.GetExtension(path);
            return archiveExts.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }
    }
}
