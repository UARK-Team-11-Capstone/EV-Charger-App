using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Security.Principal;

namespace EV_Charger_App.Services
{
    internal class Session
    {
        private const int tokenLength = 16;
        private byte[] token;

        Session()
        {
            token = GenerateToken();
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

        // Converts a session token to a hexadecimal string
        public string TokenToString(byte[] token)
        {
            return BitConverter.ToString(token).Replace("-", string.Empty);
        }

        public string getToken()
        {
            return TokenToString(token);
        }


    }
}
