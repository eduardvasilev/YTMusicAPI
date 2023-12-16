using FluentAssertions;

namespace YTMusicAPI.Tests
{
    public class TrackClientTests
    {
        [Fact]
        public async Task GetTrackInfo_Success()
        {
            TrackClient trackClient = new TrackClient();
            var track = await trackClient.GetTrackInfoAsync("https://music.youtube.com/watch?v=EOjm4SEDMu8&si=Cx6Uv7fUm5Hv_DhB", CancellationToken.None);

            track.Should().NotBeNull();
            track.Author.Should().Be("Kings Of Leon");
            track.Title.Should().Be("Sex on Fire");
            track.Streams.Should().NotBeEmpty();
            track.Thumbnails.Should().NotBeEmpty();
            track.AuthorUrl.Should().Be("https://music.youtube.com/channel/UCHqD2OBWbcWGmCve99uw47A");
        } 
        
        [Fact]
        public async Task GetTrackInfo_TrackIsExplicit_Success()
        {
            TrackClient trackClient = new TrackClient();
            var track = await trackClient.GetTrackInfoAsync("https://music.youtube.com/watch?v=7EqnoEljHCw", CancellationToken.None);

            track.Should().NotBeNull();
            track.Streams.Should().NotBeEmpty(); //TODO we need to get streams for implicit tracks
        }

    }
}