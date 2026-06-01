using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raiqub.NuSpec.Services;
using Raiqub.NuSpec.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<IFileSystem, FileSystem>();
builder.Services.AddSingleton<INuGetPackageMetadataParser, NuGetPackageMetadataParser>();
builder.Services.AddSingleton<INuGetPackageMetadataService, NuGetPackageMetadataService>();

builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<NuGetPackageMetadataTools>();

await builder.Build().RunAsync();
