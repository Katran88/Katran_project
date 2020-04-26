using System;
using System.Security.Cryptography;
using System.Text;

namespace KatranClassLibrary
{
    [Serializable]
    public class AuthtorizationTemplate
    {
        public string AuthLogin;
        public string Password;

        private AuthtorizationTemplate()
        {
            this.AuthLogin = "";
            this.Password = null;
        }

        public AuthtorizationTemplate(string auth_login, string password)
        {
            this.AuthLogin = auth_login;
            this.Password = null;

            using (MD5 md5Hash = MD5.Create())
            {
                byte[] bytes_password = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                StringBuilder tempBuilder = new StringBuilder(460);
                for (int i = 0; i < bytes_password.Length; i++)
                {
                    tempBuilder.Append(bytes_password[i].ToString("x2"));
                }
                this.Password = tempBuilder.ToString();
            }
        }
    }
}
