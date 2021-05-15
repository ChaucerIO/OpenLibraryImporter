using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class S3OpenLibraryDataManager :
        IOpenLibraryDataManager
    {
        private readonly HttpClient _openLibraryClient;
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly IAmazonDynamoDB _dynamoClient;
        private readonly ILogger<IOpenLibraryDataManager> _logger;

        public S3OpenLibraryDataManager(HttpClient openLibraryClient, IAmazonS3 s3Client, string bucketName, IAmazonDynamoDB dynamoClient,
            ILogger<IOpenLibraryDataManager> logger)
        {
            _openLibraryClient = openLibraryClient ?? throw new ArgumentNullException(nameof(openLibraryClient));
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _bucketName = string.IsNullOrWhiteSpace(bucketName) ? throw new ArgumentNullException(nameof(bucketName)) : bucketName;
            _dynamoClient = dynamoClient ?? throw new ArgumentNullException(nameof(dynamoClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IReadOnlyCollection<OpenLibraryDownload>> GetArchiveIndexAsync(DateTime from, DateTime to)
        {
            // Query Dynamo for the range
            throw new NotImplementedException();
        }

        public Task<Stream> GetLatestArchiveAsync(OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            // Query Dynamo
            var key = "";
            return _s3Client.GetObjectStreamAsync(_bucketName, key, additionalProperties: null, ct);
        }

        public Task<Stream> GetArchiveAsync(DateTime datestamp, OpenLibraryArchiveType archiveType, CancellationToken ct)
        {
            var key = $"{datestamp:yyyy-MM-dd}-{archiveType.GetKey()}.json.gz";
            return _s3Client.GetObjectStreamAsync(_bucketName, key, additionalProperties: null, ct);
        }

        public async Task<OpenLibraryVersion> SaveArchiveAsync(OpenLibraryDownload dl, CancellationToken ct)
        {
            long size;
            long mB;

            var timer = Stopwatch.StartNew();
            using (var response = await _openLibraryClient.GetAsync(dl.Source, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                size = long.Parse(response.Content.Headers.First(h => string.Equals(h.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)).Value.First());
                mB = size / (1024 * 1024);
                var key = GetS3Key(dl);
                
                _logger.LogInformation($"Beginning {dl.ArchiveType} stream ({mB:N2}MB) from {dl.Source} to S3 {_bucketName}:{key}");

                using (var responseStream = await response.Content.ReadAsStreamAsync(ct))
                {
                    await _s3Client.UploadObjectFromStreamAsync(_bucketName, key, responseStream, additionalProperties: null, ct);
                }
                
                var mbSec = mB / timer.Elapsed.TotalSeconds;
                _logger.LogInformation($"Streamed {mB:N2}MB {dl.ArchiveType} archive from {dl.Source} to S3 {_bucketName}:{key} in {timer.Elapsed.TotalSeconds:N0} seconds ({mbSec:N2} MB/sec)");
                
                return new OpenLibraryVersion
                {
                    Bytes = mB,
                    Download = dl,
                    Uri = GetS3Url(_bucketName, key),
                };
            }
        }
        
        private static string GetS3Key(OpenLibraryDownload dl)
            => $"{dl.Datestamp:yyyy-MM-dd}-{dl.ArchiveType.GetKey()}.txt.gz";

        private static string GetS3Url(string bucketName, string filename)
            => $"s3://{bucketName}/{filename}";
    }
}