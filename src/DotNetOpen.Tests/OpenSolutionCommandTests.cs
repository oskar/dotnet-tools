using System;
using System.IO;
using System.Threading;
using Spectre.Console.Testing;
using Xunit;

namespace DotNetOpen.Tests;

public class OpenSolutionCommandTests : IDisposable
{
    private readonly string _tempDirectory;

    public OpenSolutionCommandTests()
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
    public void Execute_returns_0_when_no_solution_found()
    {
        // Arrange
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = _tempDirectory
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.Equal("No solution (.sln or .slnx) found in path.", console.Output.Trim());
    }

    [Theory]
    [InlineData("Solution.sln")]
    [InlineData("Solution.slnx")]
    public void Execute_opens_single_solution(string fileName)
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, fileName), "");
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = _tempDirectory
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.Equal($"Opening {Path.Combine(_tempDirectory, fileName)}.", console.Output.Trim());
    }

    [Fact]
    public void Execute_finds_both_sln_and_slnx_files()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution1.sln"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution2.slnx"), "");
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = _tempDirectory,
            First = true
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.StartsWith("Found 2 solutions in", console.Lines[0]);
    }

    [Fact]
    public void Execute_opens_first_of_multiple_solutions_when_first_flag_set()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution1.sln"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution2.sln"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution3.sln"), "");
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = _tempDirectory,
            First = true
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.StartsWith("Found 3 solutions in", console.Lines[0]);
        Assert.Equal($"Opening {Path.Combine(_tempDirectory, "Solution1.sln")}.", console.Lines[1]);
    }

    [Fact(Skip = "Not properly implemented yet.")]
    public void Execute_shows_selection_prompt_when_multiple_solutions_found_and_first_flag_not_set()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution1.sln"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution2.sln"), "");
        File.WriteAllText(Path.Combine(_tempDirectory, "Solution3.sln"), "");
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = _tempDirectory,
            First = false
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Execute_uses_current_directory_when_search_path_is_empty()
    {
        // Arrange
        var console = CreateTestConsole();
        var command = new OpenSolutionCommand(console);
        var settings = new OpenSolutionCommand.Settings
        {
            Path = ""
        };

        // Act
        var result = command.Execute(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.Equal("No solution (.sln or .slnx) found in path.", console.Output.Trim());
    }

    private static TestConsole CreateTestConsole()
    {
        var console = new TestConsole();

        // Make sure the console is wide enough to avoid wrapping when printing the solution path
        console.Profile.Width = 1024;

        return console;
    }
}
