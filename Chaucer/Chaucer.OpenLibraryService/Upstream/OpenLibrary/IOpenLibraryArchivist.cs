using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public interface IOpenLibraryArchivist
    {
        /// <summary>
        /// Streams the Open Library download to the target location. Typically an HTTP stream to filesystem stream or HTTP stream to object store stream.
        /// </summary>
        /// <param name="updates"></param>
        /// <returns></returns>
        Task<IReadOnlyList<OpenLibraryDownloadReport>> StreamUpdatesToArchiveAsync(ICollection<OpenLibraryDownload> updates);

        Task<IReadOnlyList<Author>> NormalizeAuthorsAsync(DateTime datestamp);
    }
}