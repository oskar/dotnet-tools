using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Spectre.Console.Testing;
using Xunit;

namespace DotNetOverview.Tests;

public class OverviewCommandSolutionTests : IDisposable
{
    private readonly string _tempDirectory;

    public OverviewCommandSolutionTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Groups_projects_by_solution()
    {
        // Arrange
        CreateCsprojFile("ProjectA");
        CreateCsprojFile("ProjectB");
        CreateSlnxFile("TestSolution", ["ProjectA", "ProjectB"]);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Contains("TestSolution", console.Output);
        Assert.Contains("ProjectA", console.Output);
        Assert.Contains("ProjectB", console.Output);
    }

    [Fact]
    public void Shows_dangling_projects_warning()
    {
        // Arrange - solution with one project, plus a dangling project
        CreateCsprojFile("InSolution");
        CreateCsprojFile("Dangling");
        CreateSlnxFile("TestSolution", ["InSolution"]);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Contains("not part of any solution", console.Output);
        Assert.Contains("Dangling", console.Output);
    }

    [Fact]
    public void Json_output_includes_SolutionFileName()
    {
        // Arrange
        CreateCsprojFile("InSolution");
        CreateCsprojFile("Dangling");
        CreateSlnxFile("TestSolution", ["InSolution"]);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Length);

        var inSolution = Assert.Single(projects, p => p.Name == "InSolution");
        Assert.Equal("TestSolution.slnx", inSolution.SolutionFileName);

        var dangling = Assert.Single(projects, p => p.Name == "Dangling");
        Assert.Null(dangling.SolutionFileName);
    }

    [Fact]
    public void Paths_are_relative_by_default()
    {
        // Arrange
        CreateCsprojFile("MyProject");
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        var project = Assert.Single(projects);
        Assert.False(Path.IsPathRooted(project.Path), $"Expected relative path but got: {project.Path}");
        Assert.Equal(Path.Combine("MyProject", "MyProject.csproj"), project.Path);
    }

    [Fact]
    public void Absolute_paths_when_flag_set()
    {
        // Arrange
        CreateCsprojFile("MyProject");
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true, AbsolutePaths = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        var project = Assert.Single(projects);
        Assert.True(Path.IsPathRooted(project.Path), $"Expected absolute path but got: {project.Path}");
        Assert.Equal(Path.Combine(_tempDirectory, "MyProject", "MyProject.csproj"), project.Path);
    }

    [Fact]
    public void Paths_are_relative_by_default_with_solutions()
    {
        // Arrange
        CreateCsprojFile("ProjectA");
        CreateSlnxFile("TestSolution", ["ProjectA"]);
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        var project = Assert.Single(projects);
        Assert.False(Path.IsPathRooted(project.Path), $"Expected relative path but got: {project.Path}");
    }

    [Fact]
    public void Absolute_paths_when_flag_set_with_solutions()
    {
        // Arrange
        CreateCsprojFile("ProjectA");
        CreateSlnxFile("TestSolution", ["ProjectA"]);
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true, AbsolutePaths = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        var project = Assert.Single(projects);
        Assert.True(Path.IsPathRooted(project.Path), $"Expected absolute path but got: {project.Path}");
        Assert.StartsWith(_tempDirectory, project.Path);
    }

    [Fact]
    public void Groups_projects_by_sln_file()
    {
        // Arrange
        CreateCsprojFile("ProjectA");
        CreateCsprojFile("ProjectB");
        CreateSlnFile("TestSolution", ["ProjectA", "ProjectB"]);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Length);
        Assert.All(projects, p => Assert.Equal("TestSolution.sln", p.SolutionFileName));
    }

    [Fact]
    public void Project_in_multiple_solutions_appears_multiple_times()
    {
        // Arrange - one project referenced by two solutions
        CreateCsprojFile("SharedProject");
        CreateSlnxFile("SolutionA", ["SharedProject"]);
        CreateSlnxFile("SolutionB", ["SharedProject"]);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = _tempDirectory, Json = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        var projects = JsonSerializer.Deserialize<Project[]>(console.Output);
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Length);
        Assert.All(projects, p => Assert.Equal("SharedProject", p.Name));
        Assert.Contains(projects, p => p.SolutionFileName == "SolutionA.slnx");
        Assert.Contains(projects, p => p.SolutionFileName == "SolutionB.slnx");
    }

    private void CreateCsprojFile(string projectName)
    {
        var dir = Path.Combine(_tempDirectory, projectName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{projectName}.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
    }

    private void CreateSlnFile(string solutionName, string[] projectNames)
    {
        var projects = string.Join(Environment.NewLine,
            projectNames.Select(name =>
                $$"""
                Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{name}}", "{{name}}\{{name}}.csproj", "{00000000-0000-0000-0000-000000000000}"
                EndProject
                """));
        var content = $"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            {projects}
            Global
            EndGlobal
            """;
        File.WriteAllText(Path.Combine(_tempDirectory, $"{solutionName}.sln"), content);
    }

    private void CreateSlnxFile(string solutionName, string[] projectNames)
    {
        var projects = string.Join(Environment.NewLine,
            projectNames.Select(name => $"  <Project Path=\"{name}/{name}.csproj\" />"));
        var content = $"""
            <Solution>
            {projects}
            </Solution>
            """;
        File.WriteAllText(Path.Combine(_tempDirectory, $"{solutionName}.slnx"), content);
    }

    private static TestConsole CreateTestConsole()
    {
        var console = new TestConsole();
        console.Profile.Width = 1024;
        return console;
    }
}
