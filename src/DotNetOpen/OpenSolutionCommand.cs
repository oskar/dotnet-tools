using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace DotNetOpen;

public sealed class OpenSolutionCommand(IAnsiConsole ansiConsole) : Command<OpenSolutionCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[path]")]
        public string? Path { get; set; }

        [Description("Print version of this tool and exit")]
        [CommandOption("-v|--version")]
        public bool Version { get; set; }

        [Description("Open first solution if multiple are found.")]
        [CommandOption("-f|--first")]
        public bool First { get; set; }
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

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true
        };
        var slnFiles = Directory.EnumerateFiles(searchPath, "*.sln", enumerationOptions);
        var slnxFiles = Directory.EnumerateFiles(searchPath, "*.slnx", enumerationOptions);
        string[] files = [.. slnFiles, .. slnxFiles];
        Array.Sort(files);

        if (files.Length == 0)
        {
            ansiConsole.WriteLine("No solution (.sln or .slnx) found in path.");
            return 0;
        }

        if (files.Length == 1)
        {
            OpenFile(files[0]);
            return 0;
        }

        ansiConsole.MarkupLine($"Found [green]{files.Length}[/] solutions in [green]{searchPath}[/].");

        if (settings.First)
        {
            OpenFile(files[0]);
            return 0;
        }

        var selectionPrompt = new SelectionPrompt<string>
        {
            Title = "Select which to open:",
            PageSize = 15,
            SearchEnabled = true,
            Converter = filePath => Path.GetRelativePath(searchPath, filePath)
        };
        selectionPrompt.AddChoices(files);

        string selectedSolution;
        try
        {
            selectedSolution = new EscCancellingConsole(ansiConsole).Prompt(selectionPrompt);
        }
        catch (EscapePressedException)
        {
            return 1;
        }

        OpenFile(selectedSolution);

        return 0;
    }

    private void OpenFile(string filePath)
    {
        ansiConsole.MarkupLine($"Opening [green]{filePath}[/].");
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }

    private sealed class EscapePressedException : Exception;

    // Remove when Spectre.Console natively supports ESC cancellation in SelectionPrompt
    // (tracked in https://github.com/spectreconsole/spectre.console/issues/851)
    private sealed class EscCancellingConsole(IAnsiConsole inner) : IAnsiConsole
    {
        private readonly EscCancellingInput _input = new(inner.Input);

        public Profile Profile => inner.Profile;
        public IAnsiConsoleCursor Cursor => inner.Cursor;
        public IAnsiConsoleInput Input => _input;
        public IExclusivityMode ExclusivityMode => inner.ExclusivityMode;
        public RenderPipeline Pipeline => inner.Pipeline;
        public void Clear(bool home) => inner.Clear(home);
        public void Write(IRenderable renderable) => inner.Write(renderable);

        private sealed class EscCancellingInput(IAnsiConsoleInput inner) : IAnsiConsoleInput
        {
            public bool IsKeyAvailable() => inner.IsKeyAvailable();

            public ConsoleKeyInfo? ReadKey(bool intercept)
            {
                var key = inner.ReadKey(intercept);
                return key?.Key == ConsoleKey.Escape ? throw new EscapePressedException() : key;
            }

            public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
            {
                var key = await inner.ReadKeyAsync(intercept, cancellationToken);
                return key?.Key == ConsoleKey.Escape ? throw new EscapePressedException() : key;
            }
        }
    }
}
