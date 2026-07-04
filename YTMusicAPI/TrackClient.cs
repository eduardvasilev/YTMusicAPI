using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Model;
using YTMusicAPI.Model.Deciphers;
using YTMusicAPI.Model.Domain;
using YTMusicAPI.Utils;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace YTMusicAPI;

public class TrackClient : ITrackClient
{
    private const string ApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

    public async Task<Track> GetTrackInfoAsync(string trackUrl, CancellationToken cancellationToken)
    {
        var trackId = trackUrl;

        var value = TrackHelper.GetTrackId(trackId);
        if (!string.IsNullOrWhiteSpace(value))
            trackId = value;

        const string url = $"https://www.youtube-nocookie.com/youtubei/v1/player";

        var payload = new
        {
            videoId = trackId,
            context = new
            {
                client = new
                {
                    clientName = "ANDROID",
                    clientVersion = "20.10.38",
                    //deviceMake = "Apple",
                    //deviceModel = "iPhone16,2",
                    platform = "MOBILE",
                    osName = "Android",
                    osVersion = "11",
                    hl = "en",
                    timeZone = "UTC",
                    userAgent = "com.google.android.youtube/20.10.38 (Linux; U; ANDROID 11) gzip",
                    gl = "US",
                    utcOffsetMinutes = 0
                },
                request = new
                {
                    useSsl = true,
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

        request.Headers.Add(
            "User-Agent",
            "com.google.android.youtube/20.10.38 (Linux; U; ANDROID 11) gzip"
        );

        request.Headers.Add(
      "x-youtube-bootstrap-logged-in",
      "false"
  );

        request.Headers.Add(
"x-goog-visitor-id",
"CgtNRHRZVS15dEp3ayjc4Zy9BjIiCgJOTBIcEhgSFhMLFBUWFwwYGRobHB0eHw4PIBAREiEgZw%3D%3D"
);

        request.Headers.Add(
"x-youtube-client-name",
"56"
);

        request.Headers.Add(
"x-youtube-client-version",
"1.20250203.01.00"
);

        var rawResult = await (new HttpSender()).SendHttpRequestAsync(request, cancellationToken);

        using var doc = JsonDocument.Parse(rawResult);
        JsonElement rootElement = doc.RootElement.Clone();

        JsonElement? trackBlock = rootElement.GetPropertyOrNull("videoDetails");

        var track = BuildTrack(trackBlock);

        JsonElement? streamsBlock = rootElement.GetPropertyOrNull("streamingData")?.GetPropertyOrNull("adaptiveFormats");
        JsonElement? status = rootElement.GetPropertyOrNull("playabilityStatus")?.GetPropertyOrNull("status");

        if (streamsBlock != null)
        {
            var streamData = streamsBlock.Value.EnumerateArrayOrEmpty().ToArray();

            List<StreamData> streams = new List<StreamData>();
            foreach (var streamElement in streamData)
            {
                try
                {
                    StreamData stream = JsonConvert.DeserializeObject<StreamData>(streamElement.ToString());
                    streams.Add(stream);
                }
                catch
                {
                    // ignored
                }
            }

            track.Streams = streams;
        }
        else if (status?.ToString().Equals("ok", StringComparison.OrdinalIgnoreCase) != true) //age-restricted track
        {
            List<StreamData> streamDatas = await GetIFramePlayerAsync(trackId, cancellationToken);

            if (streamDatas.Any())
            {
                track.Streams = streamDatas;
            }
        }

        return track;
    }

    private static Track BuildTrack(JsonElement? trackBlock,
        Func<JsonElement?, string> titleResolver = null, Func<JsonElement? , (string, string)> artistResolver = null)
    {
        string id = trackBlock?.GetPropertyOrNull("videoId").ToString();
        string title = titleResolver != null ? titleResolver(trackBlock) : trackBlock?.GetPropertyOrNull("title").ToString();
       
        string channelId;
        string author;

        if (artistResolver == null)
        {
            channelId = trackBlock?.GetPropertyOrNull("channelId").ToString();
            author = trackBlock?.GetPropertyOrNull("author").ToString();

        }
        else
        {
            (string, string) artist = artistResolver(trackBlock);

            channelId = artist.Item1;
            author = artist.Item2;
        }

        var topic = " - Topic";
        if (author != null && author.EndsWith(topic))
        {
            author = author.Substring(0, author.Length - topic.Length);
        }

        JsonElement? durationField = trackBlock?.GetPropertyOrNull("lengthSeconds");
        TimeSpan? duration = null;
        if (durationField != null)
        {
            if(double.TryParse(durationField.ToString(), out double seconds))
            {
                duration = TimeSpan.FromSeconds(seconds);
            }
        }

        var thumbnails = new List<Thumbnail>();

        var trackBlocks = trackBlock.ToString();
        JObject trackJson = JObject.Parse(trackBlock.ToString());

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
            Author = author,
            AuthorChannelId = channelId,
            Id = id,
            Title = title,
            Thumbnails = thumbnails,
            Duration = duration
        };
        return track;
    }

    public async Task<AlbumTracksResult> GetAlbumTracksAsync(string albumUrl, CancellationToken cancellationToken)
    {
        const string url = $"https://music.youtube.com/youtubei/v1/browse?key={ApiKey}";

        var payload = new
        {
            browseId = "VL" + AlbumHelper.GetAlbumId(albumUrl),
            context = new
            {
                client = new
                {
                    clientName = "WEB",
                    clientVersion = "2.20210408.08.00",
                    hl = "en",
                    gl = "US",
                    utcOffsetMinutes = 0
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };
        string rawResult = await (new HttpSender()).SendHttpRequestAsync(request, cancellationToken);

        using var doc = JsonDocument.Parse(rawResult);
        JsonElement rootElement = doc.RootElement.Clone();

        List<Track> tracks = new List<Track>();
        List<Thumbnail> albumThumbnails = new List<Thumbnail>();
        string albumTitle = null;

        JObject trackJson = JObject.Parse(rootElement.ToString());

        var contentsToken = trackJson.FindTokens("contents")
            .FirstOrDefault(t => t.Type == JTokenType.Array || t.Type == JTokenType.Object);

        if (contentsToken != null)
        {
            var lockupViewModels = contentsToken.FindTokens("lockupViewModel").ToList();

            foreach (var lockupViewModel in lockupViewModels)
            {
                try
                {
                    var contentId = lockupViewModel.FindTokens("contentId").FirstOrDefault()?.Value<string>();

                    var title = lockupViewModel
                        .FindTokens("metadata")
                        .FirstOrDefault()?
                        .FindTokens("lockupMetadataViewModel")
                        .FirstOrDefault()?
                        .FindTokens("title")
                        .FirstOrDefault()?
                        .FindTokens("content")
                        .FirstOrDefault()?
                        .Value<string>();

                    var authorData = lockupViewModel
                        .FindTokens("metadata")
                        .FirstOrDefault()?
                        .FindTokens("lockupMetadataViewModel")
                        .FirstOrDefault()?
                        .FindTokens("metadata")
                        .FirstOrDefault()?
                        .FindTokens("contentMetadataViewModel")
                        .FirstOrDefault()?
                        .FindTokens("metadataRows")
                        .FirstOrDefault()?
                        .FindTokens("metadataParts")
                        .FirstOrDefault()?
                        .FindTokens("text")
                        .FirstOrDefault()?
                        .FindTokens("content")
                        .FirstOrDefault()?
                        .Value<string>();

                    string channelId = null;
                    string author = authorData;

                    var channelIdToken = lockupViewModel
                        .FindTokens("decoratedAvatarViewModel")
                        .FirstOrDefault()?
                        .FindTokens("rendererContext")
                        .FirstOrDefault()?
                        .FindTokens("commandContext")
                        .FirstOrDefault()?
                        .FindTokens("onTap")
                        .FirstOrDefault()?
                        .FindTokens("innertubeCommand")
                        .FirstOrDefault()?
                        .FindTokens("browseEndpoint")
                        .FirstOrDefault()?
                        .FindTokens("browseId")
                        .FirstOrDefault()?
                        .Value<string>();

                    if (!string.IsNullOrEmpty(channelIdToken))
                    {
                        channelId = channelIdToken;
                    }

                    var durationText = lockupViewModel
                        .FindTokens("thumbnailBadgeViewModel")
                        .FirstOrDefault()?
                        .FindTokens("text")
                        .FirstOrDefault()?
                        .Value<string>();

                    TimeSpan? duration = null;
                    if (!string.IsNullOrEmpty(durationText))
                    {
                        if (TimeSpan.TryParse(durationText, out var parsedDuration))
                        {
                            duration = parsedDuration;
                        }
                    }

                    var trackThumbnails = new List<Thumbnail>();
                    var thumbnailSources = lockupViewModel
                        .FindTokens("thumbnailViewModel")
                        .FirstOrDefault()?
                        .FindTokens("image")
                        .FirstOrDefault()?
                        .FindTokens("sources")
                        .AsJEnumerable()
                        .Children();

                    if (thumbnailSources != null)
                    {
                        foreach (var source in thumbnailSources)
                        {
                            var thumbNailUrl = source.FindTokens("url").FirstOrDefault()?.Value<string>();
                            var width = source.FindTokens("width").FirstOrDefault()?.Value<int?>();
                            var height = source.FindTokens("height").FirstOrDefault()?.Value<int?>();

                            if (!string.IsNullOrEmpty(thumbNailUrl) && width.HasValue && height.HasValue)
                            {
                                var resolution = new Resolution(width.Value, height.Value);
                                trackThumbnails.Add(new Thumbnail(thumbNailUrl, resolution));
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(contentId) && !string.IsNullOrEmpty(title))
                    {
                        if (author != null && author.EndsWith(" - Topic"))
                        {
                            author = author.Substring(0, author.Length - " - Topic".Length);
                        }

                        var track = new Track
                        {
                            Id = contentId,
                            Title = title,
                            Author = author,
                            AuthorChannelId = channelId,
                            Duration = duration,
                            Thumbnails = trackThumbnails
                        };

                        tracks.Add(track);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        var headerToken = trackJson.FindTokens("header")?.FirstOrDefault();
        if (headerToken != null)
        {
            var titleToken = headerToken
                .FindTokens("playlistHeaderRenderer")
                .FirstOrDefault()?
                .FindTokens("title")
                .FirstOrDefault()?
                .FindTokens("simpleText")
                .FirstOrDefault()?
                .Value<string>();

            if (!string.IsNullOrEmpty(titleToken))
            {
                albumTitle = titleToken;
                var albumPrefix = "Album - ";
                if (albumTitle.StartsWith(albumPrefix))
                {
                    albumTitle = albumTitle.Substring(albumPrefix.Length);
                }
            }

            var thumbnailTokens = headerToken
                .FindTokens("playlistHeaderRenderer")
                .FirstOrDefault()?
                .FindTokens("playlistHeaderBanner")
                .FirstOrDefault()?
                .FindTokens("heroPlaylistThumbnailRenderer")
                .FirstOrDefault()?
                .FindTokens("thumbnail")
                .FirstOrDefault()?
                .FindTokens("thumbnails")
                .AsJEnumerable()
                .Children();

            if (thumbnailTokens != null)
            {
                foreach (var thumbToken in thumbnailTokens)
                {
                    var thumbNailUrl = thumbToken.FindTokens("url").FirstOrDefault()?.Value<string>();
                    var width = thumbToken.FindTokens("width").FirstOrDefault()?.Value<int?>();
                    var height = thumbToken.FindTokens("height").FirstOrDefault()?.Value<int?>();

                    if (!string.IsNullOrEmpty(thumbNailUrl) && width.HasValue && height.HasValue)
                    {
                        var resolution = new Resolution(width.Value, height.Value);
                        albumThumbnails.Add(new Thumbnail(thumbNailUrl, resolution));
                    }
                }
            }
        }

        return new AlbumTracksResult
        {
            AlbumTitle = albumTitle ?? "Unknown Album",
            Tracks = tracks,
            Thumbnails = albumThumbnails
        };
    }
    private async Task<List<StreamData>> GetIFramePlayerAsync(string trackId, CancellationToken cancellationToken)
    {
        List<StreamData> results = new List<StreamData>();

        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36"
        );
        var iframe = await httpClient.GetStringAsync(
            "https://music.youtube.com/iframe_api",
            cancellationToken
        );

        var version = Regex.Match(iframe, @"player\\?/([0-9a-fA-F]{8})\\?/").Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(version))
        {
            var player = await httpClient.GetStringAsync(
                $"https://music.youtube.com/s/player/{version}/player_ias.vflset/en_US/base.js",
                cancellationToken
            );

            var signatureTimestamp = TryDecipher(player, out var operations);
            
            if (signatureTimestamp.Success)
            {
                var signatureTimestampValue = signatureTimestamp.Value;
                var url = "https://www.youtube.com/youtubei/v1/player";

                var payload = new
                {
                    videoId = trackId,
                    context = new
                    {
                        client = new
                        {
                            clientName = "TVHTML5_SIMPLY_EMBEDDED_PLAYER",
                            clientVersion = "2.0",
                            hl = "en",
                            gl = "US",
                            utcOffsetMinutes = 0
                        },
                        thirdParty = new
                        {
                            embedUrl = "https://music.youtube.com"
                        }
                    },
                    playbackContext = new
                    {
                        contentPlaybackContext = new
                        {
                            signatureTimestamp = signatureTimestampValue
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

                var rawResult =
                    await (new HttpSender()).SendHttpRequestAsync(request, cancellationToken);
                using var doc = JsonDocument.Parse(rawResult);
                JsonElement rootElement = doc.RootElement.Clone();

                JsonElement? streamsBlock =
                    rootElement.GetPropertyOrNull("streamingData")?.GetPropertyOrNull("formats");

                if (streamsBlock != null)
                {
                    var streamData = streamsBlock.Value.EnumerateArrayOrEmpty().ToArray();

                    foreach (var streamElement in streamData)
                    {
                        try
                        {
                            AgeRestrictedStreamData ageRestrictedStreamData =
                                JsonConvert.DeserializeObject<AgeRestrictedStreamData>(streamElement.ToString());

                            if (ageRestrictedStreamData != null && ageRestrictedStreamData.SignatureCipher != null)
                            {

                                string urlDecode = HttpUtility.UrlDecode(ageRestrictedStreamData.SignatureCipher);
                                NameValueCollection parameters = HttpUtility.ParseQueryString(urlDecode);
                                string signature = parameters["s"];
                                string signatureParameter = parameters["sp"];
                                DecipherManifest cipherManifest = new DecipherManifest(signatureTimestampValue, operations);

                                var streamUrl =
                                    new Uri(HttpUtility.UrlDecode(
                                            ageRestrictedStreamData.SignatureCipher
                                                .Substring(ageRestrictedStreamData.SignatureCipher.IndexOf("url=", StringComparison.Ordinal) + 4)))
                                        .AddQueryParameter(signatureParameter ?? "sig",
                                            cipherManifest.Decipher(signature)).ToString();

                                StreamData stream = JsonConvert.DeserializeObject<StreamData>(streamElement.ToString());
                                if (stream != null)
                                {
                                    stream.Url = streamUrl;
                                    results.Add(stream);
                                }
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                }
            }

        }

        return results;
    }

    private static Group TryDecipher(string player, out List<IDecipher> operations)
    {
        var signatureTimestamp = Regex
            .Match(player, @"(?:signatureTimestamp|sts):(\d{5})")
            .Groups[1];

        var cipherFunctions = Regex
            .Match(
                player,
                @"[$_\w]+=function\([$_\w]+\){([$_\w]+)=\1\.split\([\'""]{2}\);.*?return \1\.join\([\'""]{2}\)}",
                RegexOptions.Singleline
            )
            .Groups[0]
            .Value;

        var cipherName = Regex
            .Match(cipherFunctions, @"([$_\w]+)\.[$_\w]+\([$_\w]+,\d+\);")
            .Groups[1]
            .Value;


        var cipherDefinition = Regex
            .Match(
                player,
                $@"{Regex.Escape(cipherName)}={{.*?}};", RegexOptions.Singleline)
            .Groups[0]
            .Value;

        var swapFunctionName = Regex
            .Match(
                cipherDefinition,
                @"([$_\w]+):function\([$_\w]+,[$_\w]+\){+[^}]*?%[^}]*?}",
                RegexOptions.Singleline
            )
            .Groups[1]
            .Value;

        var spliceFunctionName = Regex
            .Match(
                cipherDefinition,
                @"([$_\w]+):function\([$_\w]+,[$_\w]+\){+[^}]*?splice[^}]*?}",
                RegexOptions.Singleline
            )
            .Groups[1]
            .Value;


        var reverseFunctionName = Regex
            .Match(
                cipherDefinition,
                @"([$_\w]+):function\([$_\w]+\){+[^}]*?reverse[^}]*?}",
                RegexOptions.Singleline
            )
            .Groups[1]
            .Value;

        operations = new List<IDecipher>();

        foreach (var statement in cipherFunctions.Split(';'))
        {
            var calledFunctionName = Regex
                .Match(statement, @"[$_\w]+\.([$_\w]+)\([$_\w]+,\d+\)")
                .Groups[1].Value;

            if (string.IsNullOrWhiteSpace(calledFunctionName))
            {
                continue;
            }

            if (string.Equals(calledFunctionName, swapFunctionName, StringComparison.Ordinal))
            {
                var value = Regex.Match(statement, @"\([$_\w]+,(\d+)\)")
                    .Groups[1].Value;

                if (int.TryParse(value, out int index))
                {
                    operations.Add(new SwapDecipher(index));
                }
            }
            else if (string.Equals(calledFunctionName, spliceFunctionName, StringComparison.Ordinal))
            {
                var value = Regex.Match(statement, @"\([$_\w]+,(\d+)\)").Groups[1].Value;

                if (int.TryParse(value, out int index))
                {
                    operations.Add(new SpliceDecipher(index));
                }
            }
            else if (string.Equals(calledFunctionName, reverseFunctionName, StringComparison.Ordinal))
            {
                operations.Add(new ReverseDecipher());
            }
        }

        return signatureTimestamp;
    }
}