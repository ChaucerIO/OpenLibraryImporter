using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public interface IOpenLibraryCatalogReader
    {
        /// <summary>
        /// Returns the versions available for download for the specified type.
        /// </summary>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryDownload>> GetDownloadsForType(OpenLibraryArchiveType archiveType, CancellationToken ct);

        /// <summary>
        /// Returns the most recent version available for download for each of the specified types.
        /// </summary>
        /// <param name="archiveTypes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryDownload>> GetLatestVersionForTypes(IEnumerable<OpenLibraryArchiveType> archiveTypes, CancellationToken ct);

        /// <summary>
        /// Returns the most recent version available for download for the specified type.
        /// </summary>
        /// <param name="archiveType"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OpenLibraryDownload> GetLatestVersionForType(OpenLibraryArchiveType archiveType, CancellationToken ct);
    }
}