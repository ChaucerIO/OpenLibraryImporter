using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Downstream
{
    public class FilesystemPersistence :
        IAuthorManager
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly ILogger _logger;

        public FilesystemPersistence(string dataDirectory, JsonSerializerSettings jsonSerializerSettings, ILogger logger)
        {
            _dataDirectory = string.IsNullOrWhiteSpace(dataDirectory) ? throw new ArgumentNullException(nameof(dataDirectory)) : dataDirectory; 
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task WriteAuthorsAsync(ICollection<Author> authors, string recordName)
        {
            if (string.IsNullOrWhiteSpace(recordName)) throw new ArgumentNullException(recordName);

            var fullAuthorsPath = GetFullPath(recordName);
            var serializer = JsonSerializer.Create(_jsonSerializerSettings);
            
            _logger.LogInformation($"Serializing {authors.Count:N0} authors, and writing to {fullAuthorsPath}");

            var timer = Stopwatch.StartNew();
            using (var fs = File.Create(fullAuthorsPath))
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

        public Task<ICollection<Author>> ReadAuthorsAsync(string recordName)
        {
            var fullPath = string.IsNullOrWhiteSpace(recordName) ? throw new ArgumentNullException(nameof(recordName)) : GetFullPath(recordName);
            var serializer = JsonSerializer.Create(_jsonSerializerSettings);

            _logger.LogInformation($"Reading authors from {fullPath}");

            var timer = Stopwatch.StartNew();

            ICollection<Author> result;
            
            using (var fs = File.OpenRead(fullPath))
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

        private string GetFullPath(string recordName)
            => Path.Combine(_dataDirectory, $"{recordName}.json.gz");
    }
}