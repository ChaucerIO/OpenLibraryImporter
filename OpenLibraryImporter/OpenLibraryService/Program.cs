using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.S3;
using Commons;
using Commons.Aws;
using Commons.Aws.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenLibraryService.Upstream.OpenLibrary;

namespace OpenLibraryService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var ct = new CancellationToken();
            var encoding = Encoding.UTF8;
            var jsonSerializerSettings = GetJsonSerializerSettings();
            var devDir = Path.Combine("/", "Users", "rianjs", "dev");
            var dataDir = Path.Combine(devDir, "chaucer", "Chaucer", "data");
            var openLibraryAuthorFile = "ol_dump_authors_2021-03-19.txt.gz";
            var openLibraryPublicationsFile = "ol_dump_editions_2021-03-19.txt.gz";
            var gzAuthors = Path.Combine(dataDir, openLibraryAuthorFile);
            var fs = new Filesystem();
            
            var compressingRefreshingDnsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(120),
                AutomaticDecompression = DecompressionMethods.All,
            };
            var httpClient = new HttpClient(compressingRefreshingDnsHandler);

            const string rssUrl = "https://archive.org/services/collection-rss.php?collection=ol_exports";
            var openLibRssReader = new OpenLibraryRssReader(httpClient, rssUrl, GetLogger<IOpenLibraryCatalogReader>());
            const string archivesBucketName = "chaucer-openlib-versions";
            const string archivesTableName = archivesBucketName;
            
            var (credentials, regionEndpoint) = Config.GetAwsConfig(awsProfileName: "chaucer-tform", region: "us-east-2");
            var s3Client = new AmazonS3Client(credentials, regionEndpoint);
            
            var s3Streamer = new S3Streamer(httpClient, s3Client, chunkSizeBytes: 5 * 1024 * 1024, GetLogger<IStorageStreamer>());
            var openLibArchive = new AwsOpenLibraryArchivist(
                openLibRssReader,
                s3Streamer,
                s3Client,
                archivesBucketName,
                archivesTableName,
                new AmazonDynamoDBClient(credentials, regionEndpoint),
                new Clock(),
                GetLogger<IOpenLibraryArchivist>());

            await openLibArchive.Update(ct);
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