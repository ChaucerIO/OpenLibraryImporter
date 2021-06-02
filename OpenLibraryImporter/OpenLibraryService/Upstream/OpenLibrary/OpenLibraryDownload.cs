using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;
using OpenLibraryService.Utilities;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public enum OpenLibraryArchiveType
    {
        Authors,
        Editions,
    }
    
    public static class OpenLibArchiveTypeExtensions
    {
        private static readonly HashSet<OpenLibraryArchiveType> _knownArchiveTypes = Enum.GetValues<OpenLibraryArchiveType>().ToHashSet();

        public static IReadOnlySet<OpenLibraryArchiveType> KnownArchiveTypes()
            => _knownArchiveTypes;

        public static string GetKey(this OpenLibraryArchiveType archiveTypeArchiveType)
        {
            return archiveTypeArchiveType switch
            {
                OpenLibraryArchiveType.Authors => "authors",
                OpenLibraryArchiveType.Editions => "editions",
                _ => throw new ArgumentException($"{archiveTypeArchiveType} is not a supported archive type"),
            };
        }

        public static OpenLibraryArchiveType GetArchiveType(this string archiveType)
        {
            if (string.Equals("authors", archiveType, StringComparison.Ordinal))
            {
                return OpenLibraryArchiveType.Authors;
            }

            if (string.Equals("editions", archiveType, StringComparison.Ordinal))
            {
                return OpenLibraryArchiveType.Editions;
            }
            
            // Prior two cases are way more common
            if (string.IsNullOrWhiteSpace(archiveType))
            {
                throw new ArgumentNullException(nameof(archiveType));
            }

            throw new ArgumentOutOfRangeException($"{archiveType} is not a valid {nameof(OpenLibraryArchiveType)}");
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
            => $"{dt.ToIsoDateString()}-{archiveType.GetKey()}-orig.txt.gz";
    }

    [DynamoDBTable("chaucer-openlib-versions")]
    public record OpenLibraryVersion
    {
        [DynamoDBHashKey]
        public string Kind { get; init; }

        [DynamoDBRangeKey(typeof(DynamoDateTimeConverter))]
        public DateTime PublishDate { get; init; }
        
        /// <summary>
        /// The URL originally used to download the open library payload  
        /// </summary>
        public string SourceUrl { get; init; }
        
        /// <summary>
        /// The filename
        /// </summary>
        public string ObjectName { get; init; }
        public long Bytes { get; init; }
        public string Uri { get; init; }
    }
}