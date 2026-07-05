using System.Text.RegularExpressions;
using Raiqub.NuScope.Features.ListTypes.Models;

namespace Raiqub.NuScope.Features.ListTypes.Services;

public interface INuGetAssemblyTypeReader
{
    NuGetTypeAssemblyResult ReadTypes(
        string assemblyName,
        Stream stream,
        Regex? filter,
        bool includePrivate,
        bool includeExported
    );
}
