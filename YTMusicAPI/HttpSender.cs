namespace YTMusicAPI;

internal class HttpSender
{
    private readonly HttpClient _http;

    public HttpSender()
    {
        _http = new HttpClient();
    }

    public HttpSender(HttpClient http)
    {
        _http = http;
    }

    internal async ValueTask<string> SendHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        // User-agent
        if (!request.Headers.Contains("User-Agent"))
        {
            request.Headers.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36"
            );
        }

        // Set required cookies
        request.Headers.Add("Cookie", "CONSENT=YES+cb; YSC=DwKYllHNwuw");

        using var response = await _http.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );
        
        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(errorResponse);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}