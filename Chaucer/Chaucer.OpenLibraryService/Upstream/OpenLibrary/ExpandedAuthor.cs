using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// Represents the complete object graph from the open library data. Mostly used for deserialization purposes. For working with Authors, you are probably
    /// looking for the <typeparam name="Author"></typeparam> datatype.
    /// </summary>
    public class ExpandedAuthor
    {
        //Note: bio is sometimes a plain string rather than an object
        [JsonConverter(typeof(BioConverter))]
        public string Bio { get; set; }
        
        public string Date { get; set; }
        
        public string Name { get; set; }
        
        [JsonProperty("personal_name")]
        public string PersonalName { get; set; }
        
        public string Title { get; set; }
        
        public string Website { get; set; }
        
        [JsonProperty("remote_ids")]
        public Dictionary<string, string> RemoteIds { get; set; } 
        
        [JsonProperty("alternate_names")]
        public List<string> AlternateNames { get; set; }
        
        [JsonConverter(typeof(DateTimeValueConverter))]
        public DateTime Created { get; set; }
        
        [JsonProperty("death_date")]
        public string DeathDate { get; set; }

        public List<long> Photos { get; set; }

        [JsonProperty("last_modified"), JsonConverter(typeof(DateTimeValueConverter))]
        public DateTime LastModified { get; set; }
        
        [JsonProperty("latest_revision")]
        public int LatestRevision { get; set; }
        
        public string Key { get; set; }
        
        [JsonProperty("birth_date")]
        public string BirthDate { get; set; }
        
        public class Type
        {
            public string Key { get; set; }
        }
        
        [JsonProperty("fuller_name")]
        public string FullerName { get; set; }
        
        public int Revision { get; set; }
        
        public string Wikipedia { get; set; }
    }
}