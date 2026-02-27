using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;
using Fileo.Core;
using Fileo.Core.Interfaces;
using Spectre.Console.Rendering;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            // Options:
            // -d => Downloads
            // -m => Documents (My Documents)
            // -p <path> => specific path
            // no args => interactive selection (behave like option C)

            string dir = null;
            bool argProvided = args.Length > 0;
            bool dryRun = false;
            // Check for help flag early
            if (args.Any(a => a == "-h" || a == "--help"))
            {
                PrintHelp();
                return 0;
            }
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                    if (a == "-d")
                    {
                        var detector = new KnownFolderDetector();
                        var found = detector.GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Downloads);
                        dir = string.IsNullOrEmpty(found) ? GetKnownFolderPath(KnownFolder.Downloads) : found;
                    }
                    else if (a == "-m")
                    {
                        var detector = new KnownFolderDetector();
                        var found = detector.GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Documents);
                        dir = string.IsNullOrEmpty(found) ? GetKnownFolderPath(KnownFolder.Documents) : found;
                    }
                else if (a == "-p" || a == "--path")
                {
                    if (i + 1 < args.Length)
                    {
                        dir = args[i + 1];
                        i++;
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]La opción -p requiere una ruta[/]");
                        return 1;
                    }
                }
                else if (a == "-n" || a == "--dry-run")
                {
                    dryRun = true;
                }
                else if (a.StartsWith("-p=") || a.StartsWith("--path="))
                {
                    var idx = a.IndexOf('=');
                    dir = a.Substring(idx + 1);
                }
            }

            // If no arg provided, interactive mode (behave like option C)
            if (!argProvided)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("¿Qué carpeta quieres organizar?")
                        .AddChoices(new[] { "Downloads", "Documents", "Especificar ruta", "Cancelar" }));

                if (choice == "Downloads")
                {
                    var found = new KnownFolderDetector().GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Downloads);
                    dir = string.IsNullOrEmpty(found) ? GetKnownFolderPath(KnownFolder.Downloads) : found;
                }
                else if (choice == "Documents")
                {
                    var found = new KnownFolderDetector().GetKnownFolderPath(Fileo.Core.Interfaces.KnownFolder.Documents);
                    dir = string.IsNullOrEmpty(found) ? GetKnownFolderPath(KnownFolder.Documents) : found;
                }
                else if (choice == "Especificar ruta")
                {
                    dir = AnsiConsole.Ask<string>("Ingresa la ruta absoluta o relativa:");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Operación cancelada.[/]");
                    return 0;
                }
            }

            // Expand ~ to user profile on mac/linux
            if (!string.IsNullOrEmpty(dir) && (dir == "~" || dir.StartsWith("~/")))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                dir = dir == "~" ? home : Path.Combine(home, dir.Substring(2));
            }

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                AnsiConsole.MarkupLine($"[red]Directorio no existe: {dir}[/]");
                return 2;
            }

            AnsiConsole.MarkupLine($"[grey]WorkingDir:[/] [green]{dir}[/]");
            AnsiConsole.MarkupLine($"[grey]BaseDirectory:[/] [green]{AppContext.BaseDirectory}[/]");

            var imageExts = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg", ".webp", ".tif", ".tiff" };
            var docExts = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".csv" };
            var archiveExts = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".tgz", ".bz2", ".xz" };
            var appExts = new[] { ".app", ".dmg", ".pkg", ".exe", ".msi", ".apk", ".deb", ".rpm" };

            var categories = new List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)>
            {
                ("Images", path => imageExts.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase), false, true),
                ("Documents", path => docExts.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase), false, true),
                ("Archives", path => IsArchiveLocal(path, archiveExts), false, true),
                ("Apps", path => appExts.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase), true, false),
            };

            // Set up DI and services
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<Fileo.Core.Interfaces.IFileMover, Fileo.Core.FileSystemMover>();
            services.AddSingleton<LogStore>();
            services.AddSingleton<Fileo.Core.Interfaces.IFileLogger>(sp => new LiveLogger(sp.GetRequiredService<LogStore>()));
            services.AddSingleton<Fileo.Core.Interfaces.IKnownFolderDetector, Fileo.Core.KnownFolderDetector>();
            services.AddSingleton<Fileo.Core.Interfaces.ICategoryProcessor, Fileo.Core.CategoryProcessor>();
            var provider = services.BuildServiceProvider();

            var processor = provider.GetRequiredService<Fileo.Core.Interfaces.ICategoryProcessor>();

            // Header panel with context
            var header = new Panel($"[grey]Ruta:[/] [green]{dir}[/]    [grey]Dry-run:[/] [yellow]{dryRun}[/]") { Header = new PanelHeader("Fileo — estado") };
            AnsiConsole.Write(header);

            // Show live progress for categories with richer columns
            AnsiConsole.Progress()
                .Columns(new ProgressColumn[] {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
                })
                .AutoClear(false)
                .HideCompleted(false)
                .Start(ctx =>
                {
                    // Add a live logs task on the right
                    var store = provider.GetRequiredService<LogStore>();

                    foreach (var cat in categories)
                    {
                        var task = ctx.AddTask($"{cat.name}", maxValue: 1);
                        var reporter = new LocalProgressReporter(task);
                            int moved = processor.ProcessCategory(dir, cat.name, cat.matcher, cat.includeDirs, cat.flatten, dryRun, reporter);
                        task.Value = task.MaxValue;
                        AnsiConsole.MarkupLine($"[grey]Categoria {cat.name}:[/] movidos {moved}");
                        var recent = store.GetLastForCategory(cat.name, 8);
                        if (recent.Length > 0)
                        {
                            var joined = string.Join("\n", recent.Select(r => Spectre.Console.Markup.Escape(r.Message)));
                            var panel = new Panel(new Markup(joined)) { Header = new PanelHeader("Logs (últimos)") };
                            AnsiConsole.Write(panel);
                        }
                    }

                    // normalization with progress
                    var normTask = ctx.AddTask($"Normalize", maxValue: 1);
                    var normReporter = new LocalProgressReporter(normTask);
                    processor.NormalizeCategories(dir, categories, dryRun, normReporter);
                    normTask.Value = normTask.MaxValue;
                });

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error al ordenar archivos: " + ex.Message);
            return 99;
        }
    }

    static void PrintHelp()
    {
        var table = new Table();
        table.AddColumn("Opción");
        table.AddColumn("Descripción");
        table.AddRow("-d", "Ordenar la carpeta Downloads del sistema");
        table.AddRow("-m", "Ordenar la carpeta Documents del sistema");
        table.AddRow("-p <ruta>", "Ordenar una carpeta específica (ej: -p /ruta/a/carpeta)");
        table.AddRow("-h", "Mostrar esta ayuda");

        AnsiConsole.Write(new Rule("fileo — ayuda").RuleStyle("green"));
        AnsiConsole.MarkupLine("Usa estas opciones para elegir qué carpeta ordenar:\n");
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\nEjemplos:\n");
        var examples = new Panel("fileo -d\nfileo -m\nfileo -p /Users/tu/Downloads") { Header = new PanelHeader("Ejemplos") };
        AnsiConsole.Write(examples);
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

    // Local reporter adapts IProgressReporter to a Spectre ProgressTask
    class LocalProgressReporter : IProgressReporter
    {
        private readonly ProgressTask _task;
        public LocalProgressReporter(ProgressTask task) => _task = task;
        public void Report(string category, int current, int total)
        {
            if (total > 0) _task.MaxValue = total;
            if (current > _task.Value) _task.Value = current;
        }
    }

    enum KnownFolder { Downloads, Documents }

    static string GetKnownFolderPath(KnownFolder k)
    {
        // Platform-appropriate default path
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

        // Option C behavior: prompt the user to locate the folder when default isn't present
        AnsiConsole.MarkupLine($"[yellow]No se encontró la carpeta {k} en la ruta esperada:[/] [grey]{defaultPath}[/]");

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("¿Qué quieres hacer?")
                .AddChoices(new[] { "Ingresar ruta manualmente", "Buscar en mi HOME y volúmenes", "Cancelar" }));

        if (action == "Cancelar") return null;

        if (action == "Ingresar ruta manualmente")
        {
            while (true)
            {
                var input = AnsiConsole.Ask<string>("Ingresa la ruta absoluta o relativa (vacío para cancelar):");
                if (string.IsNullOrWhiteSpace(input)) return null;
                if (input == "~" || input.StartsWith("~/"))
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    input = input == "~" ? home : Path.Combine(home, input.Substring(2));
                }
                try
                {
                    var full = Path.GetFullPath(input);
                    if (Directory.Exists(full)) return full;
                    AnsiConsole.MarkupLine($"[red]La ruta no existe: {full}[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Ruta inválida: {ex.Message}[/]");
                }

                var retry = AnsiConsole.Confirm("¿Intentar de nuevo?");
                if (!retry) return null;
            }
        }

        // Search HOME recursively (best-effort) and top-level /Volumes on macOS for likely folders
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        var nameToFind = k == KnownFolder.Downloads ? "Downloads" : "Documents";
        var matches = new List<string>();
        try
        {
            matches.AddRange(Directory.EnumerateDirectories(homeDir, nameToFind, SearchOption.AllDirectories));
        }
        catch { }
        try
        {
            if (Directory.Exists("/Volumes"))
            {
                foreach (var v in Directory.GetDirectories("/Volumes"))
                {
                    try
                    {
                        matches.AddRange(Directory.EnumerateDirectories(v, nameToFind, SearchOption.TopDirectoryOnly));
                    }
                    catch { }
                }
            }
        }
        catch { }

        matches = matches.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (matches.Count > 0)
        {
            var chosen = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Selecciona la carpeta encontrada:").AddChoices(matches));
            return chosen;
        }

        AnsiConsole.MarkupLine("[red]No se encontraron coincidencias automáticas.[/]");
        // fallback to manual entry loop
        while (true)
        {
            var input = AnsiConsole.Ask<string>("Ingresa la ruta absoluta o relativa (vacío para cancelar):");
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (input == "~" || input.StartsWith("~/"))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                input = input == "~" ? home : Path.Combine(home, input.Substring(2));
            }
            try
            {
                var full = Path.GetFullPath(input);
                if (Directory.Exists(full)) return full;
                AnsiConsole.MarkupLine($"[red]La ruta no existe: {full}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Ruta inválida: {ex.Message}[/]");
            }

            var retry = AnsiConsole.Confirm("¿Intentar de nuevo?");
            if (!retry) return null;
        }
    }

    static string ResolveCollision(string destDir, string fileName)
    {
        // keep for compatibility in Program (delegates to core behavior)
        return Path.Combine(destDir, fileName);
    }

    static bool IsArchiveLocal(string path, string[] archiveExts)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        if (name.EndsWith(".tar.gz") || name.EndsWith(".tar.bz2") || name.EndsWith(".tar.xz")) return true;
        var ext = Path.GetExtension(path);
        return archiveExts.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
}
