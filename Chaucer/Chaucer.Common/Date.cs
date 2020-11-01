using System;
using System.Text;
using NodaTime;

namespace Chaucer.Common
{
    // Need IComparable and IEquatable
    
    /// <summary>
    /// Intended to represent ambiguous dates, including birth years, birth months, and specific days 
    /// </summary>
    public struct Date :
        IEquatable<Date>
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
        public string ToString()
        {
            var builder = new StringBuilder();
            
        }
    }
}