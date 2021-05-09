using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class OpenLibraryDataManager
    {
        private readonly string _url;
        private readonly HttpClient _client;
        private readonly ILogger<OpenLibraryDataManager> _logger;

        public OpenLibraryDataManager(string url, HttpClient client, ILogger<OpenLibraryDataManager> logger)
        {
            _url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentNullException(nameof(url)) : url;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<OpenLibraryDownload>> CheckForUpdatesAsync(DateTime lastUpdate)
        {
            _logger.LogInformation($"Checking for updates to the Open Library Data");
            var timer = Stopwatch.StartNew();
            
            var dateStamps = await CheckFeedAsync();
            var upstreamPublishStamp = dateStamps
                .OrderByDescending(d => d)
                .First();
            var upstreamPublishTime = DateTime.Parse(upstreamPublishStamp);

            if (upstreamPublishTime.Date <= lastUpdate.Date)
            {
                return new List<OpenLibraryDownload>();
            }

            var newDownloads = await CheckAuthorsAndEditionsAsync(upstreamPublishStamp);

            timer.Stop();
            _logger.LogInformation($"Finished checking for updates to the Open Library Data in {timer.ElapsedMilliseconds:N0}ms with {newDownloads.Count} updates to load");
            
            return newDownloads;
        }

        private async Task<List<string>> CheckFeedAsync()
        {
            _logger.LogInformation("Checking Open Library RSS feed for updates");
            var timer = Stopwatch.StartNew();
            using (var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                {
                    timer.Stop();
                    _logger.LogError($"Failure in {timer.ElapsedMilliseconds:N0}ms when checking the Open Library RSS feed for updates.", response);
                    response.EnsureSuccessStatusCode();
                }
                
                var rss = await response.Content.ReadAsStringAsync();
                using (var sr = new StringReader(rss))
                using (var reader = XmlReader.Create(sr))
                {
                    var feed = SyndicationFeed.Load(reader);
                    var items = feed.Items
                        .Select(i => i.Title.Text)
                        .Where(t => t.StartsWith("ol_dump", StringComparison.OrdinalIgnoreCase))
                        .Select(t => ExtractDateStamp(t))
                        .ToList();
                    
                    timer.Stop();
                    _logger.LogInformation($"Finished checking Open Library RSS feed for updates in {timer.ElapsedMilliseconds:N0} with {items.Count:N0} items found");
                    
                    return items;
                }
            }
        }

        public async Task<List<OpenLibraryDownload>> CheckAuthorsAndEditionsAsync(string dateStamp)
        {
            var checks = new[]
            {
                CheckAuthorsAsync(dateStamp),
                CheckEditionsAsync(dateStamp),
            };
            await Task.WhenAll(checks);

            var toRefresh = checks
                .Where(c => c.IsCompletedSuccessfully && c.Result is not null)
                .Select(c => c.Result)
                .ToList();
            return toRefresh;
        }

        /// <summary>
        /// Returns the date stamp (e.g. 2021-04-12) from a string like ol_dump_2021-04-12
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string ExtractDateStamp(string title)
            => title.Split("_").Last();
        
        private const string _slug = "https://archive.org/download";
        private const string _ext = "txt.gz";
        private async Task<OpenLibraryDownload> CheckEditionsAsync(string dateStamp)
        {
            // https://archive.org/download/ol_dump_2021-03-19/ol_dump_editions_2021-03-19.txt.gz
            var url = $"{_slug}/ol_dump_{dateStamp}/ol_dump_editions_{dateStamp}.{_ext}";
            
            _logger.LogInformation($"Checking editions: {url}");
            var timer = Stopwatch.StartNew();
            
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                timer.Stop();
                _logger.LogInformation($"Checked editions in {timer.ElapsedMilliseconds:N0}ms. New version found = {response.IsSuccessStatusCode}");
                return response.IsSuccessStatusCode
                    ? new OpenLibraryDownload
                    {
                        Url = url,
                        Datestamp = DateTime.Parse(dateStamp),
                        Type = OpenLibraryType.Editions,
                    }
                    : null;
            }
        }

        private async Task<OpenLibraryDownload> CheckAuthorsAsync(string dateStamp)
        {
            // https://archive.org/download/ol_dump_2021-03-19/ol_dump_authors_2021-03-19.txt.gz
            var url = $"{_slug}/ol_dump_{dateStamp}/ol_dump_authors_{dateStamp}.{_ext}";
            
            _logger.LogInformation($"Checking authors: {url}");
            var timer = Stopwatch.StartNew();
            
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                timer.Stop();
                _logger.LogInformation($"Checked authors in {timer.ElapsedMilliseconds:N0}ms. New version found = {response.IsSuccessStatusCode}");
                return response.IsSuccessStatusCode
                    ? new OpenLibraryDownload
                    {
                        Url = url,
                        Datestamp = DateTime.Parse(dateStamp),
                        Type = OpenLibraryType.Authors,
                    }
                    : null;
            }
        }
    }
}