using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Commons;
using Commons.Aws.Storage;
using Microsoft.Extensions.Logging;
using OpenLibraryService.Utilities;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class AwsOpenLibraryArchivist :
        IOpenLibraryArchivist
    {
        private readonly IOpenLibraryCatalogReader _openLibRssReader;
        private readonly IStorageStreamer _storageStreamer;
        private readonly IAmazonS3 _s3;
        private readonly string _openLibVersionsBucket;
        private readonly string _dynamoVersionsTable;
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly IClock _clock;
        private readonly ILogger<IOpenLibraryArchivist> _logger;

        public AwsOpenLibraryArchivist(
            IOpenLibraryCatalogReader openLibRssReader,
            IStorageStreamer storageStreamer,
            IAmazonS3 s3Client,
            string openLibVersionsBucketName,
            string dynamoOpenLibVersionsTable,
            IAmazonDynamoDB dynamoClient,
            IClock clock,
            ILogger<IOpenLibraryArchivist> logger)
        {
            _openLibRssReader = openLibRssReader ?? throw new ArgumentNullException(nameof(openLibRssReader));
            _storageStreamer = storageStreamer ?? throw new ArgumentNullException(nameof(storageStreamer));
            _s3 = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _openLibVersionsBucket = string.IsNullOrWhiteSpace(openLibVersionsBucketName)
                ? throw new ArgumentNullException(nameof(openLibVersionsBucketName))
                : openLibVersionsBucketName;
            _dynamoVersionsTable = string.IsNullOrWhiteSpace(dynamoOpenLibVersionsTable)
                ? throw new ArgumentNullException(nameof(dynamoOpenLibVersionsTable))
                : dynamoOpenLibVersionsTable;
            _dynamoClient = dynamoClient ?? throw new ArgumentNullException(nameof(dynamoClient));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Update(CancellationToken ct)
        {
            _logger.LogInformation("Checking archives for most recent entry");
            var timer = Stopwatch.StartNew();
            var lastAuthors = FindLatestArchiveEntry(OpenLibraryArchiveType.Authors, ct);
            var lastEditions = FindLatestArchiveEntry(OpenLibraryArchiveType.Editions, ct);
            await Task.WhenAll(lastAuthors, lastAuthors);
            timer.Stop();

            var latestAuthor = lastAuthors.Result?.Download?.Datestamp ?? DateTime.MinValue;
            var latestEditions = lastEditions.Result?.Download?.Datestamp ?? DateTime.MinValue;
            var lastTimestamp = latestAuthor >= latestEditions
                ? latestAuthor
                : latestEditions;
            _logger.LogInformation($"Latest internal version is {lastTimestamp.ToIsoDateString()}. Found in {timer.ElapsedMilliseconds:N0}ms");

            _logger.LogInformation($"Checking the Open Library feed to see is there are newer versions than {lastTimestamp.ToIsoDateString()}");
            timer = Stopwatch.StartNew();
            var openLibCatalogTimestamps = await _openLibRssReader.GetCatalogDatestampsAsync(ct);
            var newestOpenLibVersion = openLibCatalogTimestamps
                .OrderByDescending(d => d)
                .FirstOrDefault();
            timer.Stop();
            _logger.LogInformation($"Found the most recent version of the Open Library data ({newestOpenLibVersion.ToIsoDateString()}), in {timer.ElapsedMilliseconds:N0}ms");
            
            if (newestOpenLibVersion <= lastTimestamp)
            {
                _logger.LogInformation("Internal archives up to date!");
                return;
            }

            _logger.LogInformation("Checking the Open Library timestamps to see which updates are required");
            timer = Stopwatch.StartNew();
            var getVersionsTasks = openLibCatalogTimestamps
                .Where(t => t > lastTimestamp)
                .Distinct()
                .Select(t => _openLibRssReader.GetDownloadsForVersionAsync(t, ct))
                .ToList();
            await Task.WhenAll(getVersionsTasks);
            timer.Stop();
            _logger.LogInformation($"Version update check finished in {timer.ElapsedMilliseconds:N0}");

            if (getVersionsTasks.Any(t => t.IsFaulted))
            {
                _logger.LogError("One or more version update checks failed");
                throw new AggregateException(getVersionsTasks.Where(t => t.IsFaulted).Select(f => f.Exception));
            }

            var versionsRequired = getVersionsTasks
                .SelectMany(t => t.Result)
                .OrderByDescending(d => d.Datestamp)
                .ToList();
            var topAuthor = versionsRequired.FirstOrDefault(v => v.ArchiveType == OpenLibraryArchiveType.Authors);
            var topEdition = versionsRequired.FirstOrDefault(v => v.ArchiveType == OpenLibraryArchiveType.Editions);
            _logger.LogInformation($"{versionsRequired.Count} version updates found");
            
            _logger.LogInformation($"Downloading {versionsRequired.Count} versions from the Open Library");
            timer = Stopwatch.StartNew();
            var updateTasks = new[] { topAuthor, topEdition }
                .Select(u => SaveArchive(u, ct))
                .ToList();
            await Task.WhenAll(updateTasks);
            timer.Stop();
            _logger.LogInformation($"Downloaded {versionsRequired.Count} versions from the Open Library in {timer.Elapsed.TotalSeconds} secs");
        }

        public async Task<IReadOnlyCollection<OpenLibraryVersion>> FindArchiveEntries(
            DateTime searchStart,
            DateTime searchEnd,
            ICollection<OpenLibraryArchiveType> archiveTypes,
            CancellationToken ct)
        {
            if (searchEnd <= searchStart)
            {
                throw new ArgumentException($"Search start ({searchStart:O}) must come before search end ({searchEnd:O})");
            }
            
            var attributeValuesMap = new Dictionary<string, AttributeValue>
            {
                {":type", new AttributeValue {S = "authors"}},
                {":from", new AttributeValue {S = searchStart.ToIsoDateString()}},
                {":to", new AttributeValue {S = searchEnd.ToIsoDateString()}},
            };

            var query = new QueryRequest
            {
                TableName = _dynamoVersionsTable,
                KeyConditionExpression = "Type = :type AND Version BETWEEN :from AND :to",
                ExpressionAttributeValues = attributeValuesMap,
            };

            var results = await _dynamoClient.QueryAsync(query, ct);
            if (results is null || results.Items.Any() == false)
            {
                return new List<OpenLibraryVersion>();
            }
            
            return new List<OpenLibraryVersion>();
        }

        public async Task<OpenLibraryVersion> FindLatestArchiveEntry(OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var attributeValuesMap = new Dictionary<string, AttributeValue>
            {
                {":kind", new AttributeValue {S = archiveType.GetKey()}},
                {":date", new AttributeValue {S = _clock.UtcNow().ToIsoDateString()}},
            };

            var query = new QueryRequest
            {
                TableName = _dynamoVersionsTable,
                KeyConditionExpression = "Kind = :kind AND PublishDate <= :date",
                ScanIndexForward = false,   // False = sort by descending
                ExpressionAttributeValues = attributeValuesMap,
                Limit = 1,
            };
            
            var results = await _dynamoClient.QueryAsync(query, ct);
            if (results is null || results.Items.Any() == false)
            {
                return null;
            }
            
            return null;
        }

        public async Task<OpenLibraryVersion> SaveArchive(OpenLibraryDownload version, CancellationToken ct)
        {
            var  transferReport = await _storageStreamer.StreamHttpToS3(version.Source, _openLibVersionsBucket, version.ObjectName, ct);
            return new OpenLibraryVersion
            {
                Bytes = transferReport.Bytes,
                Download = version,
                Uri = transferReport.DestinationUrl,
            };
        }
        
        public Task<Stream> GetArchive(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var key = OpenLibraryDownloadExtensions.GetObjectName(date, archiveType);
            return _s3.GetObjectStreamAsync(_openLibVersionsBucket, key, additionalProperties: null, ct);
        }
    }
}