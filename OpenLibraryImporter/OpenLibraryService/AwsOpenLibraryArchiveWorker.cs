using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commons;
using Microsoft.Extensions.Logging;
using OpenLibraryService.Upstream.OpenLibrary;

namespace OpenLibraryService
{
    /// <summary>
    /// Checks the Open Library for recent updates. If any are found, the updates are downloaded, and added to the internal Chaucer archives.
    /// </summary>
    /// <returns></returns>
    public class AwsOpenLibraryArchiveWorker :
        IWorker
    {
        private readonly IOpenLibraryArchivist _archivist;
        private readonly IOpenLibraryCatalogReader _rssReader;
        private readonly HashSet<OpenLibraryArchiveType> _knownArchiveTypes;
        private readonly ILogger<AwsOpenLibraryArchiveWorker> _logger;

        public AwsOpenLibraryArchiveWorker(
            IOpenLibraryArchivist archivist,
            IOpenLibraryCatalogReader openLibCatalogReader,
            IEnumerable<OpenLibraryArchiveType> knownArchiveTypes,
            ILogger<AwsOpenLibraryArchiveWorker> logger)
        {
            _archivist = archivist ?? throw new ArgumentNullException(nameof(archivist));
            _rssReader = openLibCatalogReader ?? throw new ArgumentNullException(nameof(openLibCatalogReader));
            _knownArchiveTypes = knownArchiveTypes is null || !knownArchiveTypes.Any()
                ? throw new ArgumentNullException(nameof(knownArchiveTypes))
                : knownArchiveTypes.ToHashSet();
            _logger = logger;
        }

        public async Task DoWorkAsync(CancellationToken ct)
        {
            // The OpenLibrary's servers are quite slow, so we'll kick off this IO and then come back to it later
            var latestUpstreamVersionTask = _rssReader.GetLatestVersionForTypes(_knownArchiveTypes, ct);
            _logger.LogInformation("Checking the upstream Open Library feed to see is there are newer versions for any of the archive types");
            
            _logger.LogInformation("Checking internal archives for most recent entry");
            var timer = Stopwatch.StartNew();
            var knownArchiveTypeTasks = _knownArchiveTypes
                .Select(at => _archivist.FindLatestArchiveEntry(at, ct))
                .ToList();
            await Task.WhenAll(knownArchiveTypeTasks);
            timer.Stop();

            foreach (var failure in knownArchiveTypeTasks.Where(t => !t.IsCompletedSuccessfully))
            {
                _logger.LogError(failure.Exception?.Flatten(), "Failed looking for the latest archive type");
            }

            var previousArchives = knownArchiveTypeTasks
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .Where(r => r is not null)
                .ToDictionary(r => r.Kind, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation($"Found previous archives for {previousArchives.Count:N0} archive types in {timer.ElapsedMilliseconds:N0}ms");

            var latestUpstreamVersions = await latestUpstreamVersionTask;
            if (!latestUpstreamVersionTask.IsCompletedSuccessfully)
            {
                _logger.LogError(latestUpstreamVersionTask.Exception?.Flatten(),"Unable to check upstream for the latest open library versions. Exiting work loop.");
                return;
            }

            var toUpdate = latestUpstreamVersions
                .Where(latest => !previousArchives.TryGetValue(latest.ArchiveType.GetKey(), out var internalVersion)
                                 || internalVersion.PublishDate < latest.Datestamp)
                .ToList();

            if (!toUpdate.Any())
            {
                _logger.LogInformation("The internal Open Library archives are up to date!");
                return;
            }
            
            _logger.LogInformation($"Downloading {toUpdate.Count} versions from the Open Library");
            timer = Stopwatch.StartNew();
            var updateTasks = toUpdate
                .Select(u => _archivist.SaveArchive(u, ct))
                .ToList();
            await Task.WhenAll(updateTasks);
            timer.Stop();
            _logger.LogInformation($"Downloaded {toUpdate.Count} versions from the Open Library in {timer.Elapsed.TotalSeconds} secs");
        }
    }
}