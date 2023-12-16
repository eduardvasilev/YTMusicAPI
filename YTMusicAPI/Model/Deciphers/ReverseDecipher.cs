using YTMusicAPI.Utils;

namespace YTMusicAPI.Model.Deciphers;

internal class ReverseDecipher : IDecipher
{
    public string Decipher(string input) => input.Reverse();
}
