namespace YTMusicAPI.Model.Deciphers;

internal class DecipherManifest
{
    public string SignatureTimestamp { get; }

    public IReadOnlyList<IDecipher> Operations { get; }

    public DecipherManifest(string signatureTimestamp, IReadOnlyList<IDecipher> operations)
    {
        SignatureTimestamp = signatureTimestamp;
        Operations = operations;
    }

    public string Decipher(string input) =>
        Operations.Aggregate(input, (value, decipher) => decipher.Decipher(value));
}
