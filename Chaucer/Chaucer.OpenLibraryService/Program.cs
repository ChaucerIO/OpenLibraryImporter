using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Chaucer.Common;
using Chaucer.OpenLibraryService.Downstream;
using Chaucer.OpenLibraryService.Upstream;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;
using Microsoft.Extensions.Logging;
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
            var openLibraryAuthorFile = "ol_dump_authors_2021-03-19.txt.gz";
            var openLibraryPublicationsFile = "ol_dump_editions_2021-03-19.txt.gz";
            var gzAuthors = Path.Combine(dataDir, openLibraryAuthorFile);
            var fs = new Filesystem();
            // var openLibraryProvider = new FilesystemOpenOpenLibraryDataProvider(gzAuthors, fs, jsonSerializerSettings, GetLogger<IOpenLibraryDataProvider>());
            // var authors = await openLibraryProvider.GetAuthorsAsync();

            var chaucerProvider = new FilesystemPersistence(dataDir, jsonSerializerSettings, GetLogger<FilesystemPersistence>());
            // write the bytes
            var authorsRecord = "authors-2021-03-19";
            // await chaucerProvider.WriteAuthorsAsync(authors, authorsRecord);
            // read them back
            // var secondAuthors = await chaucerProvider.ReadAuthorsAsync(authorsRecord) as List<Author>;
            
            // Write down the JSON

            var compressingRefreshingDnsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(120),
                AutomaticDecompression = DecompressionMethods.All,
            };
            const string url = "https://archive.org/services/collection-rss.php?collection=ol_exports";
            var rssClient = new HttpClient(compressingRefreshingDnsHandler);
            var rssReader = new OpenLibraryFeedManager(url, rssClient, GetLogger<OpenLibraryFeedManager>());
            await rssReader.CheckFeedAsync();

        }
        
        private static JsonSerializerSettings GetJsonSerializerSettings()
        {
            #pragma warning disable 162

            #if DEBUG
            return GetDebugJsonSerializerSettings();
            #endif
            
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
        
        private static ILogger<T> GetLogger<T>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });

            var logger = loggerFactory.CreateLogger<T>();
            return logger;
        }
    }
}