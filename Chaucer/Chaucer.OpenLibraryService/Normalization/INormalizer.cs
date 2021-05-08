namespace Chaucer.OpenLibraryService.Normalization
{
    public interface INormalizer<TIn, TOut>
    {
        public TOut Normalize(TIn input);
    }
}