namespace YTMusicAPI.Model;

public class SearchingResult<T>
{

    public SearchingResult(IEnumerable<T> result, string continuationToken, string token)
    {
        Result = result;
        ContinuationToken = continuationToken;
        Token = token;
    }

    public IEnumerable<T> Result { get; set; }
    public string ContinuationToken { get; set; }
    public string Token { get; set; }
}