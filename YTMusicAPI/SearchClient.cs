using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;
using YTMusicAPI.Utils;

namespace YTMusicAPI
{
    public class SearchClient : ISearchClient
    {
        public async Task<SearchingResult<Artist>> SearchArtistsChannelsAsync(QueryRequest queryRequest,
            CancellationToken cancellationToken)
        {
            var rawResult = await SendRequest(queryRequest, EntityType.Artist, cancellationToken);

            var jsonElement = ParseResponse(rawResult, out var token, out var continuation);

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
                root = GetRoot(jsonElement);

                searchResults = root?.EnumerateDescendantProperties("musicShelfRenderer")
                    .Select(x => x.GetProperty("contents"))
                    .Select(x => x.EnumerateArray())
                    .SelectMany(x => x)
                    .Select(x => x.GetProperty("musicResponsiveListItemRenderer"))
                    .ToArray();
            }

            List<Artist> artists = new List<Artist>();

            if (searchResults != null)
            {

                foreach (var searchResult in searchResults)
                {
                    JObject artistJson = JObject.Parse(searchResult.ToString());
                    var id = artistJson.FindTokens("browseId").FirstOrDefault()?.Value<string>();

                    var title = GetArtistTitle(searchResult);

                    var thumbnails = new List<Thumbnail>();

                    foreach (var thumbnailExtractor in artistJson.FindTokens("thumbnails").AsJEnumerable().Children())
                    {
                        var thumbnailUrl = thumbnailExtractor.FindTokens("url").Values().Last().Value<string>();

                        var thumbnailWidth = thumbnailExtractor.FindTokens("width").Values().Last().Value<int>();

                        var thumbnailHeight = thumbnailExtractor.FindTokens("height").Values().Last().Value<int>();

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

            }

            return new SearchingResult<Artist>(artists, continuation.Value<string>(), token.Value<string>());
        }


        public async Task<SearchingResult<Album>> SearchAlbumsAsync(QueryRequest queryRequest, CancellationToken cancellationToken)
        {
            var rawResult = await SendRequest(queryRequest, EntityType.Album, cancellationToken);

            var jsonElement = ParseResponse(rawResult, out var token, out var continuation);

            JsonElement[] searchResults;
            if (queryRequest.ContinuationNeed == true)
            {
                searchResults = jsonElement
                    .GetPropertyOrNull("musicShelfContinuation")
                    ?.EnumerateDescendantProperties("contents")
                    .Select(x => x.EnumerateArray())
                    .SelectMany(x => x)
                    .Select(x => x.GetProperty("musicResponsiveListItemRenderer"))
                    .ToArray();
            }
            else
            {
                searchResults = GetRoot(jsonElement)
                    ?.EnumerateDescendantProperties("musicShelfRenderer")
                    .Select(x => x.GetProperty("contents"))
                    .Select(x => x.EnumerateArray())
                    .SelectMany(x => x)
                    .Select(x => x.GetProperty("musicResponsiveListItemRenderer"))
                    .ToArray();
            }
            List<Album> albums = new List<Album>();

            if (searchResults != null)
            {
                foreach (var searchResult in 
                         searchResults)
                {
                    JObject albumJson = JObject.Parse(searchResult.ToString());

                    string id;
                    try
                    {
                        id = searchResult.GetPropertyOrNull("overlay")?
                            .GetPropertyOrNull("musicItemThumbnailOverlayRenderer")?
                            .GetPropertyOrNull("content")?
                            .GetPropertyOrNull("musicPlayButtonRenderer")?
                            .GetPropertyOrNull("playNavigationEndpoint")?
                            .GetPropertyOrNull("watchPlaylistEndpoint")?
                            .GetPropertyOrNull("playlistId")?
                            .GetString() ?? string.Empty;
                    }
                    catch
                    {
                        continue;
                    }

                    var title = searchResult.GetPropertyOrNull("overlay")?
                        .GetPropertyOrNull("musicItemThumbnailOverlayRenderer")?
                        .GetPropertyOrNull("content")?
                        .GetPropertyOrNull("musicPlayButtonRenderer")?
                        .GetPropertyOrNull("accessibilityPlayData")?
                        .GetPropertyOrNull("accessibilityData")?
                        .GetPropertyOrNull("label")?
                        .GetString()?.Substring(5);

                    var artistInfo = GetAlbumAuthorInfo(searchResult);


                    var thumbnails = new List<Thumbnail>();

                    foreach (var thumbnailExtractor in albumJson.FindTokens("thumbnails").AsJEnumerable().Children())
                    {
                        var thumbnailUrl = thumbnailExtractor.FindTokens("url").Values().Last().Value<string>();

                        var thumbnailWidth = thumbnailExtractor.FindTokens("width").Values().Last().Value<int>();

                        var thumbnailHeight = thumbnailExtractor.FindTokens("height").Values().Last().Value<int>();

                        var thumbnailResolution = new Resolution(thumbnailWidth, thumbnailHeight);
                        var thumbnail = new Thumbnail(thumbnailUrl, thumbnailResolution);
                        thumbnails.Add(thumbnail);
                    }

                    var year = searchResult
                        .GetPropertyOrNull("flexColumns")?
                        .EnumerateArrayOrNull()?
                        .ElementAtOrDefault(1)
                        .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                        .GetPropertyOrNull("text")?
                        .GetPropertyOrNull("runs")?
                        .EnumerateArrayOrNull()?
                        .LastOrDefault()
                        .GetPropertyOrNull("text")
                        ?.GetString();

                    var recordType = searchResult
                        .GetPropertyOrNull("flexColumns")?
                        .EnumerateArrayOrNull()?
                        .ElementAtOrDefault(1)
                        .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                        .GetPropertyOrNull("text")?
                        .GetPropertyOrNull("runs")?
                        .EnumerateArrayOrNull()?
                        .FirstOrDefault()
                        .GetPropertyOrNull("text")
                        ?.GetString();

                    var album = new Album
                    {
                        Title = title,
                        Author = artistInfo.artist,
                        AuthorChannelId = artistInfo.browseid,
                        Id = id,
                        RecordType = recordType,
                        Year = year,
                        Thumbnails = thumbnails
                    };
                    albums.Add(album);
                }
            }

            return new SearchingResult<Album>(albums, continuation?.Value<string>(), token?.Value<string>());
        }

        public async Task<SearchingResult<Track>> SearchTracksAsync(QueryRequest queryRequest, CancellationToken cancellationToken)
        {
            var rawResult = await SendRequest(queryRequest, EntityType.Track, cancellationToken);

            var jsonElement = ParseResponse(rawResult, out var token, out var continuation);

            List<Track> tracks = new List<Track>();

            JsonElement[] searchResults;
            if (queryRequest.ContinuationNeed == true)
            {
                searchResults = jsonElement
                    .GetPropertyOrNull("musicShelfContinuation")
                    ?.GetProperty("contents")
                    .EnumerateArrayOrEmpty()
                    .ToArray();
            }
            else
            {
                searchResults = jsonElement
                    .EnumerateDescendantProperties("contents")
                    .LastOrDefault()
                    .EnumerateArray()
                    .ToArray();
            }

            if (searchResults != null)
            {
                foreach (var searchResult in searchResults)
                {
                    JObject trackJson = JObject.Parse(searchResult.ToString());

                    string id;
                    try
                    {
                        id = searchResult
                            .GetPropertyOrNull("musicResponsiveListItemRenderer")?
                            .GetPropertyOrNull("overlay")?
                            .GetPropertyOrNull("musicItemThumbnailOverlayRenderer")?
                            .GetPropertyOrNull("content")?
                            .GetPropertyOrNull("musicPlayButtonRenderer")?
                            .GetPropertyOrNull("playNavigationEndpoint")?
                            .GetPropertyOrNull("watchEndpoint")?
                            .GetPropertyOrNull("videoId")?
                            .GetString();
                    }
                    catch
                    {
                        continue;
                    }

                    var infos = searchResult
                        .GetPropertyOrNull("musicResponsiveListItemRenderer")?
                        .GetPropertyOrNull("flexColumns")
                        ?.EnumerateArrayOrEmpty();

                    var title = infos?.ElementAtOrDefault(0)
                        .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                        .GetPropertyOrNull("text")?
                        .GetPropertyOrNull("runs")?
                        .EnumerateArrayOrEmpty()
                        .ElementAtOrDefault(0)
                        .GetPropertyOrNull("text")?
                        .GetString();

                    JsonElement? artistInfo = infos?.ElementAtOrDefault(1)
                        .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                        .GetPropertyOrNull("text")?
                        .GetPropertyOrNull("runs")?
                        .EnumerateArrayOrEmpty()
                        .ElementAtOrDefault(0);
                    var author = artistInfo
                        ?.GetPropertyOrNull("text")?
                        .GetString();

                    var channelId = artistInfo?.GetPropertyOrNull("navigationEndpoint")?
                        .GetPropertyOrNull("browseEndpoint")?
                        .GetPropertyOrNull("browseId")?
                        .GetString();

                    var thumbnails = new List<Thumbnail>();

                    foreach (var thumbnailExtractor in trackJson.FindTokens("thumbnails").AsJEnumerable().Children())
                    {
                        var thumbnailUrl = thumbnailExtractor.FindTokens("url").Values().Last().Value<string>();

                        var thumbnailWidth = thumbnailExtractor.FindTokens("width").Values().Last().Value<int>();

                        var thumbnailHeight = thumbnailExtractor.FindTokens("height").Values().Last().Value<int>();

                        var thumbnailResolution = new Resolution(thumbnailWidth, thumbnailHeight);
                        var thumbnail = new Thumbnail(thumbnailUrl, thumbnailResolution);
                        thumbnails.Add(thumbnail);
                    }

                    var track = new Track
                    {
                        Id = id,
                        Title = title,
                        Thumbnails = thumbnails,
                        Author = author,
                        AuthorChannelId = channelId
                    };

                    tracks.Add(track);
                }
            }

            return new SearchingResult<Track>(tracks, continuation?.Value<string>(), token?.Value<string>());


        }

        private static JsonElement? GetRoot(JsonElement jsonElement)
        {
            return jsonElement.GetPropertyOrNull("contents") ??
                   jsonElement.GetPropertyOrNull("onResponseReceivedCommands");
        }

        private static JsonElement ParseResponse(string rawResult, out JToken token, out JToken continuation)
        {
            JObject jsonResult = JObject.Parse(rawResult);
            JToken continuationContents = jsonResult.FindTokens("continuationContents").FirstOrDefault();

            token = jsonResult.FindTokens("clickTrackingParams").FirstOrDefault()!;
            continuation = jsonResult.FindTokens("continuations").FirstOrDefault()?.FindTokens("continuation")
                .FirstOrDefault();

            using var doc = JsonDocument.Parse(continuationContents != null ? continuationContents.ToString() : rawResult);
            JsonElement jsonElement = doc.RootElement.Clone();
            return jsonElement;
        }
        
        private static async Task<string> SendRequest(QueryRequest queryRequest, EntityType entityType, CancellationToken cancellationToken)
        {
            const string url =
                $"https://music.youtube.com/youtubei/v1/search?key=AIzaSyC9XL3ZjWddXya6X74dJoCTL-WEYFDNX30";
            var continuationData = queryRequest.ContinuationData?.ContinuationToken;
            string inputToken = queryRequest.ContinuationData?.Token;
            var payload = GetPayload(queryRequest.Query, entityType);
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
            return rawResult;
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

        public (string artist, string browseid) GetAlbumAuthorInfo(JsonElement album)
        {
            try
            {
                var arrayEnumerator = album
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
                            var artistInfo = jsonElement
                                .GetPropertyOrNull("musicResponsiveListItemFlexColumnRenderer")?
                                .GetPropertyOrNull("text")?
                                .GetPropertyOrNull("runs")
                                ?.EnumerateArray()
                                .FirstOrDefault(x => x.GetPropertyOrNull("navigationEndpoint").HasValue);

                            var browseId = artistInfo?.GetPropertyOrNull("navigationEndpoint")
                                ?.GetPropertyOrNull("browseEndpoint")
                                ?.GetPropertyOrNull("browseId")
                                .ToString();

                            string artist = artistInfo
                                ?.GetPropertyOrNull("text").ToString();

                            if (!string.IsNullOrWhiteSpace(artist))
                            {
                                return  (artist, browseId);
                            }
                        }
                    }
                }

                return ("Unknown artist", null);
            }
            catch
            {
                return ("Unknown artist", null);
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