using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Fileo.Core
{
    using Fileo.Core.Interfaces;

    public class CategoryProcessor : ICategoryProcessor
    {
        private readonly IFileMover _mover;
        private readonly IFileLogger? _logger;

        public CategoryProcessor(IFileMover? mover = null, IFileLogger? logger = null)
        {
            _mover = mover ?? new FileSystemMover();
            _logger = logger;
        }

        public int ProcessCategory(string srcDir, string destName, Func<string,bool> matcher, bool includeDirs = false, bool flatten = false, bool dryRun = false, Fileo.Core.Interfaces.IProgressReporter? progress = null)
        {
            int moved = 0;
            string destDir = Path.Combine(srcDir, destName);
            Directory.CreateDirectory(destDir);

            IEnumerable<string> entries = includeDirs ? Directory.GetFileSystemEntries(srcDir) : Directory.GetFiles(srcDir);
            var initialMatches = entries.Where(e => matcher(e)).ToList();
            int initialTotal = initialMatches.Count;
            progress?.Report(destName, 0, initialTotal);

            foreach (var entry in entries)
            {
                try
                {
                    if (!matcher(entry)) continue;

                    string name = Path.GetFileName(entry);
                    string dest = ResolveCollision(destDir, name);

                    if (dryRun)
                    {
                        _logger?.Log($"{name} -> {destName}/{Path.GetFileName(dest)}", LogLevel.DryRun, destName);
                    }
                    else
                    {
                        if (_mover.DirectoryExists(entry)) _mover.MoveDirectory(entry, dest);
                        else _mover.MoveFile(entry, dest);
                        _logger?.Log($"{name} -> {destName}/{Path.GetFileName(dest)}", LogLevel.Info, destName);
                    }

                    moved++;
                    progress?.Report(destName, moved, initialTotal);
                }
                catch (Exception ex)
                {
                    _logger?.Log($"Error moviendo {entry}: {ex.Message}", LogLevel.Error, destName);
                }
            }

            if (flatten && Directory.Exists(destDir))
            {
                // compute flatten matches after initial move
                var subdirs = Directory.GetDirectories(destDir, "*", SearchOption.AllDirectories);
                var flattenMatches = 0;
                foreach (var sub in subdirs)
                {
                    flattenMatches += Directory.GetFiles(sub).Count(f => matcher(f));
                }
                progress?.Report(destName, moved, initialTotal + flattenMatches);
                foreach (var sub in subdirs)
                {
                    string[] files = Directory.GetFiles(sub);
                    foreach (var f in files)
                    {
                        try
                        {
                            if (!matcher(f)) continue;
                            string name = Path.GetFileName(f);
                            string dest = ResolveCollision(destDir, name);
                            if (dryRun) _logger?.Log($"{name} -> {destName}/{Path.GetFileName(dest)}", LogLevel.DryRun, destName);
                            else
                            {
                                _mover.MoveFile(f, dest);
                                _logger?.Log($"{name} -> {destName}/{Path.GetFileName(dest)}", LogLevel.Info, destName);
                            }
                            moved++;
                            progress?.Report(destName, moved, initialTotal + flattenMatches);
                        }
                        catch (Exception ex)
                        {
                               _logger?.Log($"Error moviendo {f}: {ex.Message}", LogLevel.Error, destName);
                        }
                    }
                }
            }

            return moved;
        }

        public void NormalizeCategories(string srcDir, List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)> categories, bool dryRun = false, Fileo.Core.Interfaces.IProgressReporter? progress = null)
        {
            foreach (var cat in categories)
            {
                string catDir = Path.Combine(srcDir, cat.name);
                if (!Directory.Exists(catDir)) continue;
                var files = Directory.GetFiles(catDir, "*", SearchOption.AllDirectories);
                int total = files.Count();
                int current = 0;
                progress?.Report(cat.name, current, total);
                foreach (var f in files)
                {
                    try
                    {
                        if (IsInsideAppBundle(f, catDir)) continue;

                        var target = categories.FirstOrDefault(c => c.matcher(f));
                        if (target == default) continue;

                        if (string.Equals(target.name, cat.name, StringComparison.OrdinalIgnoreCase))
                        {
                            if (cat.flatten)
                            {
                                var name = Path.GetFileName(f);
                                var dest = ResolveCollision(catDir, name);
                                if (!string.Equals(Path.GetDirectoryName(f), catDir, StringComparison.OrdinalIgnoreCase))
                                {
                                                if (dryRun) _logger?.Log($"{name} -> {cat.name}/{Path.GetFileName(dest)}", LogLevel.DryRun, cat.name);
                                                else { _mover.MoveFile(f, dest); _logger?.Log($"Movido (normalize flatten): {name} -> {cat.name}/{Path.GetFileName(dest)}", LogLevel.Info, cat.name); }
                                }
                            }
                            continue;
                        }

                        string targetDir = Path.Combine(srcDir, target.name);
                        Directory.CreateDirectory(targetDir);
                        var fileName = Path.GetFileName(f);
                        var destPath = ResolveCollision(targetDir, fileName);
                        if (dryRun) _logger?.Log($"{fileName} -> {target.name}/{Path.GetFileName(destPath)}", LogLevel.DryRun, target.name);
                        else { _mover.MoveFile(f, destPath); _logger?.Log($"Movido (normalize): {fileName} -> {target.name}/{Path.GetFileName(destPath)}", LogLevel.Info, target.name); }
                        }
                    catch (Exception ex)
                    {
                        _logger?.Log($"Error normalizando {f}: {ex.Message}", LogLevel.Error, null);
                    }
                        current++;
                        progress?.Report(cat.name, current, total);
                }
            }
        }

        static bool IsInsideAppBundle(string filePath, string categoryRoot)
        {
            var dir = Path.GetDirectoryName(filePath);
            while (!string.IsNullOrEmpty(dir) && dir.StartsWith(categoryRoot, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(Path.GetExtension(dir), ".app", StringComparison.OrdinalIgnoreCase)) return true;
                dir = Path.GetDirectoryName(dir);
            }
            return false;
        }

        static string ResolveCollision(string destDir, string fileName)
        {
            return FileUtils.ResolveCollision(destDir, fileName);
        }
    }
}
