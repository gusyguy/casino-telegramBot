using Newtonsoft.Json;

namespace Presentation.Common.Extensions;
public class HttpClientExtension
{
    static HttpClient _httpClient = new HttpClient();

    public static async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers = null)
    {
        if (headers != null) foreach (var header in headers)
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return JsonConvert.DeserializeObject<T>(
            await response.Content.ReadAsStringAsync());
    }
    public static async Task<T> PostAsync<T>(string url, Dictionary<string, string> data, Dictionary<string, string> headers = null)
    {
        if (headers != null) foreach (var header in headers)
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);

        var content = new FormUrlEncodedContent(data);
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        return JsonConvert.DeserializeObject<T>(
            await response.Content.ReadAsStringAsync());
    }

}
