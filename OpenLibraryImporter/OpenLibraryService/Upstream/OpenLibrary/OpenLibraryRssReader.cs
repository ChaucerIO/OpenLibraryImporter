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

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class OpenLibraryRssReader :
        IOpenLibraryCatalogReader
    {
        private readonly HttpClient _client;
        private readonly string _url;
        private readonly ILogger<IOpenLibraryCatalogReader> _logger;

        public OpenLibraryRssReader(HttpClient client, string url, ILogger<IOpenLibraryCatalogReader> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentNullException(nameof(url)) : url;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task<IReadOnlyCollection<DateTime>> GetCatalogDatestampsAsync()
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
                        .Select(t => DateTime.Parse(ExtractDateStamp(t)))
                        .ToList();
                    
                    timer.Stop();
                    _logger.LogInformation($"Finished checking Open Library RSS feed for updates in {timer.ElapsedMilliseconds:N0} with {items.Count:N0} items found");
                    
                    return items;
                }
            }
        }
        
        /// <summary>
        /// Returns the date stamp (e.g. 2021-04-12) from a string like ol_dump_2021-04-12
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string ExtractDateStamp(string title)
            => title.Split("_").Last();

        public async Task<IReadOnlyCollection<OpenLibraryDownload>> GetDownloadsForVersionAsync(DateTime datestamp)
        {
            var checks = new[]
            {
                CheckDataTypeAsync(datestamp, "authors"),
                CheckDataTypeAsync(datestamp, "editions"),
            };
            await Task.WhenAll(checks);

            var toRefresh = checks
                .Where(c => c.IsCompletedSuccessfully && c.Result is not null)
                .Select(c => c.Result)
                .ToList();
            return toRefresh;
        }
        
        private async Task<OpenLibraryDownload> CheckDataTypeAsync(DateTime datestamp, string type)
        {
            var validType = string.Equals(type, "authors", StringComparison.Ordinal) || string.Equals(type, "editions", StringComparison.Ordinal);
            if (!validType) throw new ArgumentException($"' {type} ' is not a valid open library download type");
        
            const string rootUrl = "https://archive.org/download";
            const string ext = "txt.gz";
            
            // https://archive.org/download/ol_dump_2021-03-19/ol_dump_editions_2021-03-19.txt.gz
            var formattedDate = datestamp.ToString("yyyy-MM-dd");
            var url = $"{rootUrl}/ol_dump_{formattedDate}/ol_dump_{type}_{formattedDate}.{ext}";
            
            _logger.LogInformation($"Checking {type}: {url}");
            var timer = Stopwatch.StartNew();
            
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                timer.Stop();
                _logger.LogInformation($"Checked {type} in {timer.ElapsedMilliseconds:N0}ms. New version found = {response.IsSuccessStatusCode}");
                return response.IsSuccessStatusCode
                    ? new OpenLibraryDownload
                    {
                        Source = url,
                        Datestamp = datestamp,
                        ArchiveType = OpenLibraryArchiveType.Authors,
                    }
                    : null;
            }
        }
    }
}