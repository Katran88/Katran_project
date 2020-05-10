using System;

namespace KatranClassLibrary
{
    [Serializable]
    public enum RRType
    {
        None, 
        Authorization, 
        Registration, 
        Error,
        RefreshUserConnection, 
        RefreshContacts
    }

    [Serializable]
    public class RRTemplate
    {
        public RRType RRType;
        public object RRObject;

        RRTemplate()
        {
            this.RRType = RRType.None;
            this.RRObject = null;
        }

        public RRTemplate(RRType requestType, object requestObject)
        {
            this.RRType = requestType;
            this.RRObject = requestObject;
        }
    }
}
