// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.IO.Compression;
// using System.Linq;
// using System.Net.Http;
// using System.Threading.Tasks;
// using Commons;
// using Microsoft.Extensions.Logging;
// using Newtonsoft.Json;
// using OpenLibraryService.Upstream.OpenLibrary;
//
// namespace OpenLibraryService.Downstream
// {
//     public class FilesystemArchivist :
//         IChaucerArchivist
//     {
//         private readonly string _dataDirectory;
//         private readonly JsonSerializerSettings _jsonSerializerSettings;
//         private readonly HttpClient _openLibraryClient;
//         private readonly IFilesystem _fs;
//         private readonly ILogger _logger;
//
//         public FilesystemArchivist(string dataDirectory, HttpClient openLibraryClient, IFilesystem fs, JsonSerializerSettings jsonSerializerSettings,
//             ILogger logger)
//         {
//             _dataDirectory = string.IsNullOrWhiteSpace(dataDirectory) ? throw new ArgumentNullException(nameof(dataDirectory)) : dataDirectory;
//             _openLibraryClient = openLibraryClient ?? throw new ArgumentNullException(nameof(openLibraryClient));
//             _fs = fs ?? throw new ArgumentNullException(nameof(fs));
//             _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
//             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//         }
//
//         public Task SaveAuthorsAsync(ICollection<Author> authors, DateTime datestamp)
//         {
//             var fullAuthorsPath = GetNormalizedFullPath("authors", datestamp);
//             var serializer = JsonSerializer.Create(_jsonSerializerSettings);
//             
//             _logger.LogInformation($"Serializing {authors.Count:N0} authors, and writing to {fullAuthorsPath}");
//
//             var timer = Stopwatch.StartNew();
//             using (var fs = _fs.FileCreate(fullAuthorsPath))
//             using (var gzipStream = new GZipStream(fs, CompressionLevel.Optimal, leaveOpen: true))
//             using (var sw = new StreamWriter(gzipStream))
//             using (var tw = new JsonTextWriter(sw))
//             {
//                 serializer.Serialize(tw, authors);
//             }
//             timer.Stop();
//             _logger.LogInformation($"Serialized and wrote {authors.Count:N0} authors to {fullAuthorsPath} in {timer.ElapsedMilliseconds:N0}ms");
//
//             return Task.CompletedTask;
//         }
//
//         public Task<IReadOnlyCollection<Author>> GetAuthorsAsync(DateTime datestamp)
//         {
//             var fullPath = GetNormalizedFullPath("authors", datestamp);
//             var serializer = JsonSerializer.Create(_jsonSerializerSettings);
//
//             _logger.LogInformation($"Reading authors from {fullPath}");
//
//             var timer = Stopwatch.StartNew();
//
//             List<Author> result;
//             
//             using (var fs = _fs.FileOpenRead(fullPath))
//             using (var gzipStream = new GZipStream(fs, CompressionMode.Decompress))
//             using (var textReader = new StreamReader(gzipStream))
//             using (var jsonReader = new JsonTextReader(textReader))
//             {
//                 result = serializer.Deserialize<List<Author>>(jsonReader);
//             }
//             
//             timer.Stop();
//             _logger.LogInformation($"Deserialized {result.Count:N0} authors from {fullPath} in {timer.ElapsedMilliseconds:N0}ms");
//             
//             return Task.FromResult((IReadOnlyCollection<Author>)result);
//         }
//
//         
//
//         private string GetNormalizedFullPath(string type, DateTime datestamp)
//             => GetNormalizedFullPath(type, datestamp.ToString("yyyy-MM-dd"));
//
//         private string GetNormalizedFullPath(string type, string formattedDatestamp)
//         {
//             if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
//             if (string.IsNullOrWhiteSpace(formattedDatestamp)) throw new ArgumentNullException(nameof(formattedDatestamp));
//
//             var fn = $"{formattedDatestamp}-{type}.json.gz";
//             var path = Path.Combine(_dataDirectory, fn);
//             return path;
//         }
//
//         
//
//         
//         
//         public Task<IReadOnlyCollection<Author>> NormalizeAuthorsAsync(DateTime datestamp)
//         {
//             var rawAuthorsPath = GetRawArchiveFilePath("authors", datestamp);
//             var serializer = JsonSerializer.Create(_jsonSerializerSettings);
//             
//             List<Author> authors;
//             
//             using (var fs = _fs.FileOpenRead(rawAuthorsPath))
//             using (var gzipStream = new GZipStream(fs, CompressionMode.Decompress))
//             using (var textReader = new StreamReader(gzipStream))
//             using (var jsonReader = new JsonTextReader(textReader))
//             {
//                 authors = serializer.Deserialize<List<Author>>(jsonReader);
//             }
//
//             return Task.FromResult((IReadOnlyCollection<Author>) authors);
//         }
//         
//         public Task<DateTime> GetLastAuthorsDatestampAsync()
//             => Task.FromResult(FindRecords("authors").FirstOrDefault());
//
//         public Task<IReadOnlyCollection<DateTime>> GetAuthorsDatestampsAsync()
//             => Task.FromResult((IReadOnlyCollection<DateTime>)FindRecords("authors"));
//
//         public Task<DateTime> GetLastEditionsDatestampAsync()
//             => Task.FromResult(FindRecords("editions").FirstOrDefault());
//
//         public Task<IReadOnlyCollection<DateTime>> GetEditionsDatestampsAsync()
//             => Task.FromResult((IReadOnlyCollection<DateTime>)FindRecords("editions"));
//
//         private List<DateTime> FindRecords(string type)
//         {
//             var searchPattern = $"*-{type}-orig.txt.gz";
//
//             var matches = _fs.DirectoryGetFiles(_dataDirectory, searchPattern, SearchOption.TopDirectoryOnly)
//                 .Select(fullPath => Path.GetFileNameWithoutExtension(fullPath))
//                 .Select(fn => fn.Substring(0, 10)) // yyyy-MM-dd-type => yyyy-MM-dd
//                 .Select(fn => DateTime.Parse(fn))
//                 .OrderByDescending(date => date)
//                 .ToList();
//             
//             return matches;
//         }
//     }
// }