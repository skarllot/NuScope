using System.Xml.Linq;
using Raiqub.NuSpec.Features.Common.Models;

namespace Raiqub.NuSpec.Features.Common.Services;

public sealed class NuGetPackageMetadataParser : INuGetPackageMetadataParser
{
    public NuGetPackageMetadata? Parse(Stream stream)
    {
        XDocument document;

        try
        {
            document = XDocument.Load(stream);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        var metadata = document.Root?.Elements().FirstOrDefault(element => element.Name.LocalName == "metadata");
        if (metadata is null)
        {
            return null;
        }

        return new NuGetPackageMetadata
        {
            Id = Value(metadata, "id"),
            Version = Value(metadata, "version"),
            Title = Value(metadata, "title"),
            Authors = Value(metadata, "authors"),
            Owners = Value(metadata, "owners"),
            Description = Value(metadata, "description"),
            Summary = Value(metadata, "summary"),
            ReleaseNotes = Value(metadata, "releaseNotes"),
            Language = Value(metadata, "language"),
            ProjectUrl = Value(metadata, "projectUrl"),
            IconUrl = Value(metadata, "iconUrl"),
            Icon = Value(metadata, "icon"),
            LicenseUrl = Value(metadata, "licenseUrl"),
            LicenseType = Element(metadata, "license")?.Attribute("type")?.Value,
            License = Element(metadata, "license")?.Value,
            Repository = ParseRepository(metadata),
            RequireLicenseAcceptance =
                Value(metadata, "requireLicenseAcceptance") is { } requireLicenseAcceptance
                && bool.TryParse(requireLicenseAcceptance, out var parsedRequireLicenseAcceptance)
                && parsedRequireLicenseAcceptance,
            Copyright = Value(metadata, "copyright"),
            Readme = Value(metadata, "readme"),
            Tags = SplitTags(Value(metadata, "tags")),
            DependencyGroups = GetDependencyGroups(metadata),
        };
    }

    private static string[] SplitTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static NuGetDependencyGroup[] GetDependencyGroups(XElement metadata)
    {
        var dependencies = Element(metadata, "dependencies");
        if (dependencies is null)
        {
            return [];
        }

        var groups = Elements(dependencies, "group")
            .Select(group => new NuGetDependencyGroup
            {
                TargetFramework = group.Attribute("targetFramework")?.Value,
                Dependencies = GetDependencies(group),
            })
            .ToArray();

        if (groups.Length > 0)
        {
            return groups;
        }

        var directDependencies = GetDependencies(dependencies);
        return directDependencies.Length == 0 ? [] : [new NuGetDependencyGroup { Dependencies = directDependencies }];
    }

    private static NuGetDependency[] GetDependencies(XElement element) =>
        Elements(element, "dependency")
            .Select(dependency => new NuGetDependency
            {
                Id = dependency.Attribute("id")?.Value,
                Version = dependency.Attribute("version")?.Value,
                Exclude = dependency.Attribute("exclude")?.Value,
            })
            .ToArray();

    private static NuGetRepositoryMetadata? ParseRepository(XElement metadata)
    {
        var repository = Element(metadata, "repository");
        return repository is null
            ? null
            : new NuGetRepositoryMetadata
            {
                Type = repository.Attribute("type")?.Value,
                Url = repository.Attribute("url")?.Value,
                Branch = repository.Attribute("branch")?.Value,
                Commit = repository.Attribute("commit")?.Value,
            };
    }

    private static string? Value(XElement metadata, string name) => Element(metadata, name)?.Value;

    private static XElement? Element(XElement element, string localName) =>
        Elements(element, localName).FirstOrDefault();

    private static IEnumerable<XElement> Elements(XElement element, string localName) =>
        element.Elements().Where(child => child.Name.LocalName == localName);
}
