using System;

namespace OpenLibraryService.Utilities
{
    public static class DateTimeExtensions
    {
        public static string ToIsoDateString(this DateTime dt)
            => dt.ToString("yyyy-MM-dd");
    }
}