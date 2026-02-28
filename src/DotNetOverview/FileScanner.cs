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
            ShouldIncludePredicate = static (ref FileSystemEntry entry) => !entry.IsDirectory,
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
            var ext = Path.GetExtension(file);
            if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                csprojList.Add(file);
            }
            else if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                solutionList.Add(file);
            }
        }

        var solutionFiles = solutionList.ToArray();
        Array.Sort(solutionFiles);

        return new ScanResult([.. csprojList], solutionFiles);
    }
}
