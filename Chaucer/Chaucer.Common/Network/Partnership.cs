using System.Collections.Generic;
using Chaucer.Common.Contact;

namespace Chaucer.Common.Network
{
    public class Partnership
    {
        public string Administrator { get; set; }
        public EmailAddress EmailAddress { get; set; }
        public MailingAddress MailingAddress { get; set; }
        public List<LibraryNetwork> ParticipatingNetworks { get; set; }
    }
}