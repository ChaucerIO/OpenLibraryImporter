using System;
using Amazon.DynamoDBv2.DataModel;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public enum OpenLibraryArchiveType
    {
        Authors,
        Editions,
    }
    
    public static class OpenLibraryTypeExtensions
    {
        public static string GetKey(this OpenLibraryArchiveType archiveTypeArchiveType)
        {
            return archiveTypeArchiveType switch
            {
                OpenLibraryArchiveType.Authors => "authors",
                OpenLibraryArchiveType.Editions => "editions",
                _ => throw new ArgumentException($"{archiveTypeArchiveType} is not a supported archive type"),
            };
        }
    }
    
    public record OpenLibraryDownload
    {
        public string Source { get; init; }
        public OpenLibraryArchiveType ArchiveType { get; init; }
        public DateTime Datestamp { get; init; }
    }

    [DynamoDBTable("chaucer-openlib-versions")]
    public record OpenLibraryVersion
    {
        [DynamoDBHashKey]
        public string Type { get; init; }
        [DynamoDBRangeKey(typeof(DynamoDateTimeConverter))]
        public string Version { get; init; }
        public OpenLibraryDownload Download { get; init; }
        public long Bytes { get; init; }
        public string Uri { get; init; }
    }
}