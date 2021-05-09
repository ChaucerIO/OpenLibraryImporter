using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaucer.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class FilesystemOpenLibraryDataProvider :
        IOpenLibraryDataProvider
    {
        private readonly string _path;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly IFilesystem _fs;
        private readonly ILogger<IOpenLibraryDataProvider> _logger;

        public FilesystemOpenLibraryDataProvider(string uri, IFilesystem fs, JsonSerializerSettings jsonSettings, ILogger<IOpenLibraryDataProvider> logger)
        {
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentNullException(nameof(uri));
            _path = uri;

            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _jsonSettings = jsonSettings ?? throw new ArgumentNullException(nameof(jsonSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Author>> GetAuthorsAsync()
        {
            var timer = Stopwatch.StartNew();
            var compressedAuthors = await _fs.FileReadAllBytesAsync(_path);
            _logger.LogInformation($"{timer.ElapsedMilliseconds:N0}ms to read gzip into memory");

            timer = Stopwatch.StartNew();
            var authorBlobs = await Compression.FromGzippedStringAsync(compressedAuthors).ToListAsync();
            _logger.LogInformation($"{timer.ElapsedMilliseconds:N0}ms to decompress");

            var authors = authorBlobs
                .AsParallel()
                .Select(ta => ta.Split("\t").LastOrDefault())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(a => JsonConvert.DeserializeObject<Author>(a, _jsonSettings))
                .ToList();

            return authors;
        }

        private async Task<string> GetGzippedString(string path, Encoding textEncoding)
        {
            var tabbedContents = await _fs.FileReadAllBytesAsync(path);
            return textEncoding.GetString(tabbedContents);
        }

        public async Task<string> GetGzippedAuthorDataAsync(string path, Encoding textEncoding)
            => await GetGzippedString(path, textEncoding);

        public IEnumerable<string> ExtractAuthorJson(string tabbedContents)
            => GetColumn(tabbedContents, "\t", 5);

        public async Task<string> GetGzippedTitleJsonAsync(string path, Encoding encoding)
            => await GetGzippedString(path, encoding);

        public IEnumerable<string> ExtractTitleJson(string tabbedContents)
            => GetColumn(tabbedContents, "\t", 5);

        private IEnumerable<string> GetColumn(string rawText, string separator, int columnNumber)
        {
            var lines = rawText.Split(new[] {Environment.NewLine, "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var columns = line.Split(separator);
                var json = columns[columnNumber];
                yield return json;
            }
        }
    }
}