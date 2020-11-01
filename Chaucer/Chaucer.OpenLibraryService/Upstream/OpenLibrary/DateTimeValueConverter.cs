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
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var dt = (token["value"] ?? DateTime.MinValue).Value<DateTime>();
            return dt;
        }

        private static string ToTrimmedOrNullString(string input)
        {
            return string.IsNullOrWhiteSpace(input)
                ? null
                : input.Trim();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}