using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chaucer.OpenLibraryService.Upstream.OpenLibrary
{
    public class DisplayLinkConverter : JsonConverter
    {
        public override bool CanWrite => false; 

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
                try
                {
                    // [0] = {KeyValuePair<string, JToken>} [url, deu.anarchopedia.org/Ralf_Landmesser]
                    // [1] = {KeyValuePair<string, JToken>} [type, {\n  "key": "/type/link"\n}]
                    // [2] = {KeyValuePair<string, JToken>} [title, Anarchopedia (german)]
                    var url = element.SelectToken("url").ToString();
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        uri = FixUrl(url);
                    }

                    if (uri is null)
                    {
                        continue;
                    }
                    
                    var title = element.SelectToken("title").ToString();
                    dict[uri] = title;
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return dict;
        }

        private static Uri FixUrl(string broken)
        {
            // Empty URL cases
            if (string.IsNullOrWhiteSpace(broken))
            {
                return null;
            }

            var normalized = broken.Trim();
            
            // Missing http
            if (!normalized.StartsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                var newUri = Uri.UriSchemeHttp;
                var hasScheme = normalized.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal);
                if (!hasScheme)
                {
                    newUri += Uri.SchemeDelimiter;
                }

                newUri += normalized;
                return Uri.TryCreate(newUri, UriKind.Absolute, out var ok)
                    ? ok
                    : throw new ArgumentException($"URL appeared to be missing a scheme, but something else was also wrong: {broken}");
            }
            
            var isEmptyScheme = normalized.EndsWith(Uri.SchemeDelimiter, StringComparison.Ordinal)
                || normalized.EndsWith(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            if (isEmptyScheme)
            {
                throw new ArgumentException($"URL appears to be empty: {broken}");
            }
            
            // Hostname is unparseable, maybe because a . is a ,
            var hostnameType = Uri.CheckHostName(normalized);
            if (hostnameType != UriHostNameType.Dns)
            {
                // URL is encoded
                var decoded = HttpUtility.UrlDecode(normalized);
                Uri ok;
                if (Uri.TryCreate(decoded, UriKind.Absolute, out ok))
                {
                    return ok;
                }
                
                // Multiple http prefixes => Unhandled
                var lastHttpIndex = normalized.LastIndexOf(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
                var lastHttpValue = normalized.Substring(lastHttpIndex);
                if (Uri.TryCreate(lastHttpValue, UriKind.Absolute, out ok))
                {
                    return ok;
                }

                // Removing space is just as destructive as not, because sometimes multiple URLs are jammed into a single field, but removing spaces
                // makes them parseable, which is also wrong.
                throw new ArgumentException($"URL appears to contain an unparseable hostname: {broken}");
            }
            
            throw new ArgumentException($"Unparseable URL: {broken}");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}