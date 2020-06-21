using System.Collections.Generic;
using Chaucer.Common.Contact;
using Chaucer.Common.People;

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
        public List<NameValuePair<Person>> OtherPeople { get; set; }
        public string TimeZone { get; set; }
    }
}