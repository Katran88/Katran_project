using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class AddRemoveContactTargetTemplate
    {
        public Contact NewContact;

        public AddRemoveContactTargetTemplate()
        {
            NewContact = new Contact();
        }

        public AddRemoveContactTargetTemplate(Contact newContact)
        {
            NewContact = newContact;
        }
    }
}
