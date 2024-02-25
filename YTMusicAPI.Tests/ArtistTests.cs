using FluentAssertions;

namespace YTMusicAPI.Tests;

public class ArtistTests
{
    [Fact]
    public async Task GetArtistImage_Success()
    {
        ArtistClient artistClient = new ArtistClient();
        var image = await artistClient.GetArtistImageAsync("https://music.youtube.com/channel/UC1bWXfKY5YoBduVCcSP8SGQ", CancellationToken.None);

        image.Should().NotBeNull();
        image.Should().StartWith("https://");
    }
}