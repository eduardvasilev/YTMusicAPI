using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Model;

public class AlbumTracksResult
{
    public List<Track> Tracks { get; set; }

    public string AlbumTitle { get; set; }
    public List<Thumbnail> Thumbnails { get; set; }
}