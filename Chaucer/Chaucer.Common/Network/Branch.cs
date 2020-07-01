using System.Collections.Generic;
using Chaucer.Common.Contact;
using Chaucer.Common.Extensions;
using Chaucer.Common.People;
using NodaTime;

namespace Chaucer.Common.Network
{
    public class Branch
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Administrator { get; set; }
        public bool IsOpenToPublic { get; set; }
        public EmailAddress EmailAddress { get; set; }
        public MailingAddress MailingAddress { get; set; }
        public HoursOfOperation ActiveHours { get; set; }
        public List<HoursOfOperation> AlternativeHours { get; set; }
        public List<Named<Person>> OtherPeople { get; set; }
        public string IanaTimeZone => TimeZone.Id;
        public DateTimeZone TimeZone { get; set; }
    }
}