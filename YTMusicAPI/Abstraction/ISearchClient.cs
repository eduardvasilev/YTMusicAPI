using YTMusicAPI.Model;

namespace YTMusicAPI.Abstraction;

public interface ISearchClient
{
    Task<SearchingResult<Artist>> GetArtistsChannelsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
    
    Task<SearchingResult<Album>> GetAlbumsAsync(QueryRequest queryRequest,
        CancellationToken cancellationToken);
}