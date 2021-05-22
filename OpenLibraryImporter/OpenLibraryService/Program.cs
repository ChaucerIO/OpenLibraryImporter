using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;
using Commons;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenLibraryService.Downstream;
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

            // var fsDataManager = new FilesystemArchivist(dataDir, httpClient, fs, jsonSerializerSettings, GetLogger<FilesystemArchivist>());

            // var editions = await fsDataManager.GetAuthorsDatestampsAsync();
            // var authors = await fsDataManager.GetEditionsDatestampsAsync();
            // var lastRecordTime = editions.FirstOrDefault() >= authors.FirstOrDefault()
            //     ? editions.FirstOrDefault()
            //     : authors.FirstOrDefault();

            const string rssUrl = "https://archive.org/services/collection-rss.php?collection=ol_exports";
            var openLibRssReader = new OpenLibraryRssReader(httpClient, rssUrl, GetLogger<IOpenLibraryCatalogReader>());
            const string archivesBucketName = "chaucer-openlib-versions";
            const string archivesTableName = archivesBucketName;
            
            var (credentials, regionEndpoint) = GetAwsConfig();
            var openLibArchive = new AwsOpenLibraryArchivist(
                openLibRssReader,
                httpClient,
                new AmazonS3Client(credentials, regionEndpoint),
                archivesBucketName,
                archivesTableName,
                new AmazonDynamoDBClient(credentials, regionEndpoint),
                new Clock(),
                GetLogger<IOpenLibraryArchivist>());

            await openLibArchive.Update(ct);
            
            // var newVersions = await openLibArchive.GetOpenLibCatalogFeed(DateTime.Now.AddDays(-30), ct);
            // var foo = openLibArchive.
            
            
            //https://rianjs.net//big/openlib/2021-04-12-authors-orig.txt.gz
            var dl = new OpenLibraryDownload
            {
                Datestamp = DateTime.Parse("2021-03-12"),
                Source = "https://rianjs.net//big/openlib/2021-04-12-authors-orig.txt.gz",
                ArchiveType = OpenLibraryArchiveType.Authors,
            };
            
            // var saveOp = await openLibArchive.SaveArchiveAsync(dl, CancellationToken.None);

            // var openLibraryMgr = new HttpToFilesystemOpenLibraryDataManager(httpClient, fs, GetLogger<HttpToFilesystemOpenLibraryDataManager>());
            // var updates = await openLibraryMgr.CheckForUpdatesAsync(lastRecordTime);
            // if (!updates.Any())
            // {
            //     return;
            // }
            
            // var updateTasks = await fsDataManager.StreamUpdatesToArchiveAsync(updates);

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
        
        private static (AWSCredentials credentials, RegionEndpoint regionEndpoint) GetAwsConfig(string awsProfileName = "chaucer-tform", string region = "us-east-2")
        {
            var credProfileStoreChain = new CredentialProfileStoreChain();
            if (credProfileStoreChain.TryGetAWSCredentials(awsProfileName, out var awsCredentials))
            {
                // Console.WriteLine("Access Key: " + awsCredentials.GetCredentials().AccessKey);
                // Console.WriteLine("Secret Key: " + awsCredentials.GetCredentials().SecretKey);
                return (awsCredentials, RegionEndpoint.GetBySystemName("us-east-2"));
            }

            throw new ArgumentException($"{awsProfileName} was not a profile available in the credentials store");
        }
    }
}