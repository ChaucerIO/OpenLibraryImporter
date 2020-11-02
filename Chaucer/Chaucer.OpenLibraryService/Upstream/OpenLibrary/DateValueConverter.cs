using System;
using System.Globalization;
using Chaucer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class DateValueConverter: JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var date = token.Value<string>();
            
            if (DateTime.TryParse(date, out var result))
            {
                return Date.FromDateTime(result);
            }

            return Date.FromDateTime(DateTime.MinValue);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}