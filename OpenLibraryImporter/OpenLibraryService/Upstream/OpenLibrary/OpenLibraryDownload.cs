using System;
using Amazon.DynamoDBv2.DataModel;
using OpenLibraryService.Utilities;

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
        public string ObjectName => this.GetObjectName();
    }

    public static class OpenLibraryDownloadExtensions
    {
        public static string GetObjectName(this OpenLibraryDownload dl)
            => GetObjectName(dl.Datestamp, dl.ArchiveType);
        
        public static string GetObjectName(DateTime dt, OpenLibraryArchiveType archiveType)
            => $"{dt.ToIsoDateString()}-{archiveType.GetKey()}.json.gz";
    }

    [DynamoDBTable("chaucer-openlib-versions")]
    public record OpenLibraryVersion
    {
        [DynamoDBHashKey] public string Kind => Download.ArchiveType.GetKey();

        [DynamoDBRangeKey(typeof(DynamoDateTimeConverter))]
        public string PublishDate => Download.Datestamp.ToIsoDateString();
        
        public OpenLibraryDownload Download { get; init; }
        public long Bytes { get; init; }
        public string Uri { get; init; }
    }
}