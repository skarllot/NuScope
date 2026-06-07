using System.Text.RegularExpressions;
using Raiqub.NuSpec.Features.Common.Models;
using Raiqub.NuSpec.Features.GetNuGetUrls.Models;

namespace Raiqub.NuSpec.Features.GetNuGetUrls.Services;

public static class NuGetPackageUrlExtractor
{
    private static readonly Regex _urlRegex = new(
        @"https?://[^\s<>()\[\]{}""']*[^\s<>()\[\]{}""'.,;:!?]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    public static NuGetPackageUrlsResult Extract(NuGetPackageMetadata metadata)
    {
        var projectUrl = NormalizeUrl(metadata.ProjectUrl);
        var repositoryUrl = NormalizeUrl(metadata.Repository?.Url);
        var otherUrls = GetOtherUrls(metadata, projectUrl, repositoryUrl);

        return NuGetPackageUrlsResult.Found(projectUrl, repositoryUrl, otherUrls);
    }

    private static NuGetPackageMetadataUrl[] GetOtherUrls(
        NuGetPackageMetadata metadata,
        string? projectUrl,
        string? repositoryUrl
    )
    {
        var excludedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddExcludedUrl(projectUrl, excludedUrls);
        AddExcludedUrl(repositoryUrl, excludedUrls);

        var urls = new List<NuGetPackageMetadataUrl>();
        AddUrls(urls, excludedUrls, "iconUrl", metadata.IconUrl);
        AddUrls(urls, excludedUrls, "licenseUrl", metadata.LicenseUrl);
        AddUrls(urls, excludedUrls, "description", metadata.Description);
        AddUrls(urls, excludedUrls, "summary", metadata.Summary);
        AddUrls(urls, excludedUrls, "releaseNotes", metadata.ReleaseNotes);
        AddUrls(urls, excludedUrls, "copyright", metadata.Copyright);

        return urls.ToArray();
    }

    private static void AddUrls(
        List<NuGetPackageMetadataUrl> urls,
        HashSet<string> excludedUrls,
        string source,
        string? value
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (NormalizeUrl(value) is { } directUrl)
        {
            AddUrl(urls, excludedUrls, source, directUrl);
            return;
        }

        foreach (var url in _urlRegex.Matches(value).Select(match => match.Value.Trim()))
        {
            AddUrl(urls, excludedUrls, source, url);
        }
    }

    private static void AddUrl(
        List<NuGetPackageMetadataUrl> urls,
        HashSet<string> excludedUrls,
        string source,
        string url
    )
    {
        if (!excludedUrls.Add(url))
        {
            return;
        }

        urls.Add(new NuGetPackageMetadataUrl { Source = source, Url = url });
    }

    private static void AddExcludedUrl(string? url, HashSet<string> excludedUrls)
    {
        if (url is not null)
        {
            excludedUrls.Add(url);
        }
    }

    private static string? NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var url = value.Trim();
        return
            Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? url
            : null;
    }
}
