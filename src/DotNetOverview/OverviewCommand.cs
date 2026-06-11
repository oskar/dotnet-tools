using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DotNetOverview;

public sealed class OverviewCommand(IAnsiConsole ansiConsole, TextWriter? rawOutput = null) : Command<OverviewCommand.Settings>
{
    // JSON is written directly to stdout rather than through IAnsiConsole to avoid
    // Spectre.Console wrapping long lines to fit the terminal width, which produces invalid JSON.
    private readonly TextWriter _rawOutput = rawOutput ?? Console.Out;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[path]")]
        public string? Path { get; set; }

        [Description("Print version of this tool and exit")]
        [CommandOption("-v|--version")]
        public bool Version { get; set; }

        [Description("Show project file paths (relative to search path) instead of name")]
        [CommandOption("-p|--show-paths")]
        public bool ShowPaths { get; set; }

        [Description("Show absolute paths instead of relative")]
        [CommandOption("-a|--absolute-paths")]
        public bool AbsolutePaths { get; set; }

        [Description("Format the result as JSON")]
        [CommandOption("-j|--json")]
        public bool Json { get; set; }
    }

    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Version)
        {
            var attribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var version = attribute?.InformationalVersion.Split('+')[0] ?? "Unknown";
            ansiConsole.WriteLine(version);
            return 0;
        }

        // Calculate absolute path from supplied path and default
        // to current directory if no path is specified.
        var rawPath = string.IsNullOrEmpty(settings.Path)
            ? Directory.GetCurrentDirectory()
            : settings.Path;

        string[] searchPaths;
        string commonAncestor;

        if (rawPath.Contains('*'))
        {
            var absoluteRaw = Path.IsPathRooted(rawPath) ? rawPath : Path.GetFullPath(rawPath);
            var parent = Path.GetDirectoryName(absoluteRaw);
            var pattern = Path.GetFileName(absoluteRaw);

            if (pattern is null || parent is null || parent.Contains('*'))
            {
                ansiConsole.MarkupLine("Wildcard is only supported in the last path segment.");
                return 1;
            }

            if (!Directory.Exists(parent))
            {
                ansiConsole.MarkupLine($"Path does not exist: [green]{Markup.Escape(parent)}[/].");
                return 1;
            }

            var matched = Directory.EnumerateDirectories(parent, pattern).OrderBy(d => d).ToArray();

            if (matched.Length == 0)
            {
                ansiConsole.MarkupLine($"No directories matched pattern: [green]{Markup.Escape(absoluteRaw)}[/].");
                return 1;
            }

            searchPaths = matched;
            commonAncestor = parent;
        }
        else
        {
            var searchPath = Path.GetFullPath(rawPath);

            if (!Directory.Exists(searchPath))
            {
                ansiConsole.MarkupLine($"Path does not exist: [green]{Markup.Escape(searchPath)}[/].");
                return 1;
            }

            searchPaths = [searchPath];
            commonAncestor = searchPath;
        }

        (IReadOnlyList<string> allCsprojFiles, IReadOnlyList<string> solutionFiles) = ansiConsole.WithSpinner(
            "Scanning csproj and solution files...",
            () =>
            {
                var csprojFiles = new List<string>();
                var slnFiles = new List<string>();
                foreach (var sp in searchPaths)
                {
                    var result = FileScanner.Scan(sp);
                    csprojFiles.AddRange(result.CsprojFiles);
                    slnFiles.AddRange(result.SolutionFiles);
                }
                csprojFiles.Sort();
                slnFiles.Sort();
                return (csprojFiles as IReadOnlyList<string>, slnFiles as IReadOnlyList<string>);
            });

        if (allCsprojFiles.Count == 0)
        {
            ansiConsole.WriteLine("No csproj files found in path.");
            return 0;
        }

        var projects = ansiConsole.WithSpinner(
            "Parsing projects...",
            () => CollectProjects(allCsprojFiles, solutionFiles));

        foreach (Project project in projects)
        {
            if (!settings.AbsolutePaths)
            {
                // Adjust path to be relative to common ancestor
                project.Path = Path.GetRelativePath(commonAncestor, project.Path);
            }

            if (project.Solution is not null)
            {
                if (!settings.ShowPaths)
                    project.Solution = Path.GetFileName(project.Solution);
                else if (!settings.AbsolutePaths)
                    project.Solution = Path.GetRelativePath(commonAncestor, project.Solution);
                // else: ShowPaths + AbsolutePaths → keep the stored absolute path as-is
            }
        }

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(projects, JsonOptions);
            _rawOutput.WriteLine(json);
            return 0;
        }

        if (solutionFiles.Count > 0)
        {
            foreach (var group in projects.GroupBy(p => p.Solution))
            {
                if (group.Key is null)
                    continue;

                ansiConsole.Write(Utilities.FormatProjects(group.ToList(), settings.ShowPaths, group.Key));
            }

            var dangling = projects.Where(p => p.Solution is null).ToList();
            if (dangling.Count > 0)
            {
                ansiConsole.Write(Utilities.FormatProjects(dangling, settings.ShowPaths,
                    "Dangling (not part of any solution)"));
            }
        }
        else
        {
            ansiConsole.Write(Utilities.FormatProjects(projects, settings.ShowPaths, null));
        }

        ansiConsole.MarkupLine($"Found [green]{allCsprojFiles.Count}[/] project(s).");

        return 0;
    }

    private static List<Project> CollectProjects(IReadOnlyList<string> allCsprojFiles, IReadOnlyList<string> solutionFiles)
    {
        if (solutionFiles.Count == 0)
        {
            return allCsprojFiles
                .Select(ProjectParser.Parse)
                .ToList();
        }

        var solutions = solutionFiles.Select(SolutionParser.Parse).ToList();

        var claimedProjectPaths = new HashSet<string>(
            solutions.SelectMany(s => s.ProjectPaths),
            StringComparer.OrdinalIgnoreCase);

        var projects = new List<Project>();

        foreach (var solution in solutions)
        {
            foreach (var projectPath in solution.ProjectPaths.Where(File.Exists))
            {
                var project = ProjectParser.Parse(projectPath);
                project.Solution = solution.Path;
                projects.Add(project);
            }
        }

        // Add dangling projects (not part of any solution)
        var danglingProjects = allCsprojFiles
            .Where(f => !claimedProjectPaths.Contains(Path.GetFullPath(f)))
            .Select(ProjectParser.Parse);

        projects.AddRange(danglingProjects);

        return projects;
    }
}
