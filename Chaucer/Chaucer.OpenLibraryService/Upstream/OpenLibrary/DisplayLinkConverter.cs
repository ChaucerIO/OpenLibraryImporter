using System;
using System.Collections.Generic;
using System.Linq;
using Chaucer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class DisplayLinkConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var elements = JToken.Load(reader) as JArray;
            if (elements is null)
            {
                return null;
            }
            
            var dict = new Dictionary<Uri, string>(elements.Count);
            foreach (var element in elements)
            {
                var url = element.SelectToken("url").ToString();
                var uri = new Uri(url, UriKind.Absolute);
                var title = element.SelectToken("title").ToString();
                dict[uri] = title;
            }

            return dict;
            
            // var date = elements.Value<string>();
            //
            // if (DateTime.TryParse(date, out var result))
            // {
            //     return Date.FromDateTime(result);
            // }
            //
            // return Date.FromDateTime(DateTime.MinValue);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}