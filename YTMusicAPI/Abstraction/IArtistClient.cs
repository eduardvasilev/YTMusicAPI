namespace YTMusicAPI.Abstraction;

public interface IArtistClient
{
    Task<string> GetArtistImageAsync(string artistUrl, CancellationToken cancellationToken);
}