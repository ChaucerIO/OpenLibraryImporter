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

            var fs = new Filesystem();
            var fsProvider = new FilesystemOpenLibraryDataProvider(gzAuthors, Encoding.UTF8, fs);

            var foo = await fsProvider.GetAuthorsAsync();

        }
    }
}