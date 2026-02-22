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

        [Description("Show number of projects found")]
        [CommandOption("-c|--count")]
        public bool Count { get; set; }

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

        var projects = allCsprojFiles
            .OrderBy(f => f)
            .Select(ProjectParser.Parse)
            .ToList();

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
        }
        else
        {
            ansiConsole.Write(Utilities.FormatProjects(projects, settings.ShowPaths));
        }

        if (settings.Count)
        {
            ansiConsole.MarkupLine($"Found [green]{allCsprojFiles.Length}[/] project(s).");
        }

        return 0;
    }
}
