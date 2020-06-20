using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class Author
    {
        [JsonPropertyName("bio")]
        public string Bio { get; set; }
        
        [JsonPropertyName("name")]
        public string DisplayName { get; set; }
        
        [JsonPropertyName("links")]
        public List<DisplayLink> Links { get; set; }
        
        [JsonPropertyName("personal_name")]
        public string PersonalName { get; set; }
        
        [JsonPropertyName("alternate_names")]
        public List<string> AlternateNames { get; set; }
        
        [JsonPropertyName("title")]
        public string Title { get; set; }
        
        [JsonPropertyName("wikipedia")]
        public Uri WikipediaUri { get; set; }
        
        [JsonPropertyName("created")]
        public DateTime Created { get; set; }
        
        [JsonPropertyName("last_modified")]
        public DateTime LastModified { get; set; }
        
        [JsonPropertyName("latest_revision")]
        public int LatestRevision { get; set; }
        
        [JsonPropertyName("revision")]
        public int Revision { get; set; }
        
        [JsonPropertyName("photos")]
        public List<int> Photos { get; set; }
        
        [JsonPropertyName("birth_date")]
        public DateTime DateOfBirth { get; set; }
        
        [JsonPropertyName("person")]
        public string EntityType { get; set; }
        
        [JsonPropertyName("remote_ids")]
        public Dictionary<string, string> RemoteIds { get; set; }
    }
    
    public class DisplayLink
    {
        [JsonPropertyName("title")]
        public string Tag { get; set; }
        
        [JsonPropertyName("url")]
        public Uri Uri { get; set; }
    }
}