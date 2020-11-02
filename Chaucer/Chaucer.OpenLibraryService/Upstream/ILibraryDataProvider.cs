using System.Collections.Generic;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;

namespace Chaucer.OpenLibraryService.Upstream
{
    public interface ILibraryDataProvider
    {
        // Task<string> GetGzippedAuthorDataAsync(string path, Encoding textEncoding);
        // IEnumerable<string> ExtractAuthorJson(string path);
        // Task<string> GetGzippedTitleJsonAsync(string path, Encoding textEncoding);
        // IEnumerable<string> ExtractTitleJson(string path);

        Task<List<Author>> GetAuthorsAsync();
    }
}