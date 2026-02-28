using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotNetOverview;

public static class Utilities
{
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
