namespace DotNetOverview;

public class Project
{
    public required string Name { get; set; }
    public required string Path { get; set; }

    public bool? SdkFormat { get; set; }
    public string? TargetFramework { get; set; }
    public string? OutputType { get; set; }
    public string? Authors { get; set; }
    public string? Version { get; set; }
    public string? SolutionFileName { get; set; }
}
