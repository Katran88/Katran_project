using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace KatranClassLibrary
{
    public class Chat
    {
        private UdpClient udpclient;
        private IPAddress multicastaddress;
        private IPEndPoint remoteep;

        public Chat()
        {
            multicastaddress = IPAddress.Parse("127.0.0.1"); // один из зарезервированных для локальных нужд UDP адресов
            udpclient = new UdpClient();
            udpclient.JoinMulticastGroup(multicastaddress);
            remoteep = new IPEndPoint(multicastaddress, 8101);
        }

        public void SendMessage(string data)
        {
            Byte[] buffer = Encoding.UTF8.GetBytes(data);

            Byte[] encrypted = Encrypt(data);

            udpclient.Send(encrypted, encrypted.Length, remoteep);
        }

        public void Listen()
        {
            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 2222);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEp);

            client.JoinMulticastGroup(multicastaddress);

            Console.WriteLine("\tListening started");

            string formatted_data;

            while (true)
            {
                Byte[] data = client.Receive(ref localEp);

                formatted_data = Decrypt(data);

                Console.WriteLine(formatted_data);
            }
        }

        private byte[] Encrypt(string clearText, string EncryptionKey = "123")
        {

            byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);
            byte[] encrypted;
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 }); // еще один плюс шарпа в наличие таких вот костылей.
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encrypted = ms.ToArray();
                }
            }
            return encrypted;
        }

        private string Decrypt(byte[] cipherBytes, string EncryptionKey = "123")
        {
            string cipherText = "";
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

    }
}
