using System;
using System.IO;

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
        public DateTime Datestamp { get; init; }
        public Stream Destination { get; set; }
    }

    public record OpenLibraryDownloadReport
    {
        public OpenLibraryDownload Download { get; init; }
        public long Bytes { get; init; }
        public string Uri { get; init; }
    }
}