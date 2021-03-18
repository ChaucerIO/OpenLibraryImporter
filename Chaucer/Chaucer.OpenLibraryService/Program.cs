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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Chaucer.OpenLibraryService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var encoding = Encoding.UTF8;
            var jsonSerializerSettings = GetJsonSerializerSettings();
            var devDir = Path.Combine("/", "Users", "rianjs", "dev");
            var dataDir = Path.Combine(devDir, "chaucer", "Chaucer", "data");
            var authorFile = "ol_dump_authors_2021-02-28.txt.gz";
            var publicationsFile = "ol_dump_editions_2021-02-28.txt.gz";
            var gzAuthors = Path.Combine(dataDir, authorFile);

            var fs = new Filesystem();
            var fsProvider = new FilesystemOpenLibraryDataProvider(gzAuthors, encoding, fs, jsonSerializerSettings);

            var authors = await fsProvider.GetAuthorsAsync();
            
            // Write down the JSON
            var authorsJsonFile = "authors-2021-02-28.json";
            var fullAuthorsPath = Path.Combine(dataDir, authorsJsonFile);
            var serializedAuthors = JsonConvert.SerializeObject(authors, jsonSerializerSettings);
            fs.FileWriteAllText(fullAuthorsPath, serializedAuthors, encoding);

        }
        
        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            #if DEBUG
            return GetDebugJsonSerializerSettings();
            #endif
            
            #pragma warning disable 162
            return GetProdJsonSerializerSettings();
            #pragma warning restore 162
        }
        
        private static JsonSerializerSettings GetDebugJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), },
            };
        }

        private static JsonSerializerSettings GetProdJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), },
            };
        }
    }
}