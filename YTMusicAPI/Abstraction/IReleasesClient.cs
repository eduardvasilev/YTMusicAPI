using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface IReleasesClient
{
    /// <summary>
    /// Returns list of new released albums and singles 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<Album>> GetReleasesAsync(CancellationToken cancellationToken);
}