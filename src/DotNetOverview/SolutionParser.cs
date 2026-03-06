using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace DotNetOverview;

public static class SolutionParser
{
    public static Solution Parse(string solutionFilePath)
    {
        if (string.IsNullOrEmpty(solutionFilePath))
            throw new ArgumentNullException(nameof(solutionFilePath));

        if (!File.Exists(solutionFilePath))
            throw new ArgumentException($"Solution file does not exist ({solutionFilePath})", nameof(solutionFilePath));

        var solutionDirectory = Path.GetDirectoryName(solutionFilePath)!;

        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath)
            ?? throw new ArgumentException($"Unsupported solution file format ({solutionFilePath})", nameof(solutionFilePath));
        SolutionModel solutionModel;
        try
        {
            solutionModel = serializer.OpenAsync(solutionFilePath, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is XmlException or SolutionException)
        {
            throw new InvalidOperationException($"Solution file could not be parsed ({solutionFilePath})", ex);
        }

        var projectPaths = solutionModel.SolutionProjects
            .Select(p => Path.GetFullPath(Path.Combine(solutionDirectory, p.FilePath)))
            .Where(p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p)
            .ToList();

        return new Solution
        {
            Name = Path.GetFileName(solutionFilePath),
            Path = solutionFilePath,
            ProjectPaths = projectPaths
        };
    }
}
