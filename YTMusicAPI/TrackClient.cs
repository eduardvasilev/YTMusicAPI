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

        const string url = $"https://music.youtube.com/youtubei/v1/player?key={ApiKey}";

        var payload = new
        {
            videoId = trackId,
            context = new
            {
                client = new
                {
                    clientName = "ANDROID_TESTSUITE",
                    clientVersion = "1.9",
                    androidSdkVersion = 30,
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
                "application/json"
            )
        };

        request.Headers.Add(
            "User-Agent",
            "com.google.android.youtube/17.36.4 (Linux; U; Android 12; GB) gzip"
        );

        var rawResult = await (new HttpSender(new HttpClient())).SendHttpRequestAsync(request, cancellationToken);

        using var doc = JsonDocument.Parse(rawResult);
        JsonElement rootElement = doc.RootElement.Clone();

        JsonElement? trackBlock = rootElement.GetPropertyOrNull("videoDetails");

        var track = BuildTrack(trackBlock);

        JsonElement? streamsBlock = rootElement.GetPropertyOrNull("streamingData")?.GetPropertyOrNull("formats");
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

        var thumbnails = new List<Thumbnail>();

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
            Thumbnails = thumbnails
        };
        return track;
    }

    public async Task<List<Track>> GetAlbumTracks(string albumUrl, CancellationToken cancellationToken)
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
        string rawResult = await (new HttpSender(new HttpClient())).SendHttpRequestAsync(request, cancellationToken);

        using var doc = JsonDocument.Parse(rawResult);
        JsonElement rootElement = doc.RootElement.Clone();

        List<Track> tracks = new List<Track>();

        JObject trackJson = JObject.Parse(rootElement.ToString());

        IJEnumerable<JToken> tracksBlock = trackJson.FindTokens("playlistVideoListRenderer").FirstOrDefault()
            ?.FindTokens("contents").Children();

        if (tracksBlock != null)
        {
            foreach (var trackBlock in tracksBlock)
            {
                JToken trackRenderer = trackBlock.FindTokens("playlistVideoRenderer").FirstOrDefault();
                if (trackRenderer != null)
                {
                    var track = BuildTrack(JsonDocument.Parse(trackRenderer.ToString()).RootElement.Clone(), titleResolver:
                        block => block?.GetPropertyOrNull("title")?.GetPropertyOrNull("runs")?.EnumerateArray()
                            .FirstOrDefault().GetPropertyOrNull("text")?.ToString(), artist =>
                        {
                            var channelId = artist?.GetPropertyOrNull("shortBylineText")?.GetPropertyOrNull("runs")?.EnumerateArray()
                                .FirstOrDefault().GetPropertyOrNull("navigationEndpoint")?.GetPropertyOrNull("browseEndpoint")?.GetPropertyOrNull("browseId")?.ToString();

                            var author = artist?.GetPropertyOrNull("shortBylineText")?.GetPropertyOrNull("runs")?.EnumerateArray()
                                .FirstOrDefault().GetPropertyOrNull("text")?.ToString();

                            return (channelId,  author);
                        });
                    tracks.Add(track);
                }
            }
        }
        return tracks;
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
                    await (new HttpSender(new HttpClient())).SendHttpRequestAsync(request, cancellationToken);
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