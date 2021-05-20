using System;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace OpenLibraryService.Upstream.OpenLibrary
{
    public class DynamoDateTimeConverter :
        IPropertyConverter
    {
        public DynamoDBEntry ToEntry(object value)
        {
            return value switch
            {
                DateTime dateTime => new Primitive(dateTime.ToString("yyyy-MM-dd")),
                DateTimeOffset dateTimeOffset => new Primitive(dateTimeOffset.ToString("yyyy-MM-dd")),
                _ => throw new ArgumentException($"{value} is not a valid UTC DateTime or DateTimeOffset")
            };
        }

        public object FromEntry(DynamoDBEntry entry)
        {
            // We will emit only ISO-8601 datetime strings with no UTC offset, because the value itself is UTC. 
            var primitive = entry as Primitive;
            if (primitive?.Value is null or not string)
            {
                throw new ArgumentException("Not a valid datestamp");
            }

            return DateTime.Parse((string)primitive.Value);
        }
    }
}