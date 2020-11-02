using System;
using System.Text;

namespace Chaucer.Common
{
    // Need IComparable and IEquatable
    
    /// <summary>
    /// Intended to represent ambiguous dates, including birth years, birth months, and specific days in a year. This type does not prevent you from assigning
    /// invalid values, like a 32nd day of the 15th month of year int.MaxValue.
    /// </summary>
    public struct Date :
        IEquatable<Date>,
        IComparable<Date>
    {
        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
        
        public Date(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public Date(int year) :
            this(year, -1, -1) { }
        
        public Date(int year, int month) :
            this(year, month, -1) { }

        /// <summary>
        /// Returns a Date, by truncating the time component(s)
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static Date FromDateTime(DateTime dt)
            => new Date(dt.Year, dt.Month, dt.Day);

        /// <summary>
        /// Uses DateTime.Parse to create a DateTime, then truncates the time component(s)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Date Parse(string date)
            => FromDateTime(DateTime.Parse(date));

        public bool Equals(Date other)
            => Year == other.Year && Month == other.Month && Day == other.Day;

        public override bool Equals(object obj)
            => obj is Date other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Year, Month, Day);

        public static bool operator ==(Date left, Date right)
            => left.Equals(right);

        public static bool operator !=(Date left, Date right)
            => !left.Equals(right);

        /// <summary>
        /// Returns an ISO-8601 date string (YYYY-MM-dd)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Year < 0)
            {
                return "";
            }
            
            var builder = new StringBuilder();
            if (Year > -1)
            {
                builder.Append(Year.ToString().PadLeft(2));
            }

            if (Month > -1)
            {
                builder.Append("-");
                builder.Append(Month.ToString().PadLeft(2));
            }

            if (Day > -1)
            {
                builder.Append("-");
                builder.Append(Day.ToString().PadLeft(4));
            }

            return builder.ToString();
        }

        public int CompareTo(Date other)
        {
            var yearComparison = Year.CompareTo(other.Year);
            if (yearComparison != 0)
            {
                return yearComparison;
            }
            
            var monthComparison = Month.CompareTo(other.Month);
            return monthComparison != 0
                ? monthComparison
                : Day.CompareTo(other.Day);
        }
    }
}