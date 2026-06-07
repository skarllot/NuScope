using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.GetNuGetUrls.Services;
using Xunit;

namespace Raiqub.NuSpec.Tests.Features.GetNuGetUrls.Services;

public sealed class NuGetPackageUrlExtractorTests
{
    [Fact]
    public void ExtractTrimsDirectUrlsAndDeduplicatesExcludedUrls()
    {
        var metadata = new NuGetPackageMetadata
        {
            Id = "Package.With.Urls",
            Version = "1.0.0",
            ProjectUrl = " https://example.com/project ",
            Repository = new NuGetRepositoryMetadata { Url = " https://github.com/example/package " },
            IconUrl = " https://example.com/project ",
            LicenseUrl = " https://example.com/license ",
            Summary = "Repository mirror: https://github.com/example/package.",
        };

        var result = NuGetPackageUrlExtractor.Extract(metadata);

        Assert.Equal("https://example.com/project", result.ProjectUrl);
        Assert.Equal("https://github.com/example/package", result.RepositoryUrl);

        var otherUrl = Assert.Single(result.OtherUrls);
        Assert.Equal("licenseUrl", otherUrl.Source);
        Assert.Equal("https://example.com/license", otherUrl.Url);
    }

    [Fact]
    public void ExtractIgnoresBlankAndInvalidDirectUrlValues()
    {
        var metadata = new NuGetPackageMetadata
        {
            Id = "Package.With.Blank.Urls",
            Version = "1.0.0",
            ProjectUrl = "   ",
            IconUrl = "not a url",
            LicenseUrl = "ftp://example.com/license",
        };

        var result = NuGetPackageUrlExtractor.Extract(metadata);

        Assert.Null(result.ProjectUrl);
        Assert.Null(result.RepositoryUrl);
        Assert.Empty(result.OtherUrls);
    }

    [Fact]
    public void ExtractFindsDistinctUrlsInTextFields()
    {
        var metadata = new NuGetPackageMetadata
        {
            Id = "Package.With.Urls.In.Text",
            Version = "1.0.0",
            Description = "Docs: https://example.com/docs, support: http://example.com/support!",
            ReleaseNotes = "Also see https://example.com/docs.",
            Copyright = "Copyright https://example.com/legal)",
        };

        var result = NuGetPackageUrlExtractor.Extract(metadata);

        Assert.Collection(
            result.OtherUrls,
            url =>
            {
                Assert.Equal("description", url.Source);
                Assert.Equal("https://example.com/docs", url.Url);
            },
            url =>
            {
                Assert.Equal("description", url.Source);
                Assert.Equal("http://example.com/support", url.Url);
            },
            url =>
            {
                Assert.Equal("copyright", url.Source);
                Assert.Equal("https://example.com/legal", url.Url);
            }
        );
    }
}
