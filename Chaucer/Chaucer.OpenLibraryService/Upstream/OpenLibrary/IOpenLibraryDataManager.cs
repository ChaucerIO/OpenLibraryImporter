using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// Chaucer's internal archives of the published, Open Library archives
    /// </summary>
    public interface IOpenLibraryDataManager
    {
        /// <summary>
        /// Returns the archived Open Library catalog versions available.
        /// </summary>
        /// <param name="from">Inclusive search start</param>
        /// <param name="to">Inclusive search end</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryDownload>> GetArchiveIndexAsync(DateTime from, DateTime to, CancellationToken ct);

        /// <summary>
        /// Returns the latest version of the Open Library catalog available in the archives
        /// </summary>
        /// <returns></returns>
        Task<Stream> GetLatestArchiveAsync(OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// Returns a specific version of the Open Library catalog version from the archives. The returned Stream MUST be disposed by the caller, otherwise
        /// you will have a memory leak.
        /// </summary>
        /// <param name="datestamp"></param>
        /// <param name="archiveType">Either "authors" or "editions"</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Stream> GetArchiveAsync(DateTime datestamp, OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryVersion> SaveArchiveAsync(OpenLibraryDownload update, CancellationToken ct);
        
        // /// <summary>
        // /// Streams the Open Library download to the target location. Typically an HTTP stream to filesystem stream or HTTP stream to object store stream.
        // /// </summary>
        // /// <param name="updates"></param>
        // /// <returns></returns>
        // Task<IReadOnlyList<OpenLibraryDownloadReport>> StreamUpdatesToArchiveAsync(ICollection<OpenLibraryDownload> updates);
        //
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="lastUpdate"></param>
        // /// <returns></returns>
        // Task<IReadOnlyCollection<OpenLibraryDownload>> CheckForUpdatesAsync(DateTime lastUpdate);
        //
        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="datestamp"></param>
        // /// <returns></returns>
        // Task<IReadOnlyCollection<OpenLibraryDownload>> GetOpenLibraryCatalogUpdateJobsAsync(DateTime datestamp);
    }
}