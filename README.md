# dotnet-tools

A collection of .NET tools for common development tasks.

## [dotnet-overview](src/DotNetOverview/README.md)

[![NuGet version](https://img.shields.io/nuget/v/dotnet-overview)](https://www.nuget.org/packages/dotnet-overview) [![NuGet downloads](https://img.shields.io/nuget/dt/dotnet-overview)](https://www.nuget.org/packages/dotnet-overview)

Display an overview of all `.csproj` files in the current directory or
any specified path, showing project names, target frameworks, output
types, and SDK information. When solution files (`.sln`/`.slnx`) are
found, projects are grouped by solution. Supports JSON output for
advanced filtering.

```bash
❯ dotnet tool install -g dotnet-overview
❯ dotnet overview
                    DotNetTools.sln
┌──────────────────────┬──────────────────┬─────────────┬─────────────────────┐
│ Project              │ Target framework │ Output type │ SDK                 │
├──────────────────────┼──────────────────┼─────────────┼─────────────────────┤
│ DotNetOpen.Tests     │ net8.0           │             │ Microsoft.NET.Sdk   │
│ DotNetOpen           │ net8.0           │ Exe         │ Microsoft.NET.Sdk   │
│ DotNetOverview.Tests │ net8.0           │             │ Microsoft.NET.Sdk   │
│ DotNetOverview       │ net8.0           │ Exe         │ Microsoft.NET.Sdk   │
└──────────────────────┴──────────────────┴─────────────┴─────────────────────┘
Found 4 project(s).
```

## [dotnet-open](src/DotNetOpen/README.md)

[![NuGet version](https://img.shields.io/nuget/v/dotnet-open)](https://www.nuget.org/packages/dotnet-open) [![NuGet downloads](https://img.shields.io/nuget/dt/dotnet-open)](https://www.nuget.org/packages/dotnet-open)

Find and open solution files in the current directory or any specified
path. Presents an interactive menu when multiple solutions are found.

```bash
❯ dotnet tool install -g dotnet-open
❯ dotnet open
Opening /Users/oskar/git/dotnet-tools/DotNetTools.sln.
```
