using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class BioConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var formatted = (string) value;
            var token = JToken.FromObject(formatted);
            token.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token is JValue)
            {
                var value = token.Value<string>() ?? string.Empty;
                return ToTrimmedOrNullString(value);
            }
            
            if (token is JObject)
            {
                var jO = token as JObject;
                var value = (jO["value"] ?? string.Empty).Value<string>();
                return ToTrimmedOrNullString(value);
            }

            throw new ArgumentOutOfRangeException($"Unable to deserialize token type {reader.TokenType} as Bio object");
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