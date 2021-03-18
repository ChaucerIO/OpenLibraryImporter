using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Chaucer.Common;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class FilesystemOpenLibraryDataProvider :
        ILibraryDataProvider
    {
        private readonly string _path;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly Encoding _textEncoding;
        private readonly IFilesystem _fs;

        public FilesystemOpenLibraryDataProvider(string uri, Encoding textEncoding, IFilesystem fs, JsonSerializerSettings jsonSettings)
        {
            if (string.IsNullOrWhiteSpace(uri)) throw new ArgumentNullException(nameof(uri));
            _path = uri;

            _textEncoding = textEncoding ?? throw new ArgumentNullException(nameof(textEncoding));
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _jsonSettings = jsonSettings ?? throw new ArgumentNullException(nameof(jsonSettings));
        }

        public async Task<List<Author>> GetAuthorsAsync()
        {
            var compressedAuthors = await _fs.FileReadAllBytesAsync(_path);
            var authorBlobs = await Compression.FromGzippedStringAsync(compressedAuthors).ToListAsync();

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