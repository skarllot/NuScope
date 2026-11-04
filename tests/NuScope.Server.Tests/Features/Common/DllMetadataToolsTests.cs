using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Features.GetTypeApi.Tools;
using Raiqub.NuScope.Features.ListTypes.Models;
using Raiqub.NuScope.Features.ListTypes.Services;
using Raiqub.NuScope.Features.ListTypes.Tools;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Raiqub.NuScope.Tests.Features.ListTypes.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common;

public sealed class DllMetadataToolsTests
{
    private static readonly string DllPath = Path.GetFullPath("fixtures/Example.dll");

    [Fact]
    public void ListTypesReturnsSameTypesAsMetadataReader()
    {
        var bytes = File.ReadAllBytes(typeof(PublicClassFixture).Assembly.Location);
        var service = CreateService(bytes);
        var result = new DllListTypesTool(service).ListTypes(DllPath, @"\.Fixtures\..*Fixture$", true);

        var assembly = Assert.Single(
            CallToolResultAssertions.DeserializeSuccessfulContent<NuGetTypeAssemblyResult[]>(result)
        );
        using var stream = new MemoryStream(bytes);
        var expected = new NuGetAssemblyTypeReader().ReadTypes(
            "Example.dll",
            stream,
            new System.Text.RegularExpressions.Regex(@"\.Fixtures\..*Fixture$"),
            true,
            false
        );
        Assert.Equal(expected.Assembly, assembly.Assembly);
        Assert.Equal(expected.Types, assembly.Types);
        Assert.Empty(assembly.Exported);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListTypesHonorsPrivateFlag(bool includePrivate)
    {
        var result = new DllListTypesTool(CreateService()).ListTypes(
            DllPath,
            "InternalTypeFixture|PrivateNested",
            includePrivate
        );
        var assemblies = CallToolResultAssertions.DeserializeSuccessfulContent<NuGetTypeAssemblyResult[]>(result);

        if (includePrivate)
        {
            Assert.Equal(2, Assert.Single(assemblies).Types.Count);
        }
        else
        {
            Assert.Empty(assemblies);
        }
    }

    [Fact]
    public void ListTypesResolvesRelativePathsAndReturnsEmptyForNoMatches()
    {
        var result = new DllListTypesTool(CreateService()).ListTypes("fixtures/Example.dll", "^Missing$");

        Assert.Empty(CallToolResultAssertions.DeserializeSuccessfulContent<NuGetTypeAssemblyResult[]>(result));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ListTypesHonorsExportedFlag(bool includeExported)
    {
        var result = new DllListTypesTool(CreateService(CreateForwarder("Uri"))).ListTypes(
            DllPath,
            "^System\\.Uri$",
            includeExported: includeExported
        );
        var assemblies = CallToolResultAssertions.DeserializeSuccessfulContent<NuGetTypeAssemblyResult[]>(result);

        if (includeExported)
        {
            var assembly = Assert.Single(assemblies);
            Assert.Empty(assembly.Types);
            Assert.Equal(["type System.Uri"], assembly.Exported);
        }
        else
        {
            Assert.Empty(assemblies);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTypeApiReturnsSameApiAsMetadataReader(bool includePrivate)
    {
        var bytes = File.ReadAllBytes(typeof(TypeApiFixture<>).Assembly.Location);
        var fullName = typeof(TypeApiFixture<>).FullName!;
        var result = new DllGetTypeApiTool(CreateService(bytes)).GetTypeApi(DllPath, fullName, includePrivate);

        Assert.True(result.IsError != true);
        var resource = Assert.IsType<TextResourceContents>(
            Assert.IsType<EmbeddedResourceBlock>(Assert.Single(result.Content)).Resource
        );
        using var stream = new MemoryStream(bytes);
        var expectedApi = new NuGetTypeApiReader().ReadTypeApi(stream, fullName, includePrivate);
        Assert.Equal($"// Assembly: Example.dll{Environment.NewLine}{expectedApi}", resource.Text);
        Assert.Equal("text/x-csharp", resource.MimeType);
        Assert.Equal(
            $"dll://assemblies/{Uri.EscapeDataString(DllPath)}/{Uri.EscapeDataString(fullName)}.cs",
            resource.Uri
        );
        Assert.Equal(includePrivate, resource.Text.Contains("private int secret;", StringComparison.Ordinal));
    }

    [Fact]
    public void GetTypeApiHonorsPrivateTypeVisibility()
    {
        var tool = new DllGetTypeApiTool(CreateService());
        var fullName = typeof(TypeApiHiddenSample).FullName!;

        AssertProblem(tool.GetTypeApi(DllPath, fullName), 404);
        Assert.True(tool.GetTypeApi(DllPath, fullName, true).IsError != true);
    }

    [Fact]
    public void GetTypeApiSupportsNestedTypes()
    {
        var result = new DllGetTypeApiTool(CreateService()).GetTypeApi(
            DllPath,
            typeof(PublicClassFixture.PublicNested).FullName!
        );

        Assert.True(result.IsError != true);
    }

    [Theory]
    [InlineData("", 400)]
    [InlineData(" ", 400)]
    [InlineData("missing.dll", 404)]
    [InlineData("missing-directory/missing.dll", 404)]
    [InlineData("invalid\0.dll", 400)]
    public void BothToolsReturnStructuredProblemsForInvalidPaths(string path, int status)
    {
        var service = CreateService();

        AssertProblem(new DllListTypesTool(service).ListTypes(path), status);
        AssertProblem(new DllGetTypeApiTool(service).GetTypeApi(path, "Example.Type"), status);
    }

    [Fact]
    public void BothToolsRejectUnreadableMetadata()
    {
        var service = CreateService([1, 2, 3]);

        AssertProblem(new DllListTypesTool(service).ListTypes(DllPath), 400);
        AssertProblem(new DllGetTypeApiTool(service).GetTypeApi(DllPath, "Example.Type"), 400);
    }

    [Fact]
    public void ListTypesRejectsInvalidAndTimedOutRegex()
    {
        AssertProblem(new DllListTypesTool(CreateService()).ListTypes(DllPath, "["), 400);
        var result = new DllListTypesTool(CreateService(CreateForwarder(new string('a', 40000) + "!"))).ListTypes(
            DllPath,
            "^(System\\.)?(a+)+$",
            includeExported: true
        );
        AssertProblem(result, 400);
        Assert.Contains("timed out", result.StructuredContent!.Value.Deserialize<NuGetProblemDetailsResult>()!.Detail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetTypeApiRejectsMissingNameAndReportsMissingType(bool includePrivate)
    {
        var tool = new DllGetTypeApiTool(CreateService());

        AssertProblem(tool.GetTypeApi(DllPath, " "), 400);
        var result = tool.GetTypeApi(DllPath, "Missing.Type", includePrivate);
        AssertProblem(result, 404);
        var problem = result.StructuredContent!.Value.Deserialize<NuGetProblemDetailsResult>()!;
        Assert.Equal(
            includePrivate
                ? $"Type 'Missing.Type' was not found in DLL '{DllPath}'."
                : $"Type 'Missing.Type' was not found in DLL '{DllPath}', or it is not public.",
            problem.Detail
        );
    }

    private static DllMetadataService CreateService(byte[]? bytes = null)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), Environment.CurrentDirectory);
        fileSystem.AddFile(
            DllPath,
            new MockFileData(bytes ?? File.ReadAllBytes(typeof(PublicClassFixture).Assembly.Location))
        );
        return new DllMetadataService(fileSystem, new NuGetAssemblyTypeReader(), new NuGetTypeApiReader());
    }

    private static void AssertProblem(CallToolResult result, int status)
    {
        Assert.True(result.IsError);
        var problem = result.StructuredContent!.Value.Deserialize<NuGetProblemDetailsResult>()!;
        Assert.Equal(status, problem.Status);
        Assert.Equal(problem.Detail, Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    private static byte[] CreateForwarder(string name)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(
            0,
            metadata.GetOrAddString("Forwarders.dll"),
            metadata.GetOrAddGuid(Guid.NewGuid()),
            default,
            default
        );
        metadata.AddAssembly(
            metadata.GetOrAddString("Forwarders"),
            new Version(1, 0, 0, 0),
            default,
            default,
            default,
            AssemblyHashAlgorithm.None
        );
        var reference = metadata.AddAssemblyReference(
            metadata.GetOrAddString("System.Runtime"),
            new Version(1, 0, 0, 0),
            default,
            default,
            default,
            default
        );
        metadata.AddExportedType(
            TypeAttributes.Public | (TypeAttributes)0x00200000,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString(name),
            reference,
            0
        );
        var builder = new ManagedPEBuilder(
            new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(metadata),
            new BlobBuilder(),
            flags: CorFlags.ILOnly
        );
        var blob = new BlobBuilder();
        builder.Serialize(blob);
        return blob.ToArray();
    }
}
