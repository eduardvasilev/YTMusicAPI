using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;
using YTMusicAPI.Utils;

namespace YTMusicAPI;

public class ReleasesClient : IReleasesClient
{
    private const string ApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

    public async Task<List<Album>> GetReleasesAsync(CancellationToken cancellationToken)
    {
        const string url = $"https://music.youtube.com/youtubei/v1/browse?key={ApiKey}&prettyPrint=false";

        var payload = new
        {
            browseId = "FEmusic_new_releases_albums",
            context = new
            {
                client = new
                {
                    clientName = "WEB_REMIX",
                    clientVersion = "1.20230130.01.00",
                    hl = "en",
                    gl = "US",
                    utcOffsetMinutes = 0,
                    acceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9",
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            )
        };

        var rawResult = await (new HttpSender()).SendHttpRequestAsync(request, cancellationToken);

        using var doc = JsonDocument.Parse(rawResult);
        JsonElement rootElement = doc.RootElement.Clone();

        var grid = JObject.Parse(rootElement.ToString()).FindTokens("gridRenderer").First();

        List<JToken> albums = grid.FindTokens("musicTwoRowItemRenderer");

        var result = new List<Album>();
        foreach (var album in albums)
        {
            string id;
            try
            {
                id =
                    album.FindTokens("menu").First()?
                        .FindTokens("items")
                        .First()
                        .FindTokens("playlistId")
                        .FirstOrDefault()?.Value<string>();
            }
            catch
            {
                continue;
            }

            var title =
                album.FindTokens("title").First()?
                    .FindTokens("runs").First()
                    .FindTokens("text").FirstOrDefault()?.Value<string>();

            string artistName;
            try
            {
                artistName = (album.FindTokens("subtitle").First()
                                 ?.FindTokens("runs").Values())
                             .FirstOrDefault(x => x.Children().Values().Count() > 1)?.FindTokens("text").FirstOrDefault()
                             .Value<string>()
                             ??
                             album.FindTokens("subtitle").First()?.FindTokens("runs").Values()
                                 .LastOrDefault()?.FindTokens("text").FirstOrDefault()
                                 .Value<string>();
            }
            catch
            {
                continue;
            }

            string artistId = ((album.FindTokens("subtitle").First()
                                      ?.FindTokens("runs").Values())
                                  .FirstOrDefault(x => x.Children().Values().Count() > 1)?.FindTokens("navigationEndpoint")
                                  .FirstOrDefault()?
                                  .FindTokens("browseEndpoint")?.FirstOrDefault()?
                                  .FindTokens("browseId"))?.FirstOrDefault()?.Value<string>() ??
                              album.FindTokens("subtitle").First()?.FindTokens("runs").Values()
                                  .LastOrDefault()?.FindTokens("browseEndpoint")?.FirstOrDefault()?
                                  .FindTokens("browseId")?.FirstOrDefault()?.Value<string>(); ;

            var thumbnails = new List<Thumbnail>();

            foreach (var thumbnailExtractor in album.FindTokens("thumbnailRenderer"))
            {
                JToken thumbnailToken = thumbnailExtractor.FindTokens("thumbnails").LastOrDefault();
                var thumbnailUrl = thumbnailToken.Last().FindTokens("url").Values().Last().Value<string>();

                var thumbnailWidth = thumbnailToken.Last().FindTokens("width").Values().Last().Value<int>();

                var thumbnailHeight = thumbnailToken.Last().FindTokens("height").Values().Last().Value<int>();

                var thumbnailResolution = new Resolution(thumbnailWidth, thumbnailHeight);
                var thumbnail = new Thumbnail(thumbnailUrl, thumbnailResolution);
                thumbnails.Add(thumbnail);
            }

            var single = (album.FindTokens("subtitle").First()?.FindTokens("runs")
                    .Values()).First(x => x.Children().Values().Count() == 1)
                .FindTokens("text").FirstOrDefault()
                .Value<string>();

            //var year = album.TryGetYearDetails();
            var isSingle = string.Equals("single", single, StringComparison.OrdinalIgnoreCase);

            var playlist = new Album
            {
                Id = id,
                Title = title,
                Author = artistName,
                AuthorChannelId = artistId,
                Thumbnails = thumbnails,
                RecordType = isSingle ? "Single" : "Album" 
                
                
            };
            result.Add(playlist);
        }
        return result;
    }
}