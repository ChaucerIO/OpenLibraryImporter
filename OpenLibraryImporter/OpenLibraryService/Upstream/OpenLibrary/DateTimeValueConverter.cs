using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    /// <summary>
    /// DateTimes follow the type + value pattern that some other values follow
    /// </summary>
    public class DateTimeValueConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dt = (DateTime) value;
            var formatted = dt.ToString("s");
            var token = JToken.FromObject(formatted);
            token.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            return token switch
            {
                JObject when token["value"] is not null => (token["value"] ?? DateTime.MinValue).Value<DateTime>(),
                JValue when token.Type is JTokenType.Date => token.Value<DateTime>(),
                _ => throw new InvalidOperationException($"{token} does not appear to be a known DateTime type")
            };
        }

        public override bool CanConvert(Type objectType)
            => objectType == typeof(string);
    }
}