using System.IO.Abstractions.TestingHelpers;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.DependencyInjection;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.ListTypes.Models;
using Raiqub.NuScope.Features.ListTypes.Services;
using Raiqub.NuScope.Tests.Features.ListTypes.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.ListTypes.Services;

public sealed class NuGetPackageTypeListingServiceTests
{
    private const string PackageName = "Package.With.Types";
    private const string Version = "1.0.0";

    [Fact]
    public void ListTypesServiceCanBeResolvedFromDependencyInjection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<INuGetPackageAssetResolver>(_ => new NuGetPackageAssetResolver(
            new MockFileSystem(),
            new HttpClient(new NotFoundHttpMessageHandler())
        ));
        services.AddSingleton<INuGetAssemblyTypeReader, NuGetAssemblyTypeReader>();
        services.AddSingleton<INuGetPackageTypeListingService, NuGetPackageTypeListingService>();
        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<INuGetPackageTypeListingService>();

        Assert.IsType<NuGetPackageTypeListingService>(service);
    }

    [Fact]
    public void ListTypesReturnsPublicKindsFromLocalPackage()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            GetFixtureAssemblyBytes()
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(
            PackageName,
            Version,
            "net8.0",
            @"\.Fixtures\.(I?Public)(ClassFixture|DelegateFixture|EnumFixture|InterfaceFixture|StructFixture)$"
        );

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("lib/Types.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(
            [
                "interface Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.IPublicInterfaceFixture",
                "class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture",
                "delegate Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicDelegateFixture",
                "enum Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicEnumFixture",
                "struct Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicStructFixture",
            ],
            assembly.Types
        );
    }

    [Fact]
    public void ListTypesHidesNonPublicTypesByDefault()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            GetFixtureAssemblyBytes()
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "InternalTypeFixture|PrivateNested");

        Assert.Empty(AssertFound(result));
    }

    [Fact]
    public void ListTypesIncludesNonPublicTypesWhenRequested()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            GetFixtureAssemblyBytes()
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(
            PackageName,
            Version,
            "net8.0",
            "InternalTypeFixture|PrivateNested",
            includePrivate: true
        );

        var assembly = Assert.Single(AssertFound(result));
        Assert.Empty(assembly.Exported);
        Assert.Equal(
            [
                "class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.InternalTypeFixture",
                "class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture+PrivateNested",
            ],
            assembly.Types
        );
    }

    [Fact]
    public void ListTypesAppliesFilterRegexToFullNameOnly()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            GetFixtureAssemblyBytes()
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "^class ");

        Assert.Empty(AssertFound(result));
    }

    [Fact]
    public void ListTypesReturnsMultipleAssemblyGroupsSortedByFilename()
    {
        var packageDirectory = GetPackageDirectory(PackageName, Version);
        var firstDllPath = Path.Combine(packageDirectory, "lib", "net8.0", "Zeta.dll");
        var secondDllPath = Path.Combine(packageDirectory, "lib", "net8.0", "Alpha.dll");
        var assemblyBytes = GetFixtureAssemblyBytes();
        var fileSystem = new MockFileSystem(
            new Dictionary<string, MockFileData>
            {
                [firstDllPath] = new MockFileData(assemblyBytes),
                [secondDllPath] = new MockFileData(assemblyBytes),
            }
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "PublicClassFixture$");

        var assemblies = AssertFound(result);
        Assert.Equal(["lib/Alpha.dll", "lib/Zeta.dll"], assemblies.Select(assembly => assembly.Assembly));
    }

    [Fact]
    public void ListTypesUsesCompatibleTargetFrameworkFallback()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "netstandard2.0",
            "Types.dll",
            GetFixtureAssemblyBytes()
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "PublicClassFixture$");

        var assembly = Assert.Single(AssertFound(result));
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture"], assembly.Types);
    }

    [Fact]
    public void ListTypesReturnsRefPrefixedAssemblyNamesFromLocalPackage()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            GetFixtureAssemblyBytes(),
            root: "ref"
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "PublicClassFixture$");

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("ref/Types.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture"], assembly.Types);
    }

    [Fact]
    public void ListTypesHidesForwardedExportedTypesByDefault()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            CreateAssemblyWithExportedType("System", "Uri"),
            root: "ref"
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "^System\\.Uri$");

        Assert.Empty(AssertFound(result));
    }

    [Fact]
    public void ListTypesIncludesForwardedExportedTypesWhenRequested()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "Types.dll",
            CreateAssemblyWithExportedType("System", "Uri"),
            root: "ref"
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "^System\\.Uri$", includeExported: true);

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("ref/Types.dll", assembly.Assembly);
        Assert.Equal(["type System.Uri"], assembly.Exported);
        Assert.Empty(assembly.Types);
    }

    [Fact]
    public void ListTypesIncludesExportedForwardersRegardlessOfVisibilityFlags()
    {
        var fileSystem = CreateFileSystemWithDll(
            PackageName,
            Version,
            "net8.0",
            "mscorlib.dll",
            CreateAssemblyWithExportedType("Microsoft.Win32", "Registry", TypeAttributes.NotPublic),
            root: "ref"
        );
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0", "Registry$", includeExported: true);

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("ref/mscorlib.dll", assembly.Assembly);
        Assert.Equal(["type Microsoft.Win32.Registry"], assembly.Exported);
        Assert.Empty(assembly.Types);
    }

    [Fact]
    public void ListTypesFallsBackToNuGetOrgPackageArchiveWhenLocalPackageIsMissing()
    {
        var packageBytes = CreatePackageArchive("net8.0", "Remote.dll", GetFixtureAssemblyBytes());
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(
                "https://api.nuget.org/v3-flatcontainer/package.with.types/1.0.0/package.with.types.1.0.0.nupkg",
                request.RequestUri!.AbsoluteUri
            );
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(packageBytes) };
        });
        var service = CreateService(new MockFileSystem(), handler);

        var result = service.ListTypes(PackageName, Version, "net8.0", "PublicClassFixture$");

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("lib/Remote.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture"], assembly.Types);
    }

    [Fact]
    public void ListTypesFallsBackToRefAssetsWhenRemotePackageHasNoLibAssets()
    {
        var packageBytes = CreatePackageArchive("net8.0", "Reference.dll", GetFixtureAssemblyBytes(), root: "ref");
        var service = CreateService(
            new MockFileSystem(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(packageBytes),
            })
        );

        var result = service.ListTypes("Microsoft.NETCore.App.Ref", "10.0.9", "net8.0", "PublicClassFixture$");

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("ref/Reference.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture"], assembly.Types);
    }

    [Fact]
    public void ListTypesSkipsUnreadableRemoteDllWhenOtherDllsAreReadable()
    {
        var packageBytes = CreatePackageArchive(
            ("ref", "net8.0", "Broken.dll", new byte[] { 1, 2, 3 }),
            ("ref", "net8.0", "Reference.dll", GetFixtureAssemblyBytes())
        );
        var service = CreateService(
            new MockFileSystem(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(packageBytes),
            })
        );

        var result = service.ListTypes("Microsoft.NETCore.App.Ref", "10.0.9", "net8.0", "PublicClassFixture$");

        var assembly = Assert.Single(AssertFound(result));
        Assert.Equal("ref/Reference.dll", assembly.Assembly);
        Assert.Empty(assembly.Exported);
        Assert.Equal(["class Raiqub.NuScope.Tests.Features.ListTypes.Fixtures.PublicClassFixture"], assembly.Types);
    }

    [Fact]
    public void ListTypesReturnsNotFoundWhenPackageIsMissing()
    {
        var service = CreateService(
            new MockFileSystem(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
        );

        var result = service.ListTypes("Missing.Package", Version, "net8.0");

        AssertProblem(result, ProblemTypes.NotFound, 404, "was not found");
    }

    [Fact]
    public void ListTypesReturnsNotFoundWhenNoCompatibleLibAssetsExist()
    {
        var packageDirectory = GetPackageDirectory(PackageName, Version);
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(Path.Combine(packageDirectory, "lib", "net48"));
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0");

        AssertProblem(result, ProblemTypes.NotFound, 404, "no compatible lib or ref assets");
    }

    [Fact]
    public void ListTypesReturnsNoDllAssetsWhenRemoteCompatibleFolderIsEmpty()
    {
        var packageBytes = CreatePackageArchiveWithEmptyLibFolders("net8.0", "net472");
        var service = CreateService(
            new MockFileSystem(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(packageBytes),
            })
        );

        var result = service.ListTypes("xunit.v3.core", "3.2.2", "net8.0");

        AssertProblem(result, ProblemTypes.NotFound, 404, "has no DLL assets for 'lib/net8.0'");
    }

    [Fact]
    public void ListTypesReturnsBadRequestWhenRegexIsInvalid()
    {
        var service = CreateService(new MockFileSystem());

        var result = service.ListTypes(PackageName, Version, "net8.0", "[");

        AssertProblem(result, ProblemTypes.BadRequest, 400, "filterRegex");
    }

    [Theory]
    [InlineData("", "1.0.0", "net8.0", "Package name is required")]
    [InlineData("Package/Name", "1.0.0", "net8.0", "Package name contains characters")]
    [InlineData("Package.Name", "", "net8.0", "Package version is required")]
    [InlineData("Package.Name", "1.0.0", "", "Target framework is required")]
    public void ListTypesReturnsBadRequestWhenInputIsInvalid(
        string packageName,
        string version,
        string targetFramework,
        string expectedDetail
    )
    {
        var service = CreateService(new MockFileSystem());

        var result = service.ListTypes(packageName, version, targetFramework);

        AssertProblem(result, ProblemTypes.BadRequest, 400, expectedDetail);
    }

    [Fact]
    public void ListTypesReturnsProblemWhenDllIsInvalid()
    {
        var fileSystem = CreateFileSystemWithDll(PackageName, Version, "net8.0", "Broken.dll", [1, 2, 3]);
        var service = CreateService(fileSystem);

        var result = service.ListTypes(PackageName, Version, "net8.0");

        AssertProblem(result, ProblemTypes.InternalServerError, 500, "no readable DLL metadata");
    }

    [Fact]
    public void ListTypesReturnsServiceUnavailableWhenNuGetOrgFails()
    {
        var service = CreateService(
            new MockFileSystem(),
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway))
        );

        var result = service.ListTypes(PackageName, Version, "net8.0");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "nuget.org returned 502");
    }

    private static NuGetPackageTypeListingService CreateService(
        MockFileSystem fileSystem,
        HttpMessageHandler? handler = null
    )
    {
        return new NuGetPackageTypeListingService(
            new NuGetPackageAssetResolver(fileSystem, new HttpClient(handler ?? new NotFoundHttpMessageHandler())),
            new NuGetAssemblyTypeReader()
        );
    }

    private static MockFileSystem CreateFileSystemWithDll(
        string packageName,
        string version,
        string targetFramework,
        string assemblyName,
        byte[] assemblyBytes,
        string root = "lib"
    )
    {
        var dllPath = Path.Combine(GetPackageDirectory(packageName, version), root, targetFramework, assemblyName);
        return new MockFileSystem(new Dictionary<string, MockFileData> { [dllPath] = new MockFileData(assemblyBytes) });
    }

    private static IReadOnlyList<NuGetTypeAssemblyResult> AssertFound(NuGetPackageTypesLookup result)
    {
        Assert.Null(result.Problem);
        return Assert.IsAssignableFrom<IReadOnlyList<NuGetTypeAssemblyResult>>(result.Assemblies);
    }

    private static void AssertProblem(
        NuGetPackageTypesLookup result,
        string expectedType,
        int expectedStatus,
        string expectedDetail
    )
    {
        Assert.Null(result.Assemblies);
        var problem = Assert.IsType<NuGetProblemDetailsResult>(result.Problem);
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
    }

    private static byte[] GetFixtureAssemblyBytes()
    {
        return File.ReadAllBytes(typeof(PublicClassFixture).Assembly.Location);
    }

    private static byte[] CreatePackageArchive(
        string targetFramework,
        string assemblyName,
        byte[] assemblyBytes,
        string root = "lib"
    )
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry($"{root}/{targetFramework}/{assemblyName}");
            using var entryStream = entry.Open();
            entryStream.Write(assemblyBytes);
        }

        return stream.ToArray();
    }

    private static byte[] CreatePackageArchive(
        params (string Root, string TargetFramework, string AssemblyName, byte[] AssemblyBytes)[] assemblies
    )
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var assembly in assemblies)
            {
                var entry = archive.CreateEntry($"{assembly.Root}/{assembly.TargetFramework}/{assembly.AssemblyName}");
                using var entryStream = entry.Open();
                entryStream.Write(assembly.AssemblyBytes);
            }
        }

        return stream.ToArray();
    }

    private static byte[] CreatePackageArchiveWithEmptyLibFolders(params string[] targetFrameworks)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var targetFramework in targetFrameworks)
            {
                archive.CreateEntry($"lib/{targetFramework}/");
            }
        }

        return stream.ToArray();
    }

    private static byte[] CreateAssemblyWithExportedType(
        string @namespace,
        string name,
        TypeAttributes attributes = TypeAttributes.Public
    )
    {
        var metadataBuilder = new MetadataBuilder();
        metadataBuilder.AddModule(
            generation: 0,
            moduleName: metadataBuilder.GetOrAddString("Forwarders.dll"),
            mvid: metadataBuilder.GetOrAddGuid(Guid.NewGuid()),
            encId: default,
            encBaseId: default
        );
        metadataBuilder.AddAssembly(
            name: metadataBuilder.GetOrAddString("Forwarders"),
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKey: default,
            flags: default,
            hashAlgorithm: AssemblyHashAlgorithm.None
        );
        var implementation = metadataBuilder.AddAssemblyReference(
            name: metadataBuilder.GetOrAddString("System.Runtime"),
            version: new Version(1, 0, 0, 0),
            culture: default,
            publicKeyOrToken: default,
            hashValue: default,
            flags: default
        );
        metadataBuilder.AddExportedType(
            attributes: attributes | (TypeAttributes)0x00200000,
            @namespace: metadataBuilder.GetOrAddString(@namespace),
            name: metadataBuilder.GetOrAddString(name),
            implementation: implementation,
            typeDefinitionId: 0
        );

        var peBuilder = new ManagedPEBuilder(
            new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(metadataBuilder),
            ilStream: new BlobBuilder(),
            flags: CorFlags.ILOnly
        );
        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        return peBlob.ToArray();
    }

    private static string GetPackageDirectory(string packageName, string version)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(
            userProfile,
            ".nuget",
            "packages",
            packageName.ToLowerInvariant(),
            version.ToLowerInvariant()
        );
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(responder(request));
        }
    }

    private sealed class NotFoundHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
