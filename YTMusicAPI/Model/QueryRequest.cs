using YTMusicAPI.Model.Infrastructure;

namespace YTMusicAPI.Model;

public class QueryRequest
{
    public string Query { get; set; }

    public bool? ContinuationNeed { get; set; }
    public ContinuationData? ContinuationData { get; set; }
}