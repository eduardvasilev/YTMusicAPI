using System.Web;

namespace YTMusicAPI.Utils;

public static class UrlHelper
{
    public static Uri AddQueryParameter(this Uri url, string key, string value)
    {
        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query[key] = value;
        uriBuilder.Query = query.ToString() ?? string.Empty;
        return uriBuilder.Uri;
    }
}