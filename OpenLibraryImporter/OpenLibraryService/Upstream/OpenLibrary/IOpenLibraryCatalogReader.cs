using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public interface IOpenLibraryCatalogReader
    {
        /// <summary>
        /// Returns the date stamps associated with the published Open Library catalog versions
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyCollection<DateTime>> GetCatalogDatestampsAsync();
        
        /// <summary>
        /// Returns the downloadable components associated with the specified date stamp. For example, if only the authors are updated in a catalog revision,
        /// only the authors download information will be returned.
        /// </summary>
        /// <param name="datestamp"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<OpenLibraryDownload>> GetDownloadsForVersionAsync(DateTime datestamp);
    }
}