using System;
using System.IO;
using System.Threading;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit;

namespace DotNetOverview.Tests;

public class OverviewCommandWildcardTests : IDisposable
{
    private readonly string _tempDirectory;

    public OverviewCommandWildcardTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
            Directory.Delete(_tempDirectory, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Wildcard_matches_single_directory()
    {
        // Arrange
        CreateCsprojFile("hsbrepo", "App");

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = Path.Combine(_tempDirectory, "hsb*") };

        // Act
        var exitCode = Execute(command, settings);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("1 project(s)", console.Output);
    }

    [Fact]
    public void Wildcard_matches_multiple_directories()
    {
        // Arrange
        CreateCsprojFile("hsb-repo1", "App");
        CreateCsprojFile("hsb-repo2", "Lib");

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = Path.Combine(_tempDirectory, "hsb*") };

        // Act
        var exitCode = Execute(command, settings);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("2 project(s)", console.Output);
    }

    [Fact]
    public void Wildcard_with_no_matches_returns_error()
    {
        // Arrange — _tempDirectory exists but has no subdirs matching "nomatch*"
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = Path.Combine(_tempDirectory, "nomatch*") };

        // Act
        var exitCode = Execute(command, settings);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Matches(@"No directories matched pattern:.*nomatch\*", console.Output.Trim());
    }

    [Fact]
    public void Wildcard_in_middle_of_path_matches_directories()
    {
        // Arrange — structure: _tempDirectory/group-a/src/App.csproj
        //                      _tempDirectory/group-b/src/Lib.csproj
        // Pattern: _tempDirectory/group-*/src
        var srcA = Path.Combine(_tempDirectory, "group-a", "src");
        var srcB = Path.Combine(_tempDirectory, "group-b", "src");
        Directory.CreateDirectory(srcA);
        Directory.CreateDirectory(srcB);
        File.WriteAllText(Path.Combine(srcA, "App.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(srcB, "Lib.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = Path.Combine(_tempDirectory, "group-*", "src") };

        // Act
        var exitCode = Execute(command, settings);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("2 project(s)", console.Output);
    }

    [Fact]
    public void Wildcard_parent_does_not_exist_returns_error()
    {
        // Arrange
        var nonexistentParent = Path.Combine(_tempDirectory, "nonexistent-parent");
        Assert.False(Directory.Exists(nonexistentParent));

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = Path.Combine(nonexistentParent, "hsb*") };

        // Act
        var exitCode = Execute(command, settings);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Matches(@"Path does not exist:.*nonexistent-parent", console.Output.Trim());
    }

    private void CreateCsprojFile(string subdirName, string projectName)
    {
        var dir = Path.Combine(_tempDirectory, subdirName, projectName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{projectName}.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
    }

    private static TestConsole CreateTestConsole()
    {
        var console = new TestConsole();
        console.Profile.Width = 1024;
        return console;
    }

    private static int Execute(OverviewCommand command, OverviewCommand.Settings settings) =>
        ((ICommand<OverviewCommand.Settings>)command).ExecuteAsync(null!, settings, CancellationToken.None).GetAwaiter().GetResult();
}
