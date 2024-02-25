using System.Text.RegularExpressions;

namespace YTMusicAPI.Utils;

public class ArtistHelper
{
    public static string GetArtistId(string artistUrl)
    {
        return Regex.Match(artistUrl, @"youtube\..+?/channel/(.*?)(?:\?|&|/|$)").Groups[1].Value;
    }
}