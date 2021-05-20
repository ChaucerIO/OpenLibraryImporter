using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Commons;
using Microsoft.Extensions.Logging;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class HttpToFilesystemOpenLibraryDataManager :
        IOpenLibraryDataManager
    {
        private readonly HttpClient _client;
        private readonly IFilesystem _fs;
        private readonly ILogger<HttpToFilesystemOpenLibraryDataManager> _logger;

        public HttpToFilesystemOpenLibraryDataManager(HttpClient client, IFilesystem fs, ILogger<HttpToFilesystemOpenLibraryDataManager> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyCollection<OpenLibraryDownload>> CheckForUpdatesAsync(DateTime lastUpdate)
        {
            _logger.LogInformation("Checking for updates to the Open Library Data");
            var timer = Stopwatch.StartNew();
            
            var dateStamps = await GetOpenLibraryPublishedVersionsAsync();
            var rawUpstreamPublishStamp = dateStamps
                .OrderByDescending(d => d)
                .First();
            var upstreamPublishTimestamp = DateTime.Parse(rawUpstreamPublishStamp);

            var newDownloads = new List<OpenLibraryDownload>();
            if (lastUpdate.Date <= upstreamPublishTimestamp.Date)
            {
                newDownloads.AddRange(await GetOpenLibraryCatalogUpdateJobsAsync(upstreamPublishTimestamp));
            }

            timer.Stop();
            _logger.LogInformation($"Finished checking for updates to the Open Library Data in {timer.ElapsedMilliseconds:N0}ms with {newDownloads.Count} updates to load");
            
            return newDownloads;
        }
    }
}