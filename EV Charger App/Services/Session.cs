using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Security.Principal;

namespace EV_Charger_App.Services
{
    public class Session
    {
        private const int tokenLength = 16;
        private byte[] token;

        public Session(string email, Database db)
        {
            token = GenerateToken();
            TokenToDatabase(email, db);
        }

        // Generates a new session token
        public byte[] GenerateToken()
        {
            byte[] token = new byte[tokenLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(token);
            }

            return token;
        }

        // Converts a session token to a hexadecimal string that will be stored in the database
        public string TokenToString(byte[] token)
        {
            return BitConverter.ToString(token).Replace("-", string.Empty);
        }

        public void TokenToDatabase(string email, Database db)
        {
            string[] column = { "sessionToken" };
            string[] value = { TokenToString(token) };
            db.UpdateRecord("Users", column, value, "email", email);
        }

        public string getToken()
        {
            return TokenToString(token);
        }


        public bool TokenValid(string tokenInQuestion)
        {
            if(tokenInQuestion == TokenToString(token))
            {
                return true;
            }

            return false;
        }

    }
}
