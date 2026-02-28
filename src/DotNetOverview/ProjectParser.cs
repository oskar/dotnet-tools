using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace DotNetOverview;

public static class ProjectParser
{
    private static readonly XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

    public static Project Parse(string projectFilePath)
    {
        if (string.IsNullOrEmpty(projectFilePath))
            throw new ArgumentNullException(nameof(projectFilePath));

        if (!File.Exists(projectFilePath))
            throw new ArgumentException($"Project file does not exist ({projectFilePath})", nameof(projectFilePath));

        var project = new Project
        {
            Path = projectFilePath,
            Name = Path.GetFileNameWithoutExtension(projectFilePath)
        };

        XDocument xmlDoc;
        try
        {
            xmlDoc = XDocument.Load(projectFilePath);
        }
        catch (XmlException e)
        {
            throw new InvalidOperationException($"Failed to load project file: '{projectFilePath}'", e);
        }

        var sdk = GetSdk(xmlDoc);
        project.Sdk = sdk;

        if (sdk is not null)
        {
            project.TargetFramework =
              GetPropertyValue(xmlDoc, "TargetFramework") ??
              GetPropertyValue(xmlDoc, "TargetFrameworks");
        }
        else
        {
            project.TargetFramework = GetPropertyValue(xmlDoc, "TargetFrameworkVersion");
        }

        project.OutputType = GetPropertyValue(xmlDoc, "OutputType");
        project.Authors = GetPropertyValue(xmlDoc, "Authors");

        project.Version = GetPropertyValue(xmlDoc, "Version");
        if (string.IsNullOrWhiteSpace(project.Version))
        {
            var prefix = GetPropertyValue(xmlDoc, "VersionPrefix");
            var suffix = GetPropertyValue(xmlDoc, "VersionSuffix");

            project.Version = string.IsNullOrWhiteSpace(suffix) ? prefix : $"{prefix}-{suffix}";
        }

        return project;
    }

    private static string? GetSdk(XDocument document)
    {
        var value = document.Element("Project")?.Attribute("Sdk")?.Value;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? GetPropertyValue(XDocument document, string property)
    {
        var value = document.Element(MsBuildNamespace + "Project")
          ?.Elements(MsBuildNamespace + "PropertyGroup")
          .Elements(MsBuildNamespace + property)
          .Select(v => v.Value)
          .FirstOrDefault();

        if (!string.IsNullOrEmpty(value))
            return value;

        return document.Element("Project")
          ?.Elements("PropertyGroup")
          .Elements(property)
          .Select(v => v.Value)
          .FirstOrDefault();
    }
}
