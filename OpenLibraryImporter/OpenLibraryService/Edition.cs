namespace OpenLibraryService
{
    public record Edition :
        IDated
    {
        public string Date { get; }
    }
}