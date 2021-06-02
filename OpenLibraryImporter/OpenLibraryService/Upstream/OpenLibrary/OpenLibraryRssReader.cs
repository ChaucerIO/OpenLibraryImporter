using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using OpenLibraryService.Utilities;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class OpenLibraryRssReader :
        IOpenLibraryCatalogReader
    {
        private readonly HttpClient _client;
        private readonly string _rssUrl;
        private readonly ILogger<IOpenLibraryCatalogReader> _logger;

        public OpenLibraryRssReader(
            HttpClient client,
            string rssUrl,
            ILogger<IOpenLibraryCatalogReader> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _rssUrl = string.IsNullOrWhiteSpace(rssUrl) ? throw new ArgumentNullException(nameof(rssUrl)) : rssUrl;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyCollection<OpenLibraryDownload>> GetDownloadsForType(OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var feedItems = await GetFeedTimestamps(ct);
            var downloadsByDateTasks = feedItems
                .Select(d => GetDownload(d, archiveType, ct))
                .ToList();
            await Task.WhenAll(downloadsByDateTasks);

            var availableForType = downloadsByDateTasks
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .Where(dl =>  dl is not null)
                .ToList();
            return availableForType;
        }

        public async Task<IReadOnlyCollection<OpenLibraryDownload>> GetLatestVersionForTypes(IEnumerable<OpenLibraryArchiveType> archiveTypes, CancellationToken ct)
        {
            if (archiveTypes is null || !archiveTypes.Any())
            {
                throw new ArgumentNullException(nameof(archiveTypes));
            }
            
            var latestVersionForTypes = archiveTypes.ToDictionary(a => a, _ => (OpenLibraryDownload) null);
            var feedItems = await GetFeedTimestamps(ct);
            
            foreach (var entry in feedItems)
            {
                var stop = latestVersionForTypes.Values.All(v => v is not null);
                if (stop)
                {
                    return latestVersionForTypes.Values;
                }
                
                var downloadsForDateTasks = latestVersionForTypes
                    .Where(lv => lv.Value is null)
                    .Select(lv => GetDownload(entry.Date, lv.Key, ct))
                    .ToList();
                await Task.WhenAll(downloadsForDateTasks);

                var failures = downloadsForDateTasks.Where(t => !t.IsCompletedSuccessfully).ToList();
                if (failures.Any())
                {
                    foreach (var failure in downloadsForDateTasks.Where(t => !t.IsCompletedSuccessfully))
                    {
                        _logger.LogError(failure.Exception?.Flatten(), $"Checking the Open Library RSS feed for downloads associated with {entry.Date.ToIsoDateString()} failed");
                    }

                    return null;
                }
                
                var downloadsForDate = downloadsForDateTasks
                    .Where(t => t.IsCompletedSuccessfully)
                    .Select(t => t.Result)
                    .Where(ad => latestVersionForTypes[ad.ArchiveType] is null);
                foreach (var availableDownload in downloadsForDate)
                {
                    latestVersionForTypes[availableDownload.ArchiveType] = availableDownload;
                }
            }

            throw new ApplicationException($"Could not find downloads for all types {string.Join(", ", latestVersionForTypes.Keys)}");
        }
        
        public async Task<OpenLibraryDownload> GetLatestVersionForType(OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var plural = await GetLatestVersionForTypes(new[] {archiveType}, ct);
            return plural.SingleOrDefault();
        }

        /// <summary>
        /// Returns an ordered collection of date-only timestamps that have downloads associated with the entry.
        /// 
        /// The only useful element of the RSS feed is the date stamp which is in ISO-8601 format. Nothing else can be gleaned from a download item 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<IReadOnlyList<DateTime>> GetFeedTimestamps(CancellationToken ct)
        {
            _logger.LogInformation($"Checking Open Library RSS feed for updates: {_rssUrl}");
            var timer = Stopwatch.StartNew();
            using (var response = await _client.GetAsync(_rssUrl, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                if (!response.IsSuccessStatusCode)
                {
                    timer.Stop();
                    _logger.LogError($"Failure in {timer.ElapsedMilliseconds:N0}ms when checking the Open Library RSS feed for updates.", response);
                    response.EnsureSuccessStatusCode();
                }
                
                var rss = await response.Content.ReadAsStringAsync(ct);
                using (var sr = new StringReader(rss))
                using (var reader = XmlReader.Create(sr))
                {
                    var feed = SyndicationFeed.Load(reader);
                    timer.Stop();
                    var items = feed.Items
                        .Where(i => i.Title.Text?.StartsWith("ol_dump", StringComparison.OrdinalIgnoreCase) == true)
                        .Select(i => i.Title.Text) // Note to future self: there is no other useful information in these feed items
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(i => DateTime.Parse(ExtractDateStamp(i)))
                        .OrderByDescending(d => d)
                        .ToList();

                    _logger.LogInformation($"Finished checking Open Library RSS feed for updates in {timer.ElapsedMilliseconds:N0}ms with {items.Count:N0} items found");
                    return items;
                }
            }
        }

        private async Task<OpenLibraryDownload> GetDownload(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            // https://archive.org/download/ol_dump_2021-03-19/ol_dump_editions_2021-03-19.txt.gz
            var formattedDate = date.ToIsoDateString();
            var url = $"https://archive.org/download/ol_dump_{formattedDate}/ol_dump_{archiveType.GetKey()}_{formattedDate}.txt.gz";
            
            _logger.LogInformation($"Checking {url}");
            var timer = Stopwatch.StartNew();
            OpenLibraryDownload dl = null;
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                timer.Stop();
                if (response.IsSuccessStatusCode)
                {
                    dl = new OpenLibraryDownload
                    {
                        Datestamp = date,
                        Source = url,
                        ArchiveType = archiveType,
                    };
                }
            }
            timer.Stop();
            _logger.LogInformation($"Checked {url} in {timer.ElapsedMilliseconds}ms. Exists = {dl is not null}");
            return dl;
        }

        /// <summary>
        /// Returns the date stamp (e.g. 2021-04-12) from a string like ol_dump_2021-04-12
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string ExtractDateStamp(string title)
            => title.Split("_").Last();
    }
}