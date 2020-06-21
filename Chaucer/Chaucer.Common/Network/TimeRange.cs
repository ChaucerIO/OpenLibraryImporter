using NodaTime;

namespace Chaucer.Common.Network
{
    public class TimeRange
    {
        public LocalTime Open { get; set; }
        public LocalTime Close { get; set; }
        public Period Duration => Close - Open;
        public bool IsClosed => Duration == Period.Zero;
    }
}