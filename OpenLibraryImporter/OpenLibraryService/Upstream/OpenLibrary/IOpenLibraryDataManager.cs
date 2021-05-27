using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// Represents the Open Library seek-and-archive activities. There are no data-normalization operations here -- just read from the Open Lib systems of
    /// record, and write to Chaucer's internal archives. We do this because we want to be good citizens, and because the Open Library endpoints are incredibly
    /// slow.
    /// </summary>
    public interface IOpenLibraryArchivist
    {
        /// <summary>
        /// Checks the Open Library for recent updates. If any are found, the updates are downloaded, and added to the internal Chaucer archive.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task Update(CancellationToken ct);
        
        /// <summary>
        /// Finds the information about archive versions saved to the internal archives published during the specified time range, of the specified type
        /// </summary>
        /// <param name="searchStart"></param>
        /// <param name="searchEnd"></param>
        /// <param name="archiveTypes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryVersion>> FindArchiveEntries(
            DateTime searchStart,
            DateTime searchEnd,
            ICollection<OpenLibraryArchiveType> archiveTypes,
            CancellationToken ct);

        /// <summary>
        /// Finds the inforation about the latest archive versions saved to the internal archives data store, for the specified type
        /// </summary>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> FindLatestArchiveEntry(OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// Saves the archive to the data store, including any secondary indexing stores that aid with storage and retrieval of archive metadata
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> SaveArchive(OpenLibraryDownload archive, CancellationToken ct);

        /// <summary>
        /// Gets the archive
        /// </summary>
        /// <param name="date"></param>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Stream> GetArchive(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct);
    }
}