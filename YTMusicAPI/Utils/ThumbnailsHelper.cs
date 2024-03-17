using YTMusicAPI.Model;

namespace YTMusicAPI.Utils;

public static class ThumbnailsHelper
{
    public static Thumbnail GetHighest(this List<Thumbnail> thumbnail)
    {
        return thumbnail.MaxBy(x => x.Resolution.Height * x.Resolution.Width);
    }

    public static Thumbnail GetLowest(this List<Thumbnail> thumbnail)
    {
        return thumbnail.MinBy(x => x.Resolution.Height * x.Resolution.Width);
    }
}