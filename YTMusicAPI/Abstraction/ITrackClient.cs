using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface ITrackClient
{
    Task<Track> GetTrackInfoAsync(string trackUrl, CancellationToken cancellationToken);
}