using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;
using YTMusicAPI.Abstraction;
using YTMusicAPI.Model;
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

        string id = trackBlock?.GetPropertyOrNull("videoId").ToString();
        string title = trackBlock?.GetPropertyOrNull("title").ToString();
        string channelId = trackBlock?.GetPropertyOrNull("channelId").ToString();
        string author = trackBlock?.GetPropertyOrNull("author").ToString();
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
            
        }

        return track;
    }
}