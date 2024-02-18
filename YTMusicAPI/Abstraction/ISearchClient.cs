using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface ISearchClient
{
    /// <summary>
    /// Returns pageable list of artists
    /// </summary>
    /// <param name="queryRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SearchingResult<Artist>> SearchArtistsChannelsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Returns pageable list of albums
    /// </summary>
    /// <param name="queryRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SearchingResult<Album>> SearchAlbumsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns pageable list of tracks
    /// </summary>
    /// <param name="queryRequest"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SearchingResult<Track>> SearchTracksAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
}