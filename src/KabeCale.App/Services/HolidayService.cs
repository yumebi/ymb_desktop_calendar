using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace KabeCale.App.Services;

public class HolidayService
{
    private const string ApiUrlTemplate = "https://holidays-jp.github.io/api/v1/{0}/date.json";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(7);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly Dictionary<int, Dictionary<string, string>> _memoryCache = new();

    private class CacheEnvelope
    {
        public DateTime FetchedAtUtc { get; set; }
        public Dictionary<string, string> Data { get; set; } = new();
    }

    /// <summary>年ごとの祝日データ(日付文字列 yyyy-MM-dd -> 祝日名)を返す。取得失敗時はローカルキャッシュにフォールバックする。</summary>
    public async Task<Dictionary<string, string>> GetHolidaysAsync(int year, CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(year, out var cached))
            return cached;

        var envelope = ReadCache(year);
        var needsRefresh = envelope is null || DateTime.UtcNow - envelope.FetchedAtUtc > CacheLifetime;

        if (needsRefresh)
        {
            var fetched = await TryFetchFromNetworkAsync(year, ct);
            if (fetched is not null)
            {
                envelope = new CacheEnvelope { FetchedAtUtc = DateTime.UtcNow, Data = fetched };
                WriteCache(year, envelope);
            }
        }

        var result = envelope?.Data ?? new Dictionary<string, string>();
        _memoryCache[year] = result;
        return result;
    }

    private async Task<Dictionary<string, string>?> TryFetchFromNetworkAsync(int year, CancellationToken ct)
    {
        try
        {
            var url = string.Format(ApiUrlTemplate, year);
            var json = await SharedHttpClient.Instance.GetStringAsync(url, ct);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static CacheEnvelope? ReadCache(int year)
    {
        var path = AppPaths.HolidayCacheFile(year);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<CacheEnvelope>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteCache(int year, CacheEnvelope envelope)
    {
        AppPaths.EnsureRootDir();
        var json = JsonSerializer.Serialize(envelope, JsonOptions);
        File.WriteAllText(AppPaths.HolidayCacheFile(year), json);
    }
}
