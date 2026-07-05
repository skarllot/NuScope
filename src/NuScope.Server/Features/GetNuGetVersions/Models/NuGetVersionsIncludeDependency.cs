using System.Text.Json.Serialization;

namespace Raiqub.NuScope.Features.GetNuGetVersions.Models;

[JsonConverter(typeof(JsonStringEnumConverter<NuGetVersionsIncludeDependency>))]
public enum NuGetVersionsIncludeDependency
{
    [JsonStringEnumMemberName("targetFrameworks")]
    TargetFrameworks,

    [JsonStringEnumMemberName("full")]
    Full,
}
