using System.Net.Http;

namespace KabeCale.App.Services;

/// <summary>
/// アプリ全体で1つのHttpClientを共有する。祝日API・更新チェックAPIがそれぞれ個別に
/// HttpClientを持つと接続プール分のリソース・メモリが余分にかかるため統合している。
/// </summary>
internal static class SharedHttpClient
{
    public static readonly HttpClient Instance = new() { Timeout = TimeSpan.FromSeconds(10) };
}
