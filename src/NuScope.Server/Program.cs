using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raiqub.NuSpec.Features.Common.Services;
using Raiqub.NuSpec.Features.GetNuGetMetadata.Tools;
using Raiqub.NuSpec.Features.GetNuGetUrls.Tools;

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

builder
    .Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GetNuGetMetadataTool>()
    .WithTools<GetNuGetUrlsTool>();

await builder.Build().RunAsync();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program;
