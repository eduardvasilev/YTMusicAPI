namespace YTMusicAPI.Model;

public class Artist
{
    public string Id { get; set; }
    public string Url => $"https://music.youtube.com/channel/{Id}";
    public string Title { get; set; }
    public IReadOnlyList<Thumbnail> Thumbnails { get; set; }
}