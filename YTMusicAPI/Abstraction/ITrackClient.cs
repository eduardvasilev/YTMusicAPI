using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface ITrackClient
{
    /// <summary>
    /// Returns metadata of a track
    /// </summary>
    /// <param name="trackUrl"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Track> GetTrackInfoAsync(string trackUrl, CancellationToken cancellationToken);


    /// <summary>
    /// Returns tracks list of an album
    /// </summary>
    /// <param name="albumUrl"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AlbumTracksResult> GetAlbumTracksAsync(string albumUrl, CancellationToken cancellationToken);
}