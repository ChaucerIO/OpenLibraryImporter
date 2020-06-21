using System;
using System.Collections.Generic;
using System.Linq;
using Chaucer.Common.Borrowing;

namespace Chaucer.Common.People
{
    public class Patron
    {
        public Guid Id { get; set; }
        public Person Person { get; set; }
        public bool AccrueFees { get; set; }
        public List<Guid> ActiveCheckouts { get; set; }
        
        // Fees
        public decimal FeesOwed => OutstandingFees.Sum(f => f.RemainingFee);
        public List<AccruedFee> OutstandingFees { get; set; }

        /// <summary>
        /// The fees the patron has paid over their lifetime
        /// </summary>
        public decimal LifetimeFees => SettledFees.Sum(f => f.OriginalFee);
        public List<AccruedFee> SettledFees { get; set; }
        
        // checkout history
        // active fines
        // past fines
        public List<Guid> CheckoutHistory { get; set; }
        
    }
}