namespace YTMusicAPI.Model.Deciphers;

internal class SpliceDecipher : IDecipher
{
    private readonly int _index;

    public SpliceDecipher(int index) => _index = index;

    public string Decipher(string input) => input[_index..];
}
