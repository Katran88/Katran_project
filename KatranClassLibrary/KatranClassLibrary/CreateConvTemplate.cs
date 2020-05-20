using System;
using System.Collections.Generic;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class CreateConvTemplate
    {
        public int ChatId;
        public string Title;
        public byte[] Image;
        public List<Contact> ConvMembers;

        public CreateConvTemplate()
        {
            ChatId = -1;
            Title = "";
            Image = null;
            ConvMembers = new List<Contact>();
        }

        public CreateConvTemplate(int chatId, string title, byte[] image, List<Contact> convMembers)
        {
            ChatId = chatId;
            Title = title;
            Image = image;
            ConvMembers = convMembers;
        }
    }
}
