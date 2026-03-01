using System;
using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotNetOverview;

public static class Utilities
{
    /// <summary>
    /// Runs <paramref name="func"/> with a status spinner when the console is interactive,
    /// or directly when it is not, and returns its result.
    /// </summary>
    /// <remarks>
    /// The guard is necessary because Spectre.Console's <c>Status</c> widget uses a
    /// <c>FallbackStatusRenderer</c> on non-interactive consoles that writes each status
    /// string as a plain text line. Calling <c>Status().Start()</c> unconditionally would
    /// therefore pollute output in CI pipelines, piped commands, and unit tests that capture
    /// <c>console.Output</c>.
    /// </remarks>
    public static T WithSpinner<T>(this IAnsiConsole console, string status, Func<T> func)
    {
        if (console.Profile.Capabilities.Interactive)
        {
            return console.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .Start(status, _ => func());
        }

        return func();
    }

    public static IRenderable FormatProjects(ICollection<Project> projects, bool showPath, string? title)
    {
        var table = new Table()
          .AddColumn("Project")
          .AddColumn("Target framework")
          .AddColumn("SDK")
          .BorderColor(Color.DarkGreen);

        if (title is not null)
        {
            table.Title = new TableTitle(title);
        }

        foreach (var project in projects)
        {
            table.AddRow(
              showPath ? project.Path : project.Name,
              project.TargetFramework ?? "",
              project.Sdk ?? ""
            );
        }

        return table;
    }
}
