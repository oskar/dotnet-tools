using System.Collections.Generic;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotNetOverview;

public static class Utilities
{
    public static IRenderable FormatProjects(ICollection<Project> projects, bool showPath = false)
    {
        var table = new Table()
          .AddColumn("Project")
          .AddColumn("Target framework")
          .AddColumn("SDK format")
          .BorderColor(Color.DarkGreen);

        foreach (var project in projects)
        {
            table.AddRow(
              showPath ? project.Path : project.Name,
              project.TargetFramework ?? "",
              project.SdkFormat.HasValue ? project.SdkFormat.Value ? "Yes" : "No" : "-"
            );
        }

        return table;
    }
}
