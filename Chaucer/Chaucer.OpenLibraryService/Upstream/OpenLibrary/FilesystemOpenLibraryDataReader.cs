using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaucer.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class FilesystemOpenLibraryDataReader //: IOpenLibraryDataReader
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly IFilesystem _fs;
        private readonly ILogger<IOpenLibraryDataManager> _logger;

        public FilesystemOpenLibraryDataReader(string uri, IFilesystem fs, JsonSerializerSettings jsonSettings, ILogger<IOpenLibraryDataManager> logger)
        {
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentNullException(nameof(uri));
            _dataDirectory = uri;

            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _jsonSettings = jsonSettings ?? throw new ArgumentNullException(nameof(jsonSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Author>> GetAuthorsAsync()
        {
            var timer = Stopwatch.StartNew();
            var compressedAuthors = await _fs.FileReadAllBytesAsync(_dataDirectory);
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
        
        public async Task<IReadOnlyList<OpenLibraryVersion>> StreamUpdatesToArchiveAsync(ICollection<OpenLibraryDownload> updates)
        {
            var downloads = updates
                .Select(u => new
                {
                    Path = GetRawArchiveFilePath(u),
                    Download = u,
                })
                .Select(async dl => await FetchAndPersistPayloadAsync(dl.Download, dl.Path))
                .ToList();
            await Task.WhenAll(downloads);

            var failures = downloads.Where(d => !d.IsCompletedSuccessfully).ToList();
            foreach (var f in failures)
            {
                _logger.LogError($"Download failed: {f.Exception.Flatten()}");
            }

            var successes = downloads
                .Where(d => d.IsCompletedSuccessfully)
                .Select(d => d.Result)
                .ToList();
            return successes;
        }
        
        private async Task<OpenLibraryVersion> FetchAndPersistPayloadAsync(OpenLibraryDownload dl, string path)
        {
            _logger.LogInformation($"Beginning stream of {dl.ArchiveType} download from {dl.Source} to {path}");
            
            long size;
            var timer = Stopwatch.StartNew();
            
            using (var response = await _client.GetAsync(dl.Source, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                size = long.Parse(response.Content.Headers.First(h => string.Equals(h.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)).Value.First());

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var dest = new FileStream(path, FileMode.Create))
                {
                    await responseStream.CopyToAsync(dest);
                }
            }
            
            timer.Stop();
            var mb = size / (1024 * 1024);
            var mbSec = mb / timer.Elapsed.TotalSeconds;
            _logger.LogInformation($"Completed streaming of {dl.ArchiveType} from {dl.Source} to {path} in {timer.Elapsed.TotalSeconds:N0} sec with approx {mb:N2} MB streamed ( {mbSec:N2} MB/sec)");
            return new OpenLibraryVersion
            {
                Download = dl,
                Bytes = size,
                Uri = path,
            };
        }
        
        private string GetRawArchiveFilePath(OpenLibraryDownload dl)
        {
            var type = dl.ArchiveType switch
            {
                OpenLibraryArchiveType.Authors => "authors",
                OpenLibraryArchiveType.Editions => "editions",
                _ => throw new ArgumentException($"{dl.ArchiveType} is not a supported Open Library download type")
            };
            
            return GetRawArchiveFilePath(type, dl.Datestamp);
        }

        private string GetRawArchiveFilePath(string type, DateTime datestamp)
            => GetRawArchiveFilePath(type, datestamp.ToString("yyyy-MM-dd"));
            
        private string GetRawArchiveFilePath(string type, string formattedDatestamp)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrWhiteSpace(formattedDatestamp)) throw new ArgumentNullException(nameof(formattedDatestamp));
            
            var fn = $"{formattedDatestamp}-{type}-orig.txt.gz";
            var path = Path.Combine(_dataDirectory, fn);
            return path;
        }
    }
}