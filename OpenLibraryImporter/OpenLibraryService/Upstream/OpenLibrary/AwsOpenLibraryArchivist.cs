using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
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
        private readonly IStorageStreamer _storageStreamer;
        private readonly IAmazonS3 _s3;
        private readonly string _openLibVersionsBucket;
        private readonly IDynamoDBContext _pocoClient;
        private readonly string _openLibVersionsTable;
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly IClock _clock;
        private readonly ILogger<IOpenLibraryArchivist> _log;

        public AwsOpenLibraryArchivist(
            IStorageStreamer storageStreamer,
            IAmazonS3 s3Client,
            string openLibVersionsBucketName,
            IDynamoDBContext pocoClient,
            string dynamoOpenLibVersionsTable,
            IAmazonDynamoDB dynamoClient,
            IClock clock, ILogger<IOpenLibraryArchivist> log)
        {
            _storageStreamer = storageStreamer ?? throw new ArgumentNullException(nameof(storageStreamer));
            _s3 = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _openLibVersionsBucket = string.IsNullOrWhiteSpace(openLibVersionsBucketName)
                ? throw new ArgumentNullException(nameof(openLibVersionsBucketName))
                : openLibVersionsBucketName;
            _pocoClient = pocoClient ?? throw new ArgumentNullException(nameof(pocoClient));
            _openLibVersionsTable = string.IsNullOrWhiteSpace(dynamoOpenLibVersionsTable)
                ? throw new ArgumentNullException(nameof(dynamoOpenLibVersionsTable))
                : dynamoOpenLibVersionsTable;
            _dynamoClient = dynamoClient ?? throw new ArgumentNullException(nameof(dynamoClient));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _log = log ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<IReadOnlyCollection<OpenLibraryVersion>> FindArchiveEntries(
            DateTime searchStart,
            DateTime searchEnd,
            OpenLibraryArchiveType archiveType,
            CancellationToken ct)
        {
            if (searchEnd <= searchStart)
            {
                throw new ArgumentException($"Search start ({searchStart:O}) must come before search end ({searchEnd:O})");
            }

            var range = new object[] {searchStart, searchEnd};
            var query = _pocoClient.QueryAsync<OpenLibraryVersion>(archiveType.GetKey(), QueryOperator.Between, range);
            var results = await query.GetNextSetAsync(ct);
            return results;
        }

        public async Task<OpenLibraryVersion> FindLatestArchiveEntry(OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var attributeValuesMap = new Dictionary<string, AttributeValue>
            {
                {":kind", new AttributeValue {S = archiveType.GetKey()}},
                {":date", new AttributeValue {S = _clock.UtcNow().ToIsoDateString()}},
            };

            var queryReq = new QueryRequest
            {
                TableName = _openLibVersionsTable,
                KeyConditionExpression = "Kind = :kind AND PublishDate <= :date",
                ScanIndexForward = false,   // False = sort by descending
                ExpressionAttributeValues = attributeValuesMap,
                Limit = 1,
            };
            
            var queryResults = await _dynamoClient.QueryAsync(queryReq, ct);
            var theResult = queryResults.Items?.SingleOrDefault();
            if (theResult is null)
            {
                return null;
            }

            var doc = Document.FromAttributeMap(theResult);
            var typed = _pocoClient.FromDocument<OpenLibraryVersion>(doc);
            return typed;
        }

        public async Task<OpenLibraryVersion> SaveArchive(OpenLibraryDownload version, CancellationToken ct)
        {
            var matchingVersion = await FindArchiveEntries(version.Datestamp.AddDays(-1), version.Datestamp.AddDays(1), version.ArchiveType, ct);
            if (matchingVersion?.Any(v => v.PublishDate == version.Datestamp) == true)
            {
                throw new ArgumentException($"{version.ArchiveType.GetKey()} is already in the archives");
            }
            
            var transferReport = await _storageStreamer.StreamHttpToS3(version.Source, _openLibVersionsBucket, version.ObjectName, ct);
            var versionEntry = new OpenLibraryVersion
            {
                SourceUrl = version.Source,
                Kind = version.ArchiveType.GetKey(),
                ObjectName = version.ObjectName,
                Bytes = transferReport.Bytes,
                Uri = transferReport.DestinationUrl,
            };
            await SaveArchiveEntry(versionEntry, ct);

            return versionEntry;
        }

        public async Task SaveArchiveEntry(OpenLibraryVersion version, CancellationToken ct)
        {
            _log.LogInformation($"Writing OpenLib entry {version.ObjectName} to archive index");
            var timer = Stopwatch.StartNew();
            await _pocoClient.SaveAsync(version, ct);
            timer.Stop();
            _log.LogInformation($"Writing entry {version.ObjectName} saved to archive index in {timer.ElapsedMilliseconds:N0}ms");
        }

        public async Task<OpenLibraryVersion> GetArchiveEntry(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var q = await _pocoClient.LoadAsync<OpenLibraryVersion>(archiveType.GetKey(), date, ct);
            return q;
        }
        
        public Task<Stream> GetArchive(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var key = OpenLibraryDownloadExtensions.GetObjectName(date, archiveType);
            return _s3.GetObjectStreamAsync(_openLibVersionsBucket, key, additionalProperties: null, ct);
        }
    }
}