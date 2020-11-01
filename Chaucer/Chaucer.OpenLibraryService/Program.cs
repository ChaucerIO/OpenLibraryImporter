using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Chaucer.Common;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var authorFile = "ol_dump_authors_2020-05-31.txt.gz";
            var publicationsFile = "ol_dump_editions_2020-05-31.txt.gz";
            var downloads = Path.Combine("/", "Users", "rianjs", "Downloads");

            var gzAuthors = Path.Combine(downloads, authorFile);

            var complexAuthors = Path.Combine(downloads, "biggest-authors.txt");
            var brokenEntriesPath = Path.Combine(downloads, "broken-entries.txt");
            var lines = await File.ReadAllLinesAsync(complexAuthors);

            var authors = new List<ExpandedAuthor>();
            var broken = new List<string>();
            foreach (var line in lines)
            {
                try
                {
                    var a = JsonConvert.DeserializeObject<ExpandedAuthor>(line);
                    authors.Add(a);
                }
                catch (Exception)
                {
                    broken.Add(line);
                }
            }

            
            // File.WriteAllLines(brokenEntriesPath, broken);
            // var emptyBio = lines.Where(a => a.B)
            
            var fs = new Filesystem();
            // var fsProvider = new FilesystemOpenLibraryDataProvider(gzAuthors, Encoding.UTF8, fs);

            // var foo = await fsProvider.GetAuthorsAsync();
            
        }
    }
}