namespace YTMusicAPI.Model;
public class Thumbnail
{
    public Thumbnail(string url, Resolution resolution)
    {
        Url = url;
        Resolution = resolution;
    }

    public string Url { get; }
    public Resolution Resolution { get; }
}