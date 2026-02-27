using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Fileo.Core.Interfaces;

namespace Fileo.Core
{
    public class KnownFolderDetector : IKnownFolderDetector
    {
        public string? GetKnownFolderPath(KnownFolder k)
        {
            string defaultPath;
            if (OperatingSystem.IsWindows())
            {
                var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                defaultPath = k == KnownFolder.Downloads ? Path.Combine(user, "Downloads") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            else
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                defaultPath = k == KnownFolder.Downloads ? Path.Combine(home, "Downloads") : Path.Combine(home, "Documents");
            }

            if (Directory.Exists(defaultPath)) return defaultPath;

            // Heuristic search (non-interactive) — search HOME and /Volumes (macOS) for likely folders
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var nameToFind = k == KnownFolder.Downloads ? "Downloads" : "Documents";
            var matches = new List<string>();
            try { matches.AddRange(Directory.EnumerateDirectories(homeDir, nameToFind, SearchOption.AllDirectories)); } catch { }
            try
            {
                if (Directory.Exists("/Volumes"))
                {
                    foreach (var v in Directory.GetDirectories("/Volumes"))
                    {
                        try { matches.AddRange(Directory.EnumerateDirectories(v, nameToFind, SearchOption.TopDirectoryOnly)); } catch { }
                    }
                }
            }
            catch { }

            matches = matches.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (matches.Count > 0) return matches[0];

            return null;
        }
    }
}
