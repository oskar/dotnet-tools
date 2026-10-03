using System;
using System.IO;
using System.Threading;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using Xunit;

namespace DotNetOverview.Tests;

public class OverviewCommandTests
{
    [Fact]
    public void Prints_version_and_exits_when_Version_set()
    {
        // Arrange
        var semVerPattern = @"^\d+\.\d+\.\d+(-[\w.]+)?$";

        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Version = true };

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Matches(semVerPattern, console.Output);
    }

    [Fact]
    public void Checks_for_invalid_path()
    {
        // Arrange
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = "apaththatdoesnotexist" };
        Assert.False(Directory.Exists(settings.Path), $"Test prerequisite failed: Path '{settings.Path}' should not exist");

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Matches(@"^Path does not exist: .*apaththatdoesnotexist\.$", console.Output.Trim());
    }

    [Fact]
    public void Stops_if_no_csproj_files_found()
    {
        // Arrange
        var console = CreateTestConsole();
        var command = new OverviewCommand(console);
        var settings = new OverviewCommand.Settings { Path = "." }; // no csproj files exist in working directory when running tests
        Assert.True(Directory.Exists(settings.Path), $"Test prerequisite failed: Path '{settings.Path}' should exist");

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal("No csproj files found in path.", console.Output.Trim());
    }

    [Fact]
    public void Prints_json_if_requested()
    {
        // Arrange
        using var jsonOutput = new StringWriter();
        var console = CreateTestConsole();
        var command = new OverviewCommand(console, jsonOutput);
        var settings = new OverviewCommand.Settings()
        {
            Path = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent?.Parent?.Parent?.ToString() ?? "",
            Json = true
        };
        Assert.True(Directory.Exists(settings.Path), $"Test prerequisite failed: Path '{settings.Path}' should exist");

        // Act
        command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.True(IsJson(jsonOutput.ToString()));
    }

    [Fact]
    public void CommandApp_can_construct_and_run_command()
    {
        // Smoke test: verifies CommandApp's default type resolver can instantiate
        // OverviewCommand with its optional TextWriter parameter, catching any
        // regression where an unresolvable constructor parameter breaks activation.

        // Arrange
        var app = new CommandApp<OverviewCommand>();

        // Act
        var result = app.Run(["--version"]);

        // Assert
        Assert.Equal(0, result);
    }

    private static bool IsJson(string s)
    {
        try
        {
            System.Text.Json.JsonSerializer.Deserialize<object>(s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static TestConsole CreateTestConsole()
    {
        var console = new TestConsole();

        // Make sure the console is wide enough to avoid wrapping when printing the solution path
        console.Profile.Width = 1024;

        return console;
    }
}
