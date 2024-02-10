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

        [Fact]
        public async Task GetAlbumTracks_Success()
        {
            TrackClient trackClient = new TrackClient();
            var track = await trackClient.GetAlbumTracks("https://music.youtube.com/playlist?list=OLAK5uy_lNf-Zf5HwjuEtV9_5oj4I3vcPT0TehuU4", CancellationToken.None);

            track.Should().NotBeNull();

            track.First().Url.Should().Be("https://music.youtube.com/watch?v=YUPiIM4mZuI");
            track.First().AuthorUrl.Should().Be("https://music.youtube.com/channel/UCeiRyLo_Q9q4tlv9aaQJF5w");
            track.First().Title.Should().Be("Alphabet City");
            track.First().Thumbnails.Count.Should().Be(4);
        }
    }
}