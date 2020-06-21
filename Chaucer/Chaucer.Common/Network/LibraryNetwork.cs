using System.Collections.Generic;
using Chaucer.Common.Contact;
using Chaucer.Common.People;

namespace Chaucer.Common.Network
{
    public class LibraryNetwork
    {
        public string Administrator { get; set; }
        public EmailAddress EmailAddress { get; set; }
        public MailingAddress MailingAddress { get; set; }
        public Branch MainOffice { get; set; }
        public List<Branch> Branches { get; set; }
        public List<EmailAddress> OtherEmailAddresses { get; set; }
        public List<NameValuePair<Person>> OtherPeople { get; set; }
    }
}