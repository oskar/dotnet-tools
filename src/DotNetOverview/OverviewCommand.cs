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

public sealed class OverviewCommand(IAnsiConsole ansiConsole) : Command<OverviewCommand.Settings>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true
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

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
        var searchPath = string.IsNullOrEmpty(settings.Path)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(settings.Path);

        if (!Directory.Exists(searchPath))
        {
            ansiConsole.MarkupLine($"Path does not exist: [green]{searchPath}[/].");
            return 1;
        }

        // Discover all csproj files
        var allCsprojFiles = Directory.EnumerateFiles(searchPath, "*.csproj", EnumerationOptions)
            .ToArray();

        if (allCsprojFiles.Length == 0)
        {
            ansiConsole.WriteLine("No csproj files found in path.");
            return 0;
        }

        // Discover solution files
        var slnFiles = Directory.EnumerateFiles(searchPath, "*.sln", EnumerationOptions);
        var slnxFiles = Directory.EnumerateFiles(searchPath, "*.slnx", EnumerationOptions);
        string[] solutionFiles = [.. slnFiles, .. slnxFiles];
        Array.Sort(solutionFiles);

        var projects = CollectProjects(allCsprojFiles, solutionFiles);

        if (!settings.AbsolutePaths)
        {
            // Make paths relative to search path.
            foreach (Project project in projects)
            {
                project.Path = Path.GetRelativePath(searchPath, project.Path);
            }
        }

        if (settings.Json)
        {
            var json = JsonSerializer.Serialize(projects, JsonOptions);
            ansiConsole.WriteLine(json);
            return 0;
        }

        if (solutionFiles.Length > 0)
        {
            foreach (var group in projects.GroupBy(p => p.SolutionFileName))
            {
                if (group.Key is null)
                    continue;

                ansiConsole.Write(Utilities.FormatProjects(group.ToList(), settings.ShowPaths, group.Key));
            }

            var dangling = projects.Where(p => p.SolutionFileName is null).ToList();
            if (dangling.Count > 0)
            {
                ansiConsole.Write(Utilities.FormatProjects(dangling, settings.ShowPaths,
                    "Dangling (not part of any solution)"));
            }
        }
        else
        {
            ansiConsole.Write(Utilities.FormatProjects(projects, settings.ShowPaths));
        }

        ansiConsole.MarkupLine($"Found [green]{allCsprojFiles.Length}[/] project(s).");

        return 0;
    }

    private static List<Project> CollectProjects(string[] allCsprojFiles, string[] solutionFiles)
    {
        if (solutionFiles.Length == 0)
        {
            return allCsprojFiles
                .OrderBy(f => f)
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
                project.SolutionFileName = solution.Name;
                projects.Add(project);
            }
        }

        // Add dangling projects (not part of any solution)
        var danglingProjects = allCsprojFiles
            .Where(f => !claimedProjectPaths.Contains(Path.GetFullPath(f)))
            .OrderBy(f => f)
            .Select(ProjectParser.Parse);

        projects.AddRange(danglingProjects);

        return projects;
    }
}
