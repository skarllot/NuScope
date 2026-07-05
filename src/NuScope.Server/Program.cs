using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raiqub.NuScope.Features.Common.Services;
using Raiqub.NuScope.Features.GetNuGetMetadata.Tools;
using Raiqub.NuScope.Features.GetNuGetUrls.Tools;
using Raiqub.NuScope.Features.GetNuGetVersions.Tools;
using Raiqub.NuScope.Features.ListTypes.Services;
using Raiqub.NuScope.Features.ListTypes.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<INuGetPackageMetadataParser, NuGetPackageMetadataParser>();
builder.Services.AddSingleton<INuGetRemotePackageMetadataClient, NuGetOrgPackageMetadataClient>();
builder.Services.AddSingleton<INuGetPackageMetadataService, NuGetPackageMetadataService>();
builder.Services.AddSingleton<INuGetPackageAssetResolver, NuGetPackageAssetResolver>();
builder.Services.AddSingleton<INuGetAssemblyTypeReader, NuGetAssemblyTypeReader>();
builder.Services.AddSingleton<INuGetPackageTypeListingService, NuGetPackageTypeListingService>();

builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GetNuGetMetadataTool>()
    .WithTools<GetNuGetUrlsTool>()
    .WithTools<GetNuGetVersionsTool>()
    .WithTools<NuGetListTypesTool>();

await builder.Build().RunAsync();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program;
