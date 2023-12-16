using System.Text;

namespace YTMusicAPI.Utils;

public static class StringHelper
{
    public static string SwapChars(this string str, int firstCharIndex, int secondCharIndex) =>
        new StringBuilder(str)
        {
            [firstCharIndex] = str[secondCharIndex],
            [secondCharIndex] = str[firstCharIndex]
        }.ToString();

    public static string Reverse(this string str)
    {
        var buffer = new StringBuilder(str.Length);

        for (var i = str.Length - 1; i >= 0; i--)
            buffer.Append(str[i]);

        return buffer.ToString();
    }
}