using System.Net;
using System.Net.Http;
using System.Text;
using Raiqub.NuScope.Features.Common.Models;
using Raiqub.NuScope.Features.Common.Services;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.Common.Services;

public sealed class NuGetOrgPackageMetadataClientTests
{
    private const string ServiceIndexJson = """
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

    [Theory]
    [InlineData("""{ "resources": [] }""")]
    [InlineData("""{ "resources": {} }""")]
    [InlineData("""{ "resources": [{ "@type": "PackageBaseAddress/3.0.0", "@id": "" }] }""")]
    [InlineData(
        """{ "resources": [{ "@type": "OtherResource/1.0.0", "@id": "https://api.nuget.org/v3-flatcontainer/" }] }"""
    )]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenPackageBaseAddressIsUnavailable(
        string serviceIndexJson
    )
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsoluteUri == "https://api.nuget.org/v3/index.json"
                ? JsonResponse(serviceIndexJson)
                : throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}")
        );

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Any.Package");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "did not advertise a package content endpoint");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenPackageIsMissingFromVersionsIndex()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/missing.package/index.json" => new HttpResponseMessage(
                    HttpStatusCode.NotFound
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Missing.Package");

        AssertProblem(result, ProblemTypes.NotFound, 404, "Package 'Missing.Package' was not found on nuget.org");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenVersionsRequestFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/failing.package/index.json" => new HttpResponseMessage(
                    HttpStatusCode.BadGateway
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Failing.Package");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned 502 while looking up package");
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{ "versions": {} }""")]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableForInvalidVersionsPayload(string versionsJson)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(versionsJson),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "invalid versions response");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenNoComparableVersionExists()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": [null, "not-a-version", "1..0"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.NotFound, 404, "Package 'Package.With.Case' was not found on nuget.org");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsNotFoundWhenNuspecIsMissingForResolvedVersion()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.0/package.with.case.nuspec" =>
                    new HttpResponseMessage(HttpStatusCode.NotFound),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.NotFound, 404, "version '1.0.0' was not found on nuget.org");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenNuspecRequestFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.0/package.with.case.nuspec" =>
                    new HttpResponseMessage(HttpStatusCode.BadGateway),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned 502 while reading package");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenParsedMetadataIsInvalid()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.0/package.with.case.nuspec" =>
                    XmlResponse(
                        """
                        <package>
                          <metadata />
                        </package>
                        """
                    ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned invalid metadata");
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
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageMetadata("   "));
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

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenTransportThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Name or service not known."));
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Any.Package");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "could not be reached");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenVersionsJsonIsMalformed()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse("{"),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned invalid JSON");
    }

    [Fact]
    public void GetNuGetPackageMetadataReturnsServiceUnavailableWhenReadingNuspecFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["1.0.0"]
                    }
                    """
                ),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/1.0.0/package.with.case.nuspec" =>
                    XmlResponse(
                        """
                        <package>
                          <metadata>
                            <id>Package.With.Case</id>
                            <version>1.0.0</version>
                          </metadata>
                        </package>
                        """
                    ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(
            new HttpClient(handler),
            new ThrowingParser(new IOException("Stream read failed."))
        );

        var result = client.GetNuGetPackageMetadata("Package.With.Case");

        AssertProblem(result, ProblemTypes.ServiceUnavailable, 503, "network I/O error occurred");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsStableRemoteVersionsWithoutFetchingNuspecs()
    {
        var seenUris = new List<string>();
        var handler = new StubHttpMessageHandler(request =>
        {
            seenUris.Add(request.RequestUri!.AbsoluteUri);

            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["12.0.1", "13.0.3-beta.1", "13.0.3", "not-a-version"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Newtonsoft.Json", minimumMajor: 13);

        Assert.Null(result.Problem);
        Assert.Equal(["13.0.3"], result.Versions);
        Assert.DoesNotContain(seenUris, uri => uri.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetNuGetPackageVersionsIncludesPrereleaseRemoteVersionsWhenRequested()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["13.0.3-beta.1", "13.0.3"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Newtonsoft.Json", minimumMajor: 13, includePreRelease: true);

        Assert.Null(result.Problem);
        Assert.Equal(["13.0.3", "13.0.3-beta.1"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsLimitsRemoteVersionsAfterSorting()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/newtonsoft.json/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["11.0.1", "12.0.1", "13.0.3"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Newtonsoft.Json", maxItems: 2);

        Assert.Null(result.Problem);
        Assert.Equal(["13.0.3", "12.0.1"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenServiceIndexFails()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Any.Package");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "could not be reached");
    }

    [Theory]
    [InlineData("""{ "resources": [] }""")]
    [InlineData("""{ "resources": {} }""")]
    [InlineData("""{ "resources": [{ "@type": "PackageBaseAddress/3.0.0", "@id": "" }] }""")]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenPackageBaseAddressIsUnavailable(
        string serviceIndexJson
    )
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.AbsoluteUri == "https://api.nuget.org/v3/index.json"
                ? JsonResponse(serviceIndexJson)
                : throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}")
        );

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Any.Package");

        AssertVersionsProblem(
            result,
            ProblemTypes.ServiceUnavailable,
            503,
            "did not advertise a package content endpoint"
        );
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenVersionsRequestFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/failing.package/index.json" => new HttpResponseMessage(
                    HttpStatusCode.BadGateway
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Failing.Package");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned 502");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsNotFoundWhenNoComparableVersionExists()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": [null, "not-a-version", "1..0", "2.0.0-beta"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        AssertVersionsProblem(result, ProblemTypes.NotFound, 404, "versions were not found on nuget.org");
    }

    [Fact]
    public void GetNuGetPackageVersionsDeduplicatesCaseInsensitiveVersions()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(
                    """
                    {
                      "versions": ["2.0.0", "2.0.0", "1.0.0"]
                    }
                    """
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        Assert.Null(result.Problem);
        Assert.Equal(["2.0.0", "1.0.0"], result.Versions);
    }

    [Fact]
    public void GetNuGetPackageVersionsRejectsInvalidPackageIdentifiers()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new Xunit.Sdk.XunitException("Request should not be sent.")
        );
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageVersions("https://example.com/package"));
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageVersions("Newtonsoft/Json"));
        Assert.Throws<ArgumentException>(() => client.GetNuGetPackageVersions("   "));
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenReadingVersionsFails()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => new ThrowingContentResponse(
                    new IOException("Stream read failed.")
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "could not be reached");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenVersionsSendThrowsIOException()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => throw new IOException(
                    "Socket read failed."
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "network I/O error occurred");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetNuGetPackageVersionsRejectsNonPositiveMaxItems(int maxItems)
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new Xunit.Sdk.XunitException("Request should not be sent.")
        );
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.GetNuGetPackageVersions("Any.Package", maxItems: maxItems)
        );
    }

    [Fact]
    public void GetNuGetPackageVersionsRejectsNegativeMinimumMajor()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new Xunit.Sdk.XunitException("Request should not be sent.")
        );
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            client.GetNuGetPackageVersions("Any.Package", minimumMajor: -1)
        );
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsNotFoundWhenPackageIsMissing()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/missing.package/index.json" => new HttpResponseMessage(
                    HttpStatusCode.NotFound
                ),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Missing.Package");

        AssertVersionsProblem(result, ProblemTypes.NotFound, 404, "was not found on nuget.org");
    }

    [Theory]
    [InlineData("""{}""")]
    [InlineData("""{ "versions": {} }""")]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableForInvalidVersionsPayload(string versionsJson)
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse(versionsJson),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "invalid versions response");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenVersionsJsonIsMalformed()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.RequestUri!.AbsoluteUri switch
            {
                "https://api.nuget.org/v3/index.json" => JsonResponse(ServiceIndexJson),
                "https://api.nuget.org/v3-flatcontainer/package.with.case/index.json" => JsonResponse("{"),
                _ => throw new Xunit.Sdk.XunitException($"Unexpected URI: {request.RequestUri}"),
            };
        });

        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Package.With.Case");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "returned invalid JSON");
    }

    [Fact]
    public void GetNuGetPackageVersionsReturnsServiceUnavailableWhenTransportThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Name or service not known."));
        var client = new NuGetOrgPackageMetadataClient(new HttpClient(handler), new NuGetPackageMetadataParser());

        var result = client.GetNuGetPackageVersions("Any.Package");

        AssertVersionsProblem(result, ProblemTypes.ServiceUnavailable, 503, "could not be reached");
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

    private static void AssertVersionsProblem(
        NuGetPackageVersionsLookup result,
        string expectedType,
        int expectedStatus,
        string expectedDetail
    )
    {
        Assert.Null(result.Versions);
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

    private sealed class ThrowingParser(Exception exception) : INuGetPackageMetadataParser
    {
        public NuGetPackageMetadata? Parse(Stream stream) => throw exception;
    }

    private sealed class ThrowingContentResponse : HttpResponseMessage
    {
        public ThrowingContentResponse(IOException exception)
            : base(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ThrowingReadStream(exception));
        }
    }

    private sealed class ThrowingReadStream(IOException exception) : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw exception;

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
