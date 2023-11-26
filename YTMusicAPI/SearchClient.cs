using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Model;
using YTMusicAPI.Utils;

namespace YTMusicAPI
{
    public class SearchClient : ISearchClient
    {
        public async Task<SearchingResult<Artist>> GetArtistsChannelsAsync(QueryRequest queryRequest,
            CancellationToken cancellationToken)
        {
            const string url =
                $"https://music.youtube.com/youtubei/v1/search?key=AIzaSyC9XL3ZjWddXya6X74dJoCTL-WEYFDNX30";
            var continuationData = queryRequest.ContinuationData?.ContinuationToken;
            string inputToken = queryRequest.ContinuationData?.Token;
            var payload = GetPayload(queryRequest.Query, EntityType.Artist);
            Uri uri = new Uri(url);
            uri = queryRequest.ContinuationData?.ContinuationToken != null
                ? uri.AddQueryParameter("ctoken", queryRequest.ContinuationData.ContinuationToken)
                : uri;

            uri = continuationData != null ? uri.AddQueryParameter("continuation", continuationData) : uri;
            uri = inputToken != null ? uri.AddQueryParameter("itct", inputToken) : uri;
            uri = uri.AddQueryParameter("type", "next");


            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )

            };
            request.Headers.Add("Referer", "music.youtube.com");

            var rawResult = await (new HttpSender(new HttpClient())).SendHttpRequestAsync(request, cancellationToken);
            JObject jsonResult = JObject.Parse(rawResult);
            JToken firstOrDefault = jsonResult.FindTokens("continuationContents").FirstOrDefault();

            JToken token = jsonResult.FindTokens("clickTrackingParams").FirstOrDefault()!;
            JToken continuation = jsonResult.FindTokens("continuations").FirstOrDefault()?.FindTokens("continuation")
                .FirstOrDefault();

            using var doc = JsonDocument.Parse(firstOrDefault != null ? firstOrDefault.ToString() : rawResult);
            JsonElement jsonElement = doc.RootElement.Clone();

            JsonElement? root;
            JsonElement[] searchResults;
            if (queryRequest.ContinuationNeed == true)
            {
                root = jsonElement.GetProperty("musicShelfContinuation").GetPropertyOrNull("contents") ??
                       jsonElement.GetProperty("musicShelfContinuation")
                           .GetPropertyOrNull("onResponseReceivedCommands");

                searchResults = root?.EnumerateArray()
                    //.SelectMany(x => x)
                    .Select(x => x.GetProperty("musicResponsiveListItemRenderer"))
                    .ToArray();
            }
            else
            {
                root = jsonElement.GetPropertyOrNull("contents") ??
                       jsonElement.GetPropertyOrNull("onResponseReceivedCommands");

                searchResults = root?.EnumerateDescendantProperties("musicShelfRenderer")
                    .Select(x => x.GetProperty("contents"))
                    .Select(x => x.EnumerateArray())
                    .SelectMany(x => x)
                    .Select(x => x.GetProperty("musicResponsiveListItemRenderer"))
                    .ToArray();
            }
             
            if (searchResults != null)
            {
                List<Artist> artists = new List<Artist>();

                foreach (var searchResult in searchResults)
                {
                    JObject artistJson = JObject.Parse(searchResult.ToString());
                    var id = artistJson.FindTokens("browseId").FirstOrDefault().Value<string>();

                    var title = GetArtistTitle(searchResult);

                    var thumbnails = new List<Thumbnail>();


                    foreach (var thumbnailExtractor in artistJson.FindTokens("musicThumbnailRenderer"))
                    {
                        JToken thumnail = thumbnailExtractor.FindTokens("thumbnails").LastOrDefault();
                        var thumbnailUrl = thumnail.Last().FindTokens("url").Values().Last().Value<string>();

                        var thumbnailWidth = thumnail.Last().FindTokens("width").Values().Last().Value<int>();

                        var thumbnailHeight = thumnail.Last().FindTokens("height").Values().Last().Value<int>();

                        var thumbnailResolution = new Resolution(thumbnailWidth, thumbnailHeight);
                        var thumbnail = new Thumbnail(thumbnailUrl, thumbnailResolution);
                        thumbnails.Add(thumbnail);
                    }

                    var artist = new Artist
                    {
                        Id = id,
                        Title = title,
                        Thumbnails = thumbnails
                    };
                    artists.Add(artist);
                }

                return new SearchingResult<Artist>(artists, continuation.Value<string>(), token.Value<string>());
            }

            return new SearchingResult<Artist>(new List<Artist>(), null, null);
        }

        private string GetArtistTitle(JsonElement content)
        {
            try
            {
                var arrayEnumerator = content
                    .GetPropertyOrNull("flexColumns")
                    ?.EnumerateArray();

                if (arrayEnumerator.HasValue)
                {
                    IEnumerable<JsonElement> jsonElements = arrayEnumerator;
                    //displayPriority: 
                    foreach (var jsonElement in jsonElements)
                    {
                        JsonElement? displayPriority = jsonElement
                            .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")
                            ?.GetPropertyOrNull("displayPriority");
                        if (displayPriority.HasValue && displayPriority.ToString() ==
                            "MUSIC_RESPONSIVE_LIST_ITEM_COLUMN_DISPLAY_PRIORITY_HIGH")
                        {
                            string artist = jsonElement
                                .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                                .GetPropertyOrNull("text")?
                                .GetPropertyOrNull("runs")
                                ?.EnumerateArray()
                                .FirstOrDefault()
                                .GetPropertyOrNull("text").ToString();

                            if (!string.IsNullOrWhiteSpace(artist))
                            {
                                return artist;
                            }
                        }
                    }
                }

                return "Unknown artist";
            }
            catch
            {
                return "Unknown artist";
            }
        }

        private static object GetPayload(string query, EntityType entityType)
        {
            var payload = new
            {
                query,
                @params = ResolveEntityParameter(entityType),
                @filter_params = new[] { "Y" },

                context = new
                {
                    client = new
                    {
                        clientName = "WEB_REMIX",
                        clientVersion = "1.20220817.01.00",
                        hl = "en",
                        gl = "US",
                        utcOffsetMinutes = 0,
                        timeZone = "America/New_York"
                    }
                }
            };
            return payload;
        }

        private static string ResolveEntityParameter(EntityType searchFilter)
        {
            switch (searchFilter)
            {
                case EntityType.None:
                    return string.Empty;
                case EntityType.Track:
                    return "EgWKAQIIAWoKEAMQBBAJEAoQBQ%3D%3D";
                case EntityType.Album:
                    return "EgWKAQIYAWoKEAMQBBAJEAoQBQ%3D%3D";
                case EntityType.Artist:
                    return "EgWKAQIgAWoKEAMQBBAJEAoQBQ%3D%3D";
                default:
                    return null;
            }
        }
    }
}