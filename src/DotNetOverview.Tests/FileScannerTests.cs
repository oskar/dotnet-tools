using System;
using System.IO;
using System.Linq;
using Xunit;

namespace DotNetOverview.Tests;

public class FileScannerTests : IDisposable
{
    private readonly string _tempDirectory;

    public FileScannerTests()
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
    public void Scan_returns_empty_results_when_no_matching_files_exist()
    {
        CreateFile("readme.txt");
        CreateFile("Program.cs");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Empty(result.CsprojFiles);
        Assert.Empty(result.SolutionFiles);
    }

    [Fact]
    public void Scan_finds_csproj_files()
    {
        CreateFile("MyApp.csproj");
        CreateFile("MyLib.csproj");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Equal(2, result.CsprojFiles.Length);
        Assert.Contains(result.CsprojFiles, f => Path.GetFileName(f) == "MyApp.csproj");
        Assert.Contains(result.CsprojFiles, f => Path.GetFileName(f) == "MyLib.csproj");
    }

    [Fact]
    public void Scan_finds_sln_files()
    {
        CreateFile("MySolution.sln");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Single(result.SolutionFiles);
        Assert.Contains(result.SolutionFiles, f => Path.GetFileName(f) == "MySolution.sln");
    }

    [Fact]
    public void Scan_finds_slnx_files()
    {
        CreateFile("MySolution.slnx");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Single(result.SolutionFiles);
        Assert.Contains(result.SolutionFiles, f => Path.GetFileName(f) == "MySolution.slnx");
    }

    [Fact]
    public void Scan_finds_both_sln_and_slnx_files()
    {
        CreateFile("A.sln");
        CreateFile("B.slnx");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Equal(2, result.SolutionFiles.Length);
    }

    [Fact]
    public void Scan_recurses_into_subdirectories()
    {
        CreateFile("Repo1/src/App/App.csproj");
        CreateFile("Repo2/src/Lib/Lib.csproj");
        CreateFile("Repo1/Repo1.sln");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Equal(2, result.CsprojFiles.Length);
        Assert.Single(result.SolutionFiles);
    }

    [Fact]
    public void Scan_skips_git_directory()
    {
        CreateFile("Project.csproj");
        CreateFile(".git/hooks/Hidden.csproj");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Single(result.CsprojFiles);
        Assert.DoesNotContain(result.CsprojFiles, f => f.Contains(".git"));
    }

    [Fact]
    public void Scan_skips_bin_directory()
    {
        CreateFile("Project.csproj");
        CreateFile("bin/Debug/net8.0/Hidden.csproj");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Single(result.CsprojFiles);
        Assert.DoesNotContain(result.CsprojFiles, f => Path.GetFileName(f) == "Hidden.csproj");
    }

    [Fact]
    public void Scan_skips_obj_directory()
    {
        CreateFile("Project.csproj");
        CreateFile("obj/Debug/net8.0/Hidden.csproj");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.Single(result.CsprojFiles);
        Assert.DoesNotContain(result.CsprojFiles, f => Path.GetFileName(f) == "Hidden.csproj");
    }

    [Fact]
    public void Scan_solution_files_are_sorted_alphabetically()
    {
        CreateFile("Z.sln");
        CreateFile("A.sln");
        CreateFile("M.slnx");

        var result = FileScanner.Scan(_tempDirectory);

        var names = result.SolutionFiles.Select(f => Path.GetFileName(f)!).ToArray();
        Assert.Equal(["A.sln", "M.slnx", "Z.sln"], names);
    }

    [Fact]
    public void Scan_returns_absolute_paths()
    {
        CreateFile("Project.csproj");
        CreateFile("Solution.sln");

        var result = FileScanner.Scan(_tempDirectory);

        Assert.All(result.CsprojFiles, f => Assert.True(Path.IsPathRooted(f)));
        Assert.All(result.SolutionFiles, f => Assert.True(Path.IsPathRooted(f)));
    }

    private void CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(_tempDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "");
    }
}
