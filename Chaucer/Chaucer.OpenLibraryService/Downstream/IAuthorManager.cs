using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;

namespace Chaucer.OpenLibraryService.Downstream
{
    public interface IAuthorManager
    {
        Task SaveAuthorsAsync(ICollection<Author> authors, DateTime datestamp);
        Task<ICollection<Author>> GetAuthorsAsync(DateTime datestamp);
    }
}