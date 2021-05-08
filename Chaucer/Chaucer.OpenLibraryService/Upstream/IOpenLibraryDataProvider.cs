using System.Collections.Generic;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;

namespace Chaucer.OpenLibraryService.Upstream
{
    public interface IOpenLibraryDataProvider
    {
        Task<List<Author>> GetAuthorsAsync();
    }
}