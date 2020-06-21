using NodaTime;

namespace Chaucer.Common.Borrowing
{
    public class Payment
    {
        public LocalDate Date { get; set; }
        public decimal Paid { get; set; }
    }
}