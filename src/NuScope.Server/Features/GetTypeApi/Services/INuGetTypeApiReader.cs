namespace Raiqub.NuScope.Features.GetTypeApi.Services;

public interface INuGetTypeApiReader
{
    string? ReadTypeApi(Stream stream, string fullTypeName, bool includePrivate);
}
