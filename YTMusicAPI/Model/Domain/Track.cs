namespace YTMusicAPI.Model.Domain;

public class Track
{
    public string Id { get; set; }
    public string Url => $"https://music.youtube.com/watch?v={Id}";
    public string Title { get; set; }
    public string Author { get; set; }
    public string AuthorUrl => $"https://music.youtube.com/channel/{AuthorChannelId}";
    public string AuthorChannelId { get; set; }
    public IReadOnlyList<Thumbnail> Thumbnails { get; set; }

    public IReadOnlyList<StreamData> Streams { get; set; }

    public StreamData GetStreamWithHighestBitrate()
    {
        return Streams.MaxBy(stream => stream.Bitrate);
    }
}