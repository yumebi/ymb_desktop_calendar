using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace KabeCale.App.Services;

public record UpdateCheckResult(bool HasUpdate, string? LatestVersion, string? ReleaseUrl);

public class UpdateService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/yumebi/ymb_desktop_calendar/releases/latest";

    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("YmbDesktopCalendar", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await SharedHttpClient.Instance.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(json);
            var tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var htmlUrl = doc.RootElement.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            if (tagName is null)
                return new UpdateCheckResult(false, null, null);

            var latestVersion = ParseVersion(tagName);
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

            var hasUpdate = latestVersion is not null && latestVersion > currentVersion;
            return new UpdateCheckResult(hasUpdate, tagName, htmlUrl);
        }
        catch
        {
            return new UpdateCheckResult(false, null, null);
        }
    }

    private static Version? ParseVersion(string tag)
    {
        var trimmed = tag.TrimStart('v', 'V');
        return Version.TryParse(trimmed, out var version) ? version : null;
    }
}
