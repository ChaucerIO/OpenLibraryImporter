using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chaucer.Common;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Downstream
{
    public class FilesystemPersistence :
        IAuthorManager,
        IOpenLibraryArchivist
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly HttpClient _openLibraryClient;
        private readonly IFilesystem _fs;
        private readonly ILogger _logger;

        public FilesystemPersistence(string dataDirectory, HttpClient openLibraryClient, IFilesystem fs, JsonSerializerSettings jsonSerializerSettings,
            ILogger logger)
        {
            _dataDirectory = string.IsNullOrWhiteSpace(dataDirectory) ? throw new ArgumentNullException(nameof(dataDirectory)) : dataDirectory;
            _openLibraryClient = openLibraryClient ?? throw new ArgumentNullException(nameof(openLibraryClient));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SaveAuthorsAsync(ICollection<Author> authors, DateTime datestamp)
        {
            var fullAuthorsPath = GetFullPath(datestamp);
            var serializer = JsonSerializer.Create(_jsonSerializerSettings);
            
            _logger.LogInformation($"Serializing {authors.Count:N0} authors, and writing to {fullAuthorsPath}");

            var timer = Stopwatch.StartNew();
            using (var fs = _fs.FileCreate(fullAuthorsPath))
            using (var gzipStream = new GZipStream(fs, CompressionLevel.Optimal, leaveOpen: true))
            using (var sw = new StreamWriter(gzipStream))
            using (var tw = new JsonTextWriter(sw))
            {
                serializer.Serialize(tw, authors);
            }
            timer.Stop();
            _logger.LogInformation($"Serialized and wrote {authors.Count:N0} authors to {fullAuthorsPath} in {timer.ElapsedMilliseconds:N0}ms");

            return Task.CompletedTask;
        }

        public Task<ICollection<Author>> GetAuthorsAsync(DateTime datestamp)
        {
            var fullPath = GetFullPath(datestamp);
            var serializer = JsonSerializer.Create(_jsonSerializerSettings);

            _logger.LogInformation($"Reading authors from {fullPath}");

            var timer = Stopwatch.StartNew();

            ICollection<Author> result;
            
            using (var fs = _fs.FileOpenRead(fullPath))
            using (var gzipStream = new GZipStream(fs, CompressionMode.Decompress))
            using (var textReader = new StreamReader(gzipStream))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                result = serializer.Deserialize<List<Author>>(jsonReader);
            }
            
            timer.Stop();
            _logger.LogInformation($"Deserialized {result.Count:N0} authors from {fullPath} in {timer.ElapsedMilliseconds:N0}ms");
            return Task.FromResult(result);
        }

        private string GetRawArchiveFilePath(OpenLibraryDownload dl)
        {
            var type = dl.Type switch
            {
                OpenLibraryType.Authors => "authors",
                OpenLibraryType.Editions => "editions",
                _ => throw new ArgumentException($"{dl.Type} is not a supported Open Library download type")
            };
            
            var fn = $"{dl.Datestamp:yyyy-MM-dd}-{type}-orig.txt.gz";
            var path = Path.Combine(_dataDirectory, fn);
            return path;
        }

        private string GetFullPath(DateTime datestamp)
            => Path.Combine(_dataDirectory, $"{datestamp:yyyy-MM-dd}.json.gz");
        
        public async Task<IReadOnlyList<OpenLibraryDownloadReport>> StreamUpdatesToArchiveAsync(ICollection<OpenLibraryDownload> updates)
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

        private async Task<OpenLibraryDownloadReport> FetchAndPersistPayloadAsync(OpenLibraryDownload dl, string path)
        {
            _logger.LogInformation($"Beginning stream of {dl.Type} download from {dl.Url} to {path}");
            
            long size;
            var timer = Stopwatch.StartNew();
            
            using (var response = await _openLibraryClient.GetAsync(dl.Url, HttpCompletionOption.ResponseHeadersRead))
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
            _logger.LogInformation($"Completed streaming of {dl.Type} from {dl.Url} to {path} in {timer.Elapsed.TotalSeconds:N0} sec with approx {mb:N2} MB streamed ( {mbSec:N2} MB/sec)");
            return new OpenLibraryDownloadReport
            {
                Download = dl,
                Bytes = size,
                Uri = path,
            };
        }
        
        public Task<IReadOnlyList<Author>> NormalizeAuthorsAsync(DateTime datestamp)
        {
            // Read from the raw file path
            // Normalize, deserialize, write to the new one
            
            throw new NotImplementedException();
        }
    }
}