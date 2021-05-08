using System;
using System.Collections.Generic;
using System.Globalization;
using Chaucer.Common;

namespace Chaucer.OpenLibraryService.Normalization
{
    public class DateNormalizer :
        INormalizer<string, IList<Date>>
    {
        private static readonly CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
        
        public IList<Date> Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            
            // Date flavors: YYYY; YYYY-MM; YYYY-MM-DD; YYYY?; d.YYYY; b.YYYY; 21.2.1944
            // Date range: YYYY-YYYY, fl. YYYY, fl. YYYY-YYYY, 15th century, fl. ca. YYYY, 19th cent., active 15th cent., jin shi 1700, active YYYY
            //  1945 or 46
            
            if (DateTime.TryParse(input, out var result))
            {
                return new List<Date> {Date.FromDateTime(result)};
            }

            foreach (var culture in cultures)
            {
                if (DateTime.TryParse(input, culture, DateTimeStyles.None, out var date))
                {
                    return new List<Date> {Date.FromDateTime(date)};
                }
            }

            try
            {
                var split = input.Split("-", StringSplitOptions.TrimEntries);
                return new List<Date> {new(split)};
            }
            catch (Exception){}
            
            // Punctuation cases
            if (input.EndsWith("?", StringComparison.Ordinal))
            {
                var foo = input.TrimEnd('?');
                if (DateTime.TryParse(foo, out var date))
                {
                    return new List<Date> {Date.FromDateTime(date)};
                }

                try
                {
                    var split = foo.Split("-", StringSplitOptions.TrimEntries);
                    return new List<Date> {new(split)};
                }
                catch (Exception){}
            }
            
            
            
            
            Console.WriteLine(input);

            return new List<Date>{Date.FromDateTime(DateTime.MinValue)};
        }
    }
}