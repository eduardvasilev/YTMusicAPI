using YTMusicAPI.Utils;

namespace YTMusicAPI.Model.Deciphers;

internal class SwapDecipher : IDecipher
{
    private readonly int _index;

    public SwapDecipher(int index) => _index = index;

    public string Decipher(string input) => input.SwapChars(0, _index);
}
