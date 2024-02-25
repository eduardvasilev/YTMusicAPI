using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Utils;

namespace YTMusicAPI;

public class ArtistClient : IArtistClient
{
    public async Task<string> GetArtistImageAsync(string artistUrl, CancellationToken cancellationToken)
    {
        string artistId = ArtistHelper.GetArtistId(artistUrl);

        var payload = new
        {
            browseId = artistId,
            context = new
            {
                client = new
                {
                    clientName = "WEB_REMIX",
                    clientVersion = "1.20230517.01.00",
                    androidSdkVersion = 30,
                    hl = "en",
                    gl = "US",
                    utcOffsetMinutes = 0
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, @"https://music.youtube.com/youtubei/v1/browse?key=AIzaSyC9XL3ZjWddXya6X74dJoCTL-WEYFDNX30&prettyPrint=false")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            )
        };

        var raw = await(new HttpSender()).SendHttpRequestAsync(request, cancellationToken);

        var url = JObject.Parse(raw).FindTokens("musicImmersiveHeaderRenderer").LastOrDefault()?
            .FindTokens("thumbnails").FirstOrDefault()?.FindTokens("url")
            //.Skip(1)
            .FirstOrDefault()?.Value<string>();

        return url;
    }
}