namespace YTMusicAPI.Model;

public class Album
{
    public string Id { get; set; }
    public string Url => $"https://music.youtube.com/playlist?list={Id}";
    public string Title { get; set; }
    public string Author { get; set; }
    public string AuthorChannelId { get; set; }
    public string Year { get; set; }
    public string RecordType { get; set; }
    public IReadOnlyList<Thumbnail> Thumbnails { get; set; }

}