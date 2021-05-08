using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class OpenLibraryFeedManager
    {
        private readonly string _url;
        private readonly HttpClient _client;
        private readonly ILogger<OpenLibraryFeedManager> _logger;

        public OpenLibraryFeedManager(string url, HttpClient client, ILogger<OpenLibraryFeedManager> logger)
        {
            _url = string.IsNullOrWhiteSpace(url) ? throw new ArgumentNullException(nameof(url)) : url;
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<OpenLibraryDownload>> GetNewDownloadsAsync()
        {
            var dateStamps = await CheckFeedAsync();
        }

        private async Task<List<string>> CheckFeedAsync()
        {
            using (var response = await _client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var rss = await response.Content.ReadAsStringAsync();
                using (var sr = new StringReader(rss))
                using (var reader = XmlReader.Create(sr))
                {
                    var feed = SyndicationFeed.Load(reader);
                    var items = feed.Items
                        .Select(i => i.Title.Text)
                        .Where(t => t.StartsWith("ol_dump", StringComparison.OrdinalIgnoreCase))
                        .Select(t => ExtractDateStamp(t))
                        .OrderByDescending(t => t)
                        .ToList();
                    return items;
                }
            }
        }

        public async Task<List<OpenLibraryDownload>> GetDownloadsAsync(string dateStamp)
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
        /// Returns the date stamp (2021-04-12) from a string like ol_dump_2021-04-12
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private static string ExtractDateStamp(string title)
            => title.Split("_").Last();
        
        // ol_dump_redirects_2021-04-12.txt.gz
        // ol_dump_works_2021-04-12.txt.gz
        
        // Go find what's there...
        // https://archive.org/download/ol_dump_2021-03-19/ol_dump_editions_2021-03-19.txt.gz
        private const string _slug = "https://archive.org/download";
        private const string _ext = "txt.gz";

        private async Task<OpenLibraryDownload> CheckEditionsAsync(string dateStamp)
        {
            // ol_dump_editions_2021-04-12.txt.gz
            var url = $"{_slug}/ol_dump_{dateStamp}/ol_dump_editions_{dateStamp}.{_ext}";
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                return response.IsSuccessStatusCode
                    ? new OpenLibraryDownload
                    {
                        Url = url,
                        Timestamp = DateTime.Parse(dateStamp),
                        Type = OpenLibraryType.Editions,
                    }
                    : null;
            }
        }

        private async Task<OpenLibraryDownload> CheckAuthorsAsync(string dateStamp)
        {
            // ol_dump_authors_2021-04-12.txt.gz
            var url = $"{_slug}/ol_dump_{dateStamp}/ol_dump_authors_{dateStamp}.{_ext}";
            using (var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                return response.IsSuccessStatusCode
                    ? new OpenLibraryDownload
                    {
                        Url = url,
                        Timestamp = DateTime.Parse(dateStamp),
                        Type = OpenLibraryType.Authors,
                    }
                    : null;
            }
        }
    }
}