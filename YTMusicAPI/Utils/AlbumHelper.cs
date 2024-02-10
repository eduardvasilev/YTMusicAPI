using System.Text.RegularExpressions;

namespace YTMusicAPI.Utils;

public static class AlbumHelper
{
    public static string GetAlbumId(string albumUrl)
    {
        return Regex.Match(albumUrl, @"youtube\..+?/playlist.*?list=(.*?)(?:&|/|$)").Groups[1].Value;
    }
}