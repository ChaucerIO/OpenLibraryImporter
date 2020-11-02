using System;
using System.Collections.Generic;
using Chaucer.Common;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// Represents the complete object graph from the open library data. Mostly used for deserialization purposes. For working with Authors, you are probably
    /// looking for the <typeparam name="Author"></typeparam> datatype.
    /// </summary>
    public class Author
    {
        //Note: bio is sometimes a plain string rather than an object
        [JsonConverter(typeof(BioConverter))]
        public string Bio { get; set; }
        
        [JsonProperty("date"), JsonConverter(typeof(DateValueConverter))]
        public Date Date { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("personal_name")]
        public string PersonalName { get; set; }
        
        [JsonProperty("title")]
        public string Title { get; set; }
        
        [JsonProperty("website")]
        public string Website { get; set; }
        
        [JsonProperty("links"), JsonConverter(typeof(DisplayLinkConverter))]
        public Dictionary<Uri, string> Links { get; set; }
        
        [JsonProperty("remote_ids")]
        public Dictionary<string, string> RemoteIds { get; set; } 
        
        [JsonProperty("alternate_names")]
        public List<string> AlternateNames { get; set; }
        
        [JsonProperty("created"), JsonConverter(typeof(DateTimeValueConverter))]
        public DateTime Created { get; set; }
        
        [JsonProperty("death_date")]
        public string DeathDate { get; set; }

        [JsonProperty("photos")]
        public List<long> Photos { get; set; }

        [JsonProperty("last_modified"), JsonConverter(typeof(DateTimeValueConverter))]
        public DateTime LastModified { get; set; }
        
        [JsonProperty("latest_revision")]
        public int LatestRevision { get; set; }
        
        [JsonProperty("key")]
        public string Key { get; set; }
        
        [JsonProperty("birth_date"), JsonConverter(typeof(DateValueConverter))]
        public Date BirthDate { get; set; }
        
        [JsonProperty("fuller_name")]
        public string FullerName { get; set; }
        
        [JsonProperty("revision")]
        public int Revision { get; set; }
        
        [JsonProperty("wikipedia")]
        public string Wikipedia { get; set; }
    }
}