using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Raiqub.NuScope.Features.GetTypeApi.Services.Rendering;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Metadata.MetadataTypeNames;
using static Raiqub.NuScope.Features.GetTypeApi.Services.Rendering.CSharpApiModifiers;

namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public sealed class NuGetTypeApiReader : INuGetTypeApiReader
{
    public string? ReadTypeApi(Stream stream, string fullTypeName, bool includePrivate)
    {
        var (bufferedStream, ownsBufferedStream) = GetSeekableStream(stream);
        try
        {
            using var peReader = new PEReader(bufferedStream, PEStreamOptions.LeaveOpen);
            if (!peReader.HasMetadata)
            {
                throw new BadImageFormatException("Assembly does not contain metadata.");
            }

            var reader = peReader.GetMetadataReader();
            foreach (var handle in reader.TypeDefinitions)
            {
                var type = reader.GetTypeDefinition(handle);
                if (
                    string.Equals(GetFullName(reader, handle), fullTypeName, StringComparison.Ordinal)
                    && (includePrivate || IsPublicApi(type.Attributes))
                )
                {
                    return new CSharpTypeApiRenderer(reader, includePrivate).Render(handle);
                }
            }

            return null;
        }
        finally
        {
            if (ownsBufferedStream)
            {
                bufferedStream.Dispose();
            }
        }
    }

    private static (Stream stream, bool ownsStream) GetSeekableStream(Stream stream)
    {
        if (stream.CanSeek)
        {
            return (stream, false);
        }

        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return (memoryStream, true);
    }
}
