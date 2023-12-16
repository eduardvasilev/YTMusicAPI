using System.Text;
using System.Text.RegularExpressions;

namespace YTMusicAPI.Utils;

public static class TrackHelper
{
    public static string GetTrackId(string trackUrl)
    {
        return Regex.Match(trackUrl, @"youtube\..+?/watch.*?v=(.*?)(?:&|/|$)").Groups[1].Value;
    }
}