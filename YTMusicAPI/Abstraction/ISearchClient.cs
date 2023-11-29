using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Abstraction;

public interface ISearchClient
{
    Task<SearchingResult<Artist>> SearchArtistsChannelsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
    
    Task<SearchingResult<Album>> SearchAlbumsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);

    Task<SearchingResult<Track>> SearchTracksAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
}