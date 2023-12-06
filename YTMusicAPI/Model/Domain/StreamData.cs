using Newtonsoft.Json;

namespace YTMusicAPI.Model.Domain;

public class StreamData
{
    [JsonProperty("itag")]
    public int Itag { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("mimeType")]
    public string MimeType { get; set; }

    [JsonProperty("bitrate")]
    public int Bitrate { get; set; }

    [JsonProperty("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    public int Height { get; set; }

    [JsonProperty("lastModified")]
    public string LastModified { get; set; }

    [JsonProperty("contentLength")]
    public string ContentLength { get; set; }

    [JsonProperty("quality")]
    public string Quality { get; set; }

    [JsonProperty("fps")]
    public int Fps { get; set; }

    [JsonProperty("qualityLabel")]
    public string QualityLabel { get; set; }

    [JsonProperty("projectionType")]
    public string ProjectionType { get; set; }

    [JsonProperty("averageBitrate")]
    public int AverageBitrate { get; set; }

    [JsonProperty("audioQuality")]
    public string AudioQuality { get; set; }

    [JsonProperty("approxDurationMs")]
    public string ApproxDurationMs { get; set; }

    [JsonProperty("audioSampleRate")]
    public string AudioSampleRate { get; set; }

    [JsonProperty("audioChannels")]
    public int AudioChannels { get; set; }
}