using FluentAssertions;
using YTMusicAPI.Model;
using YTMusicAPI.Model.Domain;
using YTMusicAPI.Model.Infrastructure;

namespace YTMusicAPI.Tests
{
    public class SearchClientTests
    {
        [Fact]
        public async Task GetArtistsChannelAsync_Success()
        {
            SearchClient searchClient = new SearchClient();
            SearchingResult<Artist> firstPage = await searchClient.SearchArtistsChannelsAsync(new QueryRequest
            {
                Query = "o"
            }, CancellationToken.None);
            
            SearchingResult<Artist> secondPage = await searchClient.SearchArtistsChannelsAsync(new QueryRequest
            {
                Query = "o",
                ContinuationData = new ContinuationData(firstPage.ContinuationToken, firstPage.Token),
                ContinuationNeed = true,
                
            }, CancellationToken.None);

            firstPage.Result.FirstOrDefault()?.Title.Should().NotBe(secondPage.Result.FirstOrDefault()?.Title);
            firstPage.Result.Concat(secondPage.Result).Select(x => x.Url).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task GeAlbumsAsync_Success()
        {
            SearchClient searchClient = new SearchClient();
            SearchingResult<Album> firstPage = await searchClient.SearchAlbumsAsync(new QueryRequest
            {
                Query = "ok computer"
            }, CancellationToken.None);

            SearchingResult<Album> secondPage = await searchClient.SearchAlbumsAsync(new QueryRequest
            {
                Query = "ok computer",
                ContinuationData = new ContinuationData(firstPage.ContinuationToken, firstPage.Token),
                ContinuationNeed = true,

            }, CancellationToken.None);

            firstPage.Result.FirstOrDefault()?.Title.Should().NotBe(secondPage.Result.FirstOrDefault()?.Title);
            firstPage.Result.Concat(secondPage.Result).Select(x => x.Url).Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task SearchTracksAsync_Success()
        {
            SearchClient searchClient = new SearchClient();
            SearchingResult<Track> firstPage = await searchClient.SearchTracksAsync(new QueryRequest
            {
                Query = "Blink-182"
            }, CancellationToken.None);

            SearchingResult<Track> secondPage = await searchClient.SearchTracksAsync(new QueryRequest
            {
                Query = "Blink-182",
                ContinuationData = new ContinuationData(firstPage.ContinuationToken, firstPage.Token),
                ContinuationNeed = true,

            }, CancellationToken.None);

            firstPage.Result.FirstOrDefault()?.Title.Should().NotBe(secondPage.Result.FirstOrDefault()?.Title);
            firstPage.Result.Concat(secondPage.Result).Select(x => x.Url).Should().OnlyHaveUniqueItems();
        }
    }
}