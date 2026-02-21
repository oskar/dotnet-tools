using System;
using System.IO;
using Xunit;

namespace DotNetOverview.Tests;

public class SolutionParserTests : IDisposable
{
    private readonly string _tempDirectory;

    public SolutionParserTests()
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
    public void Parse_throws_on_null() =>
        Assert.Throws<ArgumentNullException>(() => SolutionParser.Parse(null!));

    [Fact]
    public void Parse_throws_on_empty_argument() =>
        Assert.Throws<ArgumentNullException>(() => SolutionParser.Parse(""));

    [Fact]
    public void Parse_throws_on_missing_file() =>
        Assert.Throws<ArgumentException>(() => SolutionParser.Parse("thisfiledoesnotexist.sln"));

    [Fact]
    public void Parse_extracts_projects_from_sln_file()
    {
        // Arrange
        CreateSubdirectoryWithCsproj("ProjectA");
        CreateSubdirectoryWithCsproj("ProjectB");

        var slnContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectA", "ProjectA\ProjectA.csproj", "{00000000-0000-0000-0000-000000000001}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ProjectB", "ProjectB\ProjectB.csproj", "{00000000-0000-0000-0000-000000000002}"
            EndProject
            Global
            EndGlobal
            """;
        var slnPath = CreateTempFile("Test.sln", slnContent);

        // Act
        var result = SolutionParser.Parse(slnPath);

        // Assert
        Assert.Equal("Test.sln", result.Name);
        Assert.Equal(slnPath, result.Path);
        Assert.Equal(2, result.ProjectPaths.Count);
        Assert.Contains(result.ProjectPaths, p => p.EndsWith(Path.Combine("ProjectA", "ProjectA.csproj"), StringComparison.Ordinal));
        Assert.Contains(result.ProjectPaths, p => p.EndsWith(Path.Combine("ProjectB", "ProjectB.csproj"), StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_extracts_projects_from_slnx_file()
    {
        // Arrange
        CreateSubdirectoryWithCsproj("ProjectA");
        CreateSubdirectoryWithCsproj("ProjectB");

        var slnxContent = """
            <Solution>
              <Project Path="ProjectA/ProjectA.csproj" />
              <Project Path="ProjectB/ProjectB.csproj" />
            </Solution>
            """;
        var slnxPath = CreateTempFile("Test.slnx", slnxContent);

        // Act
        var result = SolutionParser.Parse(slnxPath);

        // Assert
        Assert.Equal("Test.slnx", result.Name);
        Assert.Equal(slnxPath, result.Path);
        Assert.Equal(2, result.ProjectPaths.Count);
        Assert.Contains(result.ProjectPaths, p => p.EndsWith(Path.Combine("ProjectA", "ProjectA.csproj"), StringComparison.Ordinal));
        Assert.Contains(result.ProjectPaths, p => p.EndsWith(Path.Combine("ProjectB", "ProjectB.csproj"), StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_filters_non_csproj_projects()
    {
        // Arrange
        CreateSubdirectoryWithCsproj("CSharpProject");
        var vbDir = Path.Combine(_tempDirectory, "VBProject");
        Directory.CreateDirectory(vbDir);
        File.WriteAllText(Path.Combine(vbDir, "VBProject.vbproj"), "<Project />");

        var slnContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CSharpProject", "CSharpProject\CSharpProject.csproj", "{00000000-0000-0000-0000-000000000001}"
            EndProject
            Project("{F184B08F-C81C-45F6-A57F-5ABD9991F28F}") = "VBProject", "VBProject\VBProject.vbproj", "{00000000-0000-0000-0000-000000000002}"
            EndProject
            Global
            EndGlobal
            """;
        var slnPath = CreateTempFile("Test.sln", slnContent);

        // Act
        var result = SolutionParser.Parse(slnPath);

        // Assert
        Assert.Single(result.ProjectPaths);
        Assert.Contains(result.ProjectPaths, p => p.EndsWith(Path.Combine("CSharpProject", "CSharpProject.csproj"), StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_returns_empty_list_for_empty_solution()
    {
        // Arrange
        var slnxContent = """
            <Solution>
            </Solution>
            """;
        var slnxPath = CreateTempFile("Empty.slnx", slnxContent);

        // Act
        var result = SolutionParser.Parse(slnxPath);

        // Assert
        Assert.Equal("Empty.slnx", result.Name);
        Assert.Empty(result.ProjectPaths);
    }

    private string CreateTempFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private void CreateSubdirectoryWithCsproj(string projectName)
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
}
