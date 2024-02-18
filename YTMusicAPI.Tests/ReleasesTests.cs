using FluentAssertions;
using YTMusicAPI.Model.Domain;

namespace YTMusicAPI.Tests;

public class ReleasesTests
{
    [Fact]
    public async Task GetReleasesAsync_Successful()
    {
        ReleasesClient releasesClient = new ReleasesClient();

        List<Album> releases = await releasesClient.GetReleasesAsync(CancellationToken.None);

        releases.Should().NotBeEmpty();
    }
}