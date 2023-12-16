using Newtonsoft.Json;

namespace YTMusicAPI.Model.Domain;

public class AgeRestrictedStreamData
{
    [JsonProperty("itag")]
    public int Itag { get; set; }

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

    [JsonProperty("quality")]
    public string Quality { get; set; }

    [JsonProperty("xtags")]
    public string Xtags { get; set; }

    [JsonProperty("fps")]
    public int Fps { get; set; }

    [JsonProperty("qualityLabel")]
    public string QualityLabel { get; set; }

    [JsonProperty("projectionType")]
    public string ProjectionType { get; set; }

    [JsonProperty("audioQuality")]
    public string AudioQuality { get; set; }

    [JsonProperty("approxDurationMs")]
    public string ApproxDurationMs { get; set; }

    [JsonProperty("audioSampleRate")]
    public string AudioSampleRate { get; set; }

    [JsonProperty("audioChannels")]
    public int AudioChannels { get; set; }

    [JsonProperty("signatureCipher")]
    public string SignatureCipher { get; set; }
}