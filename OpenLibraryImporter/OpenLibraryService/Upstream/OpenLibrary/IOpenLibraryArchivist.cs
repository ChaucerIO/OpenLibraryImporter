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
        /// Saves the archive to the data store, and writes the entry metadata to the archive index.
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> SaveArchive(OpenLibraryDownload archive, CancellationToken ct);
        
        /// <summary>
        /// Gets the specified archive.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException">Throws if the archive cannot be found</exception>
        Task<Stream> GetArchive(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// Queries the archive index for metadata entries between start and end dates for the desired archive types. Returns an empty list if no matches are
        /// found.
        /// </summary>
        /// <param name="searchStart">Inclusive search start</param>
        /// <param name="searchEnd">Inclusive search end</param>
        /// <param name="archiveTypes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryVersion>> FindArchiveEntries(
            DateTime searchStart,
            DateTime searchEnd,
            OpenLibraryArchiveType archiveTypes,
            CancellationToken ct);

        /// <summary>
        /// Returns the archive metadata about the latest version of the specified archive type by querying the archive index. Returns null if nothing is found.
        /// </summary>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> FindLatestArchiveEntry(OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// Saves the archive metadata to the archive index
        /// </summary>
        /// <param name="version"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task SaveArchiveEntry(OpenLibraryVersion version, CancellationToken ct);

        /// <summary>
        /// Returns the archive metadata associated with the date + desired archive type from the archive index. Returns null if nothing is found.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> GetArchiveEntry(DateTime date, OpenLibraryArchiveType archiveType, CancellationToken ct);
    }
}