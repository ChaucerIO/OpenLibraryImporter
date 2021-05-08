using System;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public enum OpenLibraryType
    {
        Authors,
        Editions,
    }
    
    public record OpenLibraryDownload
    {
        public string Url { get; init; }
        public OpenLibraryType Type { get; init; }
        public DateTime Timestamp { get; init; }
    }
}