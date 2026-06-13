using System.Net;
using System.Net.Http;
using System.Text;
using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.Common.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests.Features.Common.Services;

public sealed class NuGetOrgPackageMetadataClientTests
{
    private const string ServiceIndexJson =
        """
        {
          "resources": [
            {
              "@id": "https://api.nuget.org/v3-flatcontainer/",
              "@type": "PackageBaseAddress/3.0.0"
            }
          ]
        }
        """;

    [Fact]
    public void GetNuGetPackageMetadataReturnsLatestRemoteMetadataWhenVersionIsNotProvided()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["13.0.2", "13.0.3-beta.1", "13.0.3"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/13.0.3/newtonsoft.json.nuspec" => XmlResponse(
                    """
                    <package>
                      <metadata>
                        <id>Newtonsoft.Json</id>
                        <version>13.0.3</version>
                      </metadata>
                    </package>
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Newtonsoft.Json");

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("Newtonsoft.Json", metadata.Id);
        Assert.Equal("13.0.3", metadata.Version);
    }

    [Fact]
    public void GetNuGetPackageMetadataUsesRequestedVersionAndLowercaseUrlSegments()
    {
        var seenUris = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            seenUris.Add(request.RequestUri!.AbsoluteUri);

            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0", "1.0.1"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.1/package.with.case.nuspec" =>
                    XmlResponse(
                        """
                        <package>
                          <metadata>
                            <id>Package.With.Case</id>
                            <version>1.0.1</version>
                          </metadata>
                        </package>
                        """
                    ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case", "1.0.1");

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("1.0.1", metadata.Version);
        Assert.Contains(
            "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.1/package.with.case.nuspec",
            seenUris
        );
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenRemoteVersionIsMissing()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/missing.package/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Missing.Package", "2.0.0");

        AssertProblem(result, ProblemTypes.NotFound, 404, "version '2.0.0' was not found on nuget.org");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenServiceIndexFails()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Any.Package");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "could not be reached");
    }

    [Fact]
    public void GetNuGetPackageMetadataRejectsAbsolutePackageIdentifiers()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new Xunit.Sdk.XunitException("Request should not be sent.")
        );
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageMetadata("https://example.com/package"));
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageMetadata("/Newtonsoft.Json"));
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageMetadata("\\Newtonsoft.Json"));
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageMetadata("Newtonsoft/Json"));
    }

    [Fact]
    public void GetNuGetPackageMetadataIgnoresMalformedVersions()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1..0", "2.0.0"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/2.0.0/package.with.case.nuspec" =>
                    XmlResponse(
                        """
                        <package>
                          <metadata>
                            <id>Package.With.Case</id>
                            <version>2.0.0</version>
                          </metadata>
                        </package>
                        """
                    ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.0", metadata.Version);
    }

    [Fact]
    public void GetNuGetPackageMetadataHandlesPrereleaseHeavyLists()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/prerelease.only/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["2.0.0-alpha", "2.0.0-beta", "2.0.1-rc1"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/prerelease.only/2.0.1-rc1/prerelease.only.nuspec" =>
                    XmlResponse(
                        """
                        <package>
                          <metadata>
                            <id>Prerelease.Only</id>
                            <version>2.0.1-rc1</version>
                          </metadata>
                        </package>
                        """
                    ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Prerelease.Only");

        var metadata = Assert.IsType<NuGetPackageMetadata>(result.Metadata);
        Assert.Equal("2.0.1-rc1", metadata.Version);
    }

    private static HttpResponseMessage JsonResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage XmlResponse(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/xml"),
        };
    }

    private static void AssertProblem(
        NuGetPackageMetadataLookup result,
        string expectedType,
        int expectedStatus,
        string expectedDetail
    )
    {
        Assert.Null(result.Metadata);
        var problem = Assert.IsType<NuGetProblemDetailsResult>(result.Problem);
        Assert.Equal(expectedType, problem.Type);
        Assert.Equal(expectedStatus, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
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
}
