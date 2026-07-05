using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetVersions.Models;
using Raiqub.NuScope.Features.GetNuGetVersions.Tools;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetNuGetVersions.Tools;

public sealed class GetNuGetVersionsToolTests
{
    [Fact]
    public void GetNuGetVersionsReturnsVersionsResultWhenPackageExists()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "12.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", minimumMajor: 13);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3"], success.Select(item => item.Version).ToArray());
        Assert.All(success, item => Assert.Null(item.DependencyGroups));
    }

    [Fact]
    public void GetNuGetVersionsIncludesPrereleaseVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3-beta.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", minimumMajor: 13, includePreRelease: true);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3", "13.0.3-beta.1"], success.Select(item => item.Version).ToArray());
        Assert.All(success, item => Assert.Null(item.DependencyGroups));
    }

    [Fact]
    public void GetNuGetVersionsLimitsVersionsWhenRequested()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "11.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "12.0.1"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "13.0.3"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json", maxItems: 2);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["13.0.3", "12.0.1"], success.Select(item => item.Version).ToArray());
        Assert.All(success, item => Assert.Null(item.DependencyGroups));
    }

    [Fact]
    public void GetNuGetVersionsDefaultsToFiveVersions()
    {
        var packageRootDirectory = GetPackageRootDirectory("Newtonsoft.Json");
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "1.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "2.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "3.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "4.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "5.0.0"));
        fileSystem.AddDirectory(Path.Combine(packageRootDirectory, "6.0.0"));

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Newtonsoft.Json");

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["6.0.0", "5.0.0", "4.0.0", "3.0.0", "2.0.0"], success.Select(item => item.Version).ToArray());
    }

    [Fact]
    public void GetNuGetVersionsUsesExplicitMaxItems()
    {
        var metadataService = new RecordingNuGetPackageMetadataService(
            NuGetPackageVersionsLookup.Found(["6.0.0", "5.0.0"])
        );

        var result = new GetNuGetVersionsTool(metadataService).GetNuGetVersions("Newtonsoft.Json", maxItems: 2);

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["6.0.0", "5.0.0"], success.Select(item => item.Version).ToArray());
        Assert.Equal(2, metadataService.MaxItems);
    }

    [Fact]
    public void GetNuGetVersionsOmitsDependenciesByDefault()
    {
        var metadataService = new RecordingNuGetPackageMetadataService(NuGetPackageVersionsLookup.Found(["13.0.3"]));

        var result = new GetNuGetVersionsTool(metadataService).GetNuGetVersions("Newtonsoft.Json");

        var success = Assert.IsType<NuGetVersionsResult>(result);
        var item = Assert.Single(success);
        Assert.Equal("13.0.3", item.Version);
        Assert.Null(item.DependencyGroups);
        Assert.Empty(metadataService.MetadataVersions);
    }

    [Fact]
    public void GetNuGetVersionsIncludesDependencyTargetFrameworksWhenRequested()
    {
        var metadataService = new RecordingNuGetPackageMetadataService(
            NuGetPackageVersionsLookup.Found(["13.0.3"]),
            CreateMetadataWithDependencies()
        );

        var result = new GetNuGetVersionsTool(metadataService).GetNuGetVersions(
            "Newtonsoft.Json",
            includeDependency: NuGetVersionsIncludeDependency.TargetFrameworks
        );

        var success = Assert.IsType<NuGetVersionsResult>(result);
        var item = Assert.Single(success);
        var dependencyGroups = Assert.IsAssignableFrom<IReadOnlyList<NuGetVersionDependencyGroup>>(
            item.DependencyGroups
        );
        var dependencyGroup = Assert.Single(dependencyGroups);
        Assert.Equal("net10.0", dependencyGroup.TargetFramework);
        Assert.Null(dependencyGroup.Dependencies);
        Assert.Equal(["13.0.3"], metadataService.MetadataVersions);
    }

    [Fact]
    public void GetNuGetVersionsIncludesFullDependenciesWhenRequested()
    {
        var metadataService = new RecordingNuGetPackageMetadataService(
            NuGetPackageVersionsLookup.Found(["13.0.3"]),
            CreateMetadataWithDependencies()
        );

        var result = new GetNuGetVersionsTool(metadataService).GetNuGetVersions(
            "Newtonsoft.Json",
            includeDependency: NuGetVersionsIncludeDependency.Full
        );

        var success = Assert.IsType<NuGetVersionsResult>(result);
        var item = Assert.Single(success);
        var dependencyGroups = Assert.IsAssignableFrom<IReadOnlyList<NuGetVersionDependencyGroup>>(
            item.DependencyGroups
        );
        var dependencyGroup = Assert.Single(dependencyGroups);
        var dependencies = Assert.IsAssignableFrom<IReadOnlyList<string>>(dependencyGroup.Dependencies);
        var dependency = Assert.Single(dependencies);
        Assert.Equal("net10.0", dependencyGroup.TargetFramework);
        Assert.Equal("System.Text.Json [10.0.0, )", dependency);
        Assert.Equal(["13.0.3"], metadataService.MetadataVersions);
    }

    [Fact]
    public void GetNuGetVersionsKeepsDependencyGroupsPerVersion()
    {
        var metadataService = new RecordingNuGetPackageMetadataService(
            NuGetPackageVersionsLookup.Found(["2.0.0", "1.0.0"]),
            new Dictionary<string, NuGetPackageMetadata>(StringComparer.OrdinalIgnoreCase)
            {
                ["2.0.0"] = CreateMetadataWithDependencies("2.0.0", "net10.0", "System.Text.Json", "[10.0.0, )"),
                ["1.0.0"] = CreateMetadataWithDependencies("1.0.0", "netstandard2.0", "System.Memory", "[4.5.5, )"),
            }
        );

        var result = new GetNuGetVersionsTool(metadataService).GetNuGetVersions(
            "Newtonsoft.Json",
            maxItems: 2,
            includeDependency: NuGetVersionsIncludeDependency.Full
        );

        var success = Assert.IsType<NuGetVersionsResult>(result);
        Assert.Equal(["2.0.0", "1.0.0"], success.Select(item => item.Version).ToArray());

        var currentGroups = Assert.IsAssignableFrom<IReadOnlyList<NuGetVersionDependencyGroup>>(
            success[0].DependencyGroups
        );
        var currentGroup = Assert.Single(currentGroups);
        Assert.Equal("net10.0", currentGroup.TargetFramework);
        var currentDependencies = Assert.IsAssignableFrom<IReadOnlyList<string>>(currentGroup.Dependencies);
        Assert.Equal("System.Text.Json [10.0.0, )", Assert.Single(currentDependencies));

        var previousGroups = Assert.IsAssignableFrom<IReadOnlyList<NuGetVersionDependencyGroup>>(
            success[1].DependencyGroups
        );
        var previousGroup = Assert.Single(previousGroups);
        Assert.Equal("netstandard2.0", previousGroup.TargetFramework);
        var previousDependencies = Assert.IsAssignableFrom<IReadOnlyList<string>>(previousGroup.Dependencies);
        Assert.Equal("System.Memory [4.5.5, )", Assert.Single(previousDependencies));
        Assert.Equal(["2.0.0", "1.0.0"], metadataService.MetadataVersions);
    }

    [Fact]
    public void NuGetVersionItemOmitsDependencyGroupsWhenEmpty()
    {
        var item = new NuGetVersionItem { Version = "13.0.3" };

        var json = JsonSerializer.Serialize(item);

        Assert.Contains("\"Version\":\"13.0.3\"", json);
        Assert.DoesNotContain("DependencyGroups", json);
    }

    [Fact]
    public void NuGetVersionDependencyGroupOmitsDependenciesWhenEmpty()
    {
        var dependencyGroup = NuGetVersionDependencyGroup.FromTargetFramework(
            new NuGetDependencyGroup { TargetFramework = "net10.0" }
        );
        var fullDependencyGroup = NuGetVersionDependencyGroup.FromDependencyGroup(
            new NuGetDependencyGroup { TargetFramework = "netstandard2.0" }
        );

        var json = JsonSerializer.Serialize(dependencyGroup);
        var fullJson = JsonSerializer.Serialize(fullDependencyGroup);

        Assert.Contains("\"TargetFramework\":\"net10.0\"", json);
        Assert.DoesNotContain("Dependencies", json);
        Assert.Contains("\"TargetFramework\":\"netstandard2.0\"", fullJson);
        Assert.DoesNotContain("Dependencies", fullJson);
    }

    [Fact]
    public void NuGetVersionDependencyGroupSerializesDependenciesAsStrings()
    {
        var dependencyGroup = NuGetVersionDependencyGroup.FromDependencyGroup(
            new NuGetDependencyGroup
            {
                TargetFramework = "net10.0",
                Dependencies = [new NuGetDependency { Id = "JasperFX", Version = "2.18.1" }],
            }
        );

        var json = JsonSerializer.Serialize(dependencyGroup);

        Assert.Contains("\"Dependencies\":[\"JasperFX 2.18.1\"]", json);
    }

    [Fact]
    public void NuGetVersionsIncludeDependencySerializesAsStringValues()
    {
        Assert.Equal("\"targetFrameworks\"", JsonSerializer.Serialize(NuGetVersionsIncludeDependency.TargetFrameworks));
        Assert.Equal("\"full\"", JsonSerializer.Serialize(NuGetVersionsIncludeDependency.Full));
        Assert.Equal(
            NuGetVersionsIncludeDependency.TargetFrameworks,
            JsonSerializer.Deserialize<NuGetVersionsIncludeDependency>("\"targetFrameworks\"")
        );
        Assert.Equal(
            NuGetVersionsIncludeDependency.Full,
            JsonSerializer.Deserialize<NuGetVersionsIncludeDependency>("\"full\"")
        );
    }

    [Fact]
    public void GetNuGetVersionsReturnsNotFoundWhenPackageIsMissing()
    {
        var fileSystem = new MockFileSystem();

        var result = new GetNuGetVersionsTool(
            new NuGetPackageMetadataService(fileSystem, new NuGetPackageMetadataParser())
        ).GetNuGetVersions("Missing.Package");

        var problem = Assert.IsType<NuGetProblemDetailsResult>(result);
        Assert.Equal(ProblemTypes.NotFound, problem.Type);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal(404, problem.Status);
        Assert.Contains("versions were not found", problem.Detail);
    }

    private static string GetPackageRootDirectory(string packageName)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages", packageName.ToLowerInvariant());
    }

    private static NuGetPackageMetadata CreateMetadataWithDependencies(
        string version = "13.0.3",
        string targetFramework = "net10.0",
        string dependencyId = "System.Text.Json",
        string dependencyVersion = "[10.0.0, )"
    ) =>
        new()
        {
            Id = "Newtonsoft.Json",
            Version = version,
            DependencyGroups =
            [
                new NuGetDependencyGroup
                {
                    TargetFramework = targetFramework,
                    Dependencies =
                    [
                        new NuGetDependency
                        {
                            Id = dependencyId,
                            Version = dependencyVersion,
                            Exclude = "Build",
                        },
                    ],
                },
            ],
        };

    private sealed class RecordingNuGetPackageMetadataService(
        NuGetPackageVersionsLookup versionsResult,
        NuGetPackageMetadata? metadata = null,
        IReadOnlyDictionary<string, NuGetPackageMetadata>? metadataByVersion = null
    ) : INuGetPackageMetadataService
    {
        public RecordingNuGetPackageMetadataService(
            NuGetPackageVersionsLookup versionsResult,
            IReadOnlyDictionary<string, NuGetPackageMetadata> metadataByVersion
        )
            : this(versionsResult, metadata: null, metadataByVersion) { }

        public int? MaxItems { get; private set; }

        public List<string> MetadataVersions { get; } = [];

        public NuGetPackageMetadataLookup GetNuGetPackageMetadata(string packageName, string? version = null)
        {
            MetadataVersions.Add(version!);
            if (metadataByVersion?.TryGetValue(version!, out var versionMetadata) is true)
            {
                return NuGetPackageMetadataLookup.Found(versionMetadata);
            }

            return NuGetPackageMetadataLookup.Found(
                metadata ?? new NuGetPackageMetadata { Id = packageName, Version = version! }
            );
        }

        public NuGetPackageVersionsLookup GetNuGetPackageVersions(
            string packageName,
            int? minimumMajor = null,
            bool includePreRelease = false,
            int? maxItems = null
        )
        {
            MaxItems = maxItems;
            return versionsResult;
        }
    }
}
