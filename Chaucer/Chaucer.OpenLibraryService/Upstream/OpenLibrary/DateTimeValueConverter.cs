using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// DateTimes follow the type + value pattern that some other values follow
    /// </summary>
    public class DateTimeValueConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dt = (DateTime) value;
            var formatted = dt.ToString("O");
            var token = JToken.FromObject(formatted);
            token.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var dt = (token["value"] ?? DateTime.MinValue).Value<DateTime>();
            return dt;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}