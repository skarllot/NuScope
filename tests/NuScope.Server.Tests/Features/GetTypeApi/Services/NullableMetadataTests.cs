using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Raiqub.NuScope.Features.GetTypeApi.Services;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class NullableMetadataTests
{
    [Theory]
    [InlineData(new byte[] { 0, 0, 2, 0, 0 }, "Invalid nullable attribute prolog.")]
    [InlineData(new byte[] { 1, 0, 255, 255, 255, 255, 0, 0 }, "Invalid nullable attribute flags.")]
    [InlineData(new byte[] { 1, 0, 4, 0, 0, 0, 2, 1, 2, 0, 0 }, "Invalid nullable attribute flags.")]
    public void RejectsMalformedNullableAttributes(byte[] attribute, string message)
    {
        using var stream = CreateAssembly(attribute);

        var exception = Assert.Throws<BadImageFormatException>(() =>
            new NuGetTypeApiReader().ReadTypeApi(stream, "Example.Sample", false)
        );

        Assert.Equal(message, exception.Message);
        Assert.True(stream.CanRead);
    }

    [Theory]
    [InlineData(new byte[] { 1, 0, 2, 0, 0 }, "string?[]?[]?")]
    [InlineData(new byte[] { 1, 0, 3, 0, 0, 0, 1, 2, 1, 0, 0 }, "string[]?[]")]
    [InlineData(new byte[] { 1, 0, 3, 0, 0, 0, 2, 1, 2, 0, 0 }, "string?[][]?")]
    [InlineData(new byte[] { 1, 0, 2, 0, 0, 0, 2, 1, 0, 0 }, "string[][]?")]
    [InlineData(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }, "string[][]")]
    public void DecodesFlagsAndTreatsMissingEntriesAsOblivious(byte[] attribute, string fieldType)
    {
        using var stream = CreateAssembly(attribute);

        var api = new NuGetTypeApiReader().ReadTypeApi(stream, "Example.Sample", false);

        Assert.Equal(
            "namespace Example\n{\n    public class Sample\n    {\n        public " + fieldType + " Items;\n    }\n}\n",
            api?.ReplaceLineEndings("\n")
        );
    }

    private static MemoryStream CreateAssembly(byte[] attribute)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(
            0,
            metadata.GetOrAddString("Fixture.dll"),
            metadata.GetOrAddGuid(Guid.NewGuid()),
            default,
            default
        );
        metadata.AddAssembly(
            metadata.GetOrAddString("Fixture"),
            new Version(1, 0),
            default,
            default,
            0,
            AssemblyHashAlgorithm.None
        );
        var runtime = metadata.AddAssemblyReference(
            metadata.GetOrAddString("System.Runtime"),
            new Version(10, 0),
            default,
            default,
            0,
            default
        );
        var objectType = metadata.AddTypeReference(
            runtime,
            metadata.GetOrAddString("System"),
            metadata.GetOrAddString("Object")
        );
        var nullableType = metadata.AddTypeReference(
            runtime,
            metadata.GetOrAddString("System.Runtime.CompilerServices"),
            metadata.GetOrAddString("NullableAttribute")
        );
        // Instance void .ctor(byte) or .ctor(byte[]), matching the supplied custom attribute blob.
        var constructorSignature = attribute.Length == 5 ? new byte[] { 0x20, 1, 1, 5 } : [0x20, 1, 1, 0x1d, 5];
        var constructor = metadata.AddMemberReference(
            nullableType,
            metadata.GetOrAddString(".ctor"),
            metadata.GetOrAddBlob(constructorSignature)
        );
        metadata.AddTypeDefinition(
            TypeAttributes.NotPublic,
            default,
            metadata.GetOrAddString("<Module>"),
            default,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1)
        );
        metadata.AddTypeDefinition(
            TypeAttributes.Public,
            metadata.GetOrAddString("Example"),
            metadata.GetOrAddString("Sample"),
            objectType,
            MetadataTokens.FieldDefinitionHandle(1),
            MetadataTokens.MethodDefinitionHandle(1)
        );
        // FIELD SZARRAY SZARRAY STRING: each of the three reference nodes consumes one nullable flag.
        var field = metadata.AddFieldDefinition(
            FieldAttributes.Public,
            metadata.GetOrAddString("Items"),
            metadata.GetOrAddBlob(new byte[] { 6, 0x1d, 0x1d, 0x0e })
        );
        metadata.AddCustomAttribute(field, constructor, metadata.GetOrAddBlob(attribute));
        var pe = new ManagedPEBuilder(
            PEHeaderBuilder.CreateLibraryHeader(),
            new MetadataRootBuilder(metadata),
            new BlobBuilder(),
            flags: CorFlags.ILOnly
        );
        var image = new BlobBuilder();
        pe.Serialize(image);
        return new MemoryStream(image.ToArray());
    }
}
