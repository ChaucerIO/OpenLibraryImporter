using System;
using System.Collections.Generic;

namespace Chaucer.Common.People
{
    public class Household
    {
        public Guid Id { get; set; }
        public List<Person> Members { get; set; }
        // public decimal Fines => this.
    }
}