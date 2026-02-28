using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;

namespace DotNetOverview;

public record ScanResult(string[] CsprojFiles, string[] SolutionFiles);

public static class FileScanner
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = true
    };

    public static ScanResult Scan(string searchPath)
    {
        var csprojList = new List<string>();
        var solutionList = new List<string>();

        var enumerable = new FileSystemEnumerable<string>(
            searchPath,
            static (ref FileSystemEntry entry) => entry.ToFullPath(),
            EnumerationOptions)
        {
            ShouldIncludePredicate = static (ref FileSystemEntry entry) =>
            {
                if (entry.IsDirectory) return false;
                var name = entry.FileName;
                return name.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);
            },
            ShouldRecursePredicate = static (ref FileSystemEntry entry) =>
            {
                var name = entry.FileName;
                return !name.Equals(".git", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    && !name.Equals("obj", StringComparison.OrdinalIgnoreCase);
            }
        };

        foreach (var file in enumerable)
        {
            if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                csprojList.Add(file);
            }
            else
            {
                solutionList.Add(file);
            }
        }

        var solutionFiles = solutionList.ToArray();
        Array.Sort(solutionFiles);

        return new ScanResult([.. csprojList], solutionFiles);
    }
}
