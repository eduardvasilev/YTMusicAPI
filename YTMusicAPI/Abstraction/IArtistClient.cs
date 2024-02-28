namespace YTMusicAPI.Abstraction;

public interface IArtistClient
{
    /// <summary>
    /// Returns main picture of an artist 
    /// </summary>
    /// <param name="artistUrl">Url of an artist</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string> GetArtistImageAsync(string artistUrl, CancellationToken cancellationToken);
}