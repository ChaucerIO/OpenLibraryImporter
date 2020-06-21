using Chaucer.Common.Borrowing;

namespace Chaucer.Common
{
    public interface IBorrowable
    {
        string Title { get; }
        Category Category { get; }
    }
}