using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface ITrackClient
{
    Task<Track> GetTrackInfoAsync(string trackUrl, CancellationToken cancellationToken);

    Task<List<Track>> GetAlbumTracks(string albumUrl, CancellationToken cancellationToken);
}