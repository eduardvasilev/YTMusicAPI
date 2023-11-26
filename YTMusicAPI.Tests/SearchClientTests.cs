using FluentAssertions;
using YTMusicAPI.Model;
using YTMusicAPI.Model.Infrastructure;

namespace YTMusicAPI.Tests
{
    public class SearchClientTests
    {
        [Fact]
        public async Task GetArtistsChannelAsync_Success()
        {
            SearchClient searchClient = new SearchClient();
            SearchingResult<Artist> firstPage = await searchClient.GetArtistsChannelsAsync(new QueryRequest
            {
                Query = "o"
            }, CancellationToken.None);
            
            SearchingResult<Artist> secondPage = await searchClient.GetArtistsChannelsAsync(new QueryRequest
            {
                Query = "o",
                ContinuationData = new ContinuationData(firstPage.ContinuationToken, firstPage.Token),
                ContinuationNeed = true,
                
            }, CancellationToken.None);

            firstPage.Result.FirstOrDefault()?.Title.Should().NotBe(secondPage.Result.FirstOrDefault()?.Title);
            firstPage.Result.Concat(secondPage.Result).Select(x => x.Url).Should().OnlyHaveUniqueItems();
        }
    }
}