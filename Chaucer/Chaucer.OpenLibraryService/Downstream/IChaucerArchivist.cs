using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;

namespace Chaucer.OpenLibraryService.Downstream
{
    public interface IChaucerArchivist
    {
        /// <summary>
        /// Reads the raw archive file, and extracts the Authors from it
        /// </summary>
        /// <param name="datestamp"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<Author>> NormalizeAuthorsAsync(DateTime datestamp);
        
        /// <summary>
        /// Saves the collection of Authors to its archival location. Typically a filesystem or object store.
        /// </summary>
        /// <param name="authors"></param>
        /// <param name="datestamp"></param>
        /// <returns></returns>
        Task SaveAuthorsAsync(ICollection<Author> authors, DateTime datestamp);
        
        /// <summary>
        /// Reads the collection of Authors from its archival location. Typically a filesystem or object store.
        /// </summary>
        /// <param name="datestamp"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<Author>> GetAuthorsAsync(DateTime datestamp);

        /// <summary>
        /// Returns the datestamp associated with the most recent, normalized Author archive
        /// </summary>
        /// <returns></returns>
        Task<DateTime> GetLastAuthorsDatestampAsync();

        /// <summary>
        /// Returns all of the datestamps for previous Author archives
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyCollection<DateTime>> GetAuthorsDatestampsAsync();

        /// <summary>
        /// Returns the datestamp associated with the most recent, normalized Edition archive
        /// </summary>
        /// <returns></returns>
        Task<DateTime> GetLastEditionsDatestampAsync();

        /// <summary>
        /// Returns all of the datestamps for previous Editions archives
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyCollection<DateTime>> GetEditionsDatestampsAsync();
    }
}