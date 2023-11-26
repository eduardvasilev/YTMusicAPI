using Newtonsoft.Json.Linq;

namespace YTMusicAPI.Model.Infrastructure;

public class ContinuationData
{
    public ContinuationData()
    {
        
    }

    public ContinuationData(string continuationToken, string token)
    {
        ContinuationToken = continuationToken;
        Token = token;
    }

    public string Token { get; set; }

    public string ContinuationToken { get; set; }
}