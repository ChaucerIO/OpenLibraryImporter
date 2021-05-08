using System.Collections.Generic;
using System.Threading.Tasks;
using Chaucer.OpenLibraryService.Upstream.OpenLibrary;

namespace Chaucer.OpenLibraryService.Downstream
{
    public interface IAuthorManager
    {
        Task WriteAuthorsAsync(ICollection<Author> authors, string recordName);
    }
}