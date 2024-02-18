
# YouTube Music API Client

This library allows to retrieve music related data from YouTube Music. Search albums, tracks and artists. 


## Features

- Search albums
- Search tracks
- Search artists
- Get new releases
- Get tracks metadata
- Get album's tracks


## Usage/Examples

#### Search
```csharp
            SearchClient searchClient = new SearchClient();
         
            //returns first page of the search
            SearchingResult<Track> firstPage = await searchClient.SearchTracksAsync(new QueryRequest
            {
                Query = "Nirvana"
            }, CancellationToken.None);

            //returns second page of the search using continuation data
            SearchingResult<Track> secondPage = await searchClient.SearchTracksAsync(new QueryRequest
            {
                Query = "Nirvana",
                ContinuationData = new ContinuationData(firstPage.ContinuationToken, firstPage.Token),
                ContinuationNeed = true,

            }, CancellationToken.None);
```

#### Tracks info

```csharp
TrackClient trackClient = new TrackClient();
var track = await trackClient.GetTrackInfoAsync("https://music.youtube.com/watch?v=EOjm4SEDMu8&si=Cx6Uv7fUm5Hv_DhB", CancellationToken.None);

```
