using System;
using System.IO;
using Xunit;

namespace DotNetOverview.Tests;

public class ProjectParserTests : IDisposable
{
    private readonly string _tempDirectory;

    public ProjectParserTests()
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
      Assert.Throws<ArgumentNullException>(() => ProjectParser.Parse(null!));

    [Fact]
    public void Parse_throws_on_empty_argument() =>
      Assert.Throws<ArgumentNullException>(() => ProjectParser.Parse(""));

    [Fact]
    public void Parse_throws_on_missing_file() =>
      Assert.Throws<ArgumentException>(() => ProjectParser.Parse("thisfiledoesnotexist.csproj"));

    [Fact]
    public void Parse_throws_on_invalid_xml()
    {
        var projectPath = CreateTempProjectFile("Corrupt.csproj", "not xml at all");

        var ex = Assert.Throws<InvalidOperationException>(() => ProjectParser.Parse(projectPath));
        Assert.Contains("Corrupt.csproj", ex.Message);
    }

    [Fact]
    public void Parse_supports_SDK_style_projects()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
          <OutputType>Exe</OutputType>
          <Authors>John Doe</Authors>
          <Version>1.2.3</Version>
        </PropertyGroup>
      </Project>
      """;
        var projectPath = CreateTempProjectFile("TestProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("TestProject", result.Name);
        Assert.Equal(projectPath, result.Path);
        Assert.True(result.SdkFormat);
        Assert.Equal("net8.0", result.TargetFramework);
        Assert.Equal("Exe", result.OutputType);
        Assert.Equal("John Doe", result.Authors);
        Assert.Equal("1.2.3", result.Version);
    }

    [Fact]
    public void Parse_supports_legacy_projects()
    {
        // Arrange
        var projectContent = """
                         <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                           <PropertyGroup>
                             <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
                             <OutputType>WinExe</OutputType>
                             <Authors>Jane Smith</Authors>
                             <Version>0.9.1</Version>
                           </PropertyGroup>
                         </Project>
                         """;

        var projectPath = CreateTempProjectFile("LegacyProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("LegacyProject", result.Name);
        Assert.Equal(projectPath, result.Path);
        Assert.False(result.SdkFormat);
        Assert.Equal("v4.7.2", result.TargetFramework);
        Assert.Equal("WinExe", result.OutputType);
        Assert.Equal("Jane Smith", result.Authors);
        Assert.Equal("0.9.1", result.Version);
    }

    [Fact]
    public void Parse_supports_multiple_target_frameworks()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFrameworks>net6.0;net8.0</TargetFrameworks>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("MultiTargetProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("MultiTargetProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("net6.0;net8.0", result.TargetFramework);
    }

    [Fact]
    public void Parse_combines_version_prefix_and_suffix()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
          <VersionPrefix>2.1.0</VersionPrefix>
          <VersionSuffix>alpha</VersionSuffix>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("VersionProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("VersionProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("2.1.0-alpha", result.Version);
    }

    [Fact]
    public void Parse_uses_version_prefix_only_as_version()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
          <VersionPrefix>3.0.0</VersionPrefix>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("PrefixOnlyProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("PrefixOnlyProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("3.0.0", result.Version);
    }

    [Fact]
    public void Parse_supports_minimal_project()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("MinimalProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("MinimalProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("net8.0", result.TargetFramework);
        Assert.Null(result.OutputType);
        Assert.Null(result.Authors);
        Assert.Null(result.Version);
    }

    [Fact]
    public void Parse_supports_multiple_property_groups()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
          <OutputType>Exe</OutputType>
        </PropertyGroup>
        <PropertyGroup>
          <Authors>Multi Group Author</Authors>
          <Version>4.5.6</Version>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("MultiGroupProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("MultiGroupProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("net8.0", result.TargetFramework);
        Assert.Equal("Exe", result.OutputType);
        Assert.Equal("Multi Group Author", result.Authors);
        Assert.Equal("4.5.6", result.Version);
    }

    [Fact]
    public void Parse_supports_empty_properties()
    {
        // Arrange
        var projectContent = """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup>
          <TargetFramework>net8.0</TargetFramework>
          <OutputType></OutputType>
          <Authors></Authors>
          <Version></Version>
        </PropertyGroup>
      </Project>
      """;

        var projectPath = CreateTempProjectFile("EmptyPropsProject.csproj", projectContent);

        // Act
        var result = ProjectParser.Parse(projectPath);

        // Assert
        Assert.Equal("EmptyPropsProject", result.Name);
        Assert.True(result.SdkFormat);
        Assert.Equal("net8.0", result.TargetFramework);
        Assert.Equal("", result.OutputType);
        Assert.Equal("", result.Authors);
        Assert.Null(result.Version);
    }

    private string CreateTempProjectFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }
}
