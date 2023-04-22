using System;
using System.Security.Cryptography;

namespace EV_Charger_App.Services
{
    public class Session
    {
        private const int tokenLength = 16;
        private byte[] token;

        int vehicleCharge = 100;

        public Session(string email, Database db)
        {
            token = GenerateToken();
            TokenToDatabase(email, db);
        }

        /// <summary>
        /// Generates a new session token
        /// </summary>
        /// <returns></returns>
        public byte[] GenerateToken()
        {
            byte[] token = new byte[tokenLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(token);
            }

            return token;
        }

        /// <summary>
        /// Converts a session token to a hexadecimal string that will be stored in the database
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public string TokenToString(byte[] token)
        {
            return BitConverter.ToString(token).Replace("-", string.Empty);
        }

        /// <summary>
        /// Updates session token in the database
        /// </summary>
        /// <param name="email"></param>
        /// <param name="db"></param>
        public void TokenToDatabase(string email, Database db)
        {
            string[] column = { "sessionToken" };
            string[] value = { TokenToString(token) };
            db.UpdateRecord("Users", column, value, "email", email);
        }

        /// <summary>
        /// Returns string of DB token
        /// </summary>
        /// <returns></returns>
        public string getToken()
        {
            return TokenToString(token);
        }

        /// <summary>
        /// Checks if the given token is valid
        /// </summary>
        /// <param name="tokenInQuestion"></param>
        /// <returns></returns>
        public bool TokenValid(string tokenInQuestion)
        {
            if (tokenInQuestion == TokenToString(token))
            {
                return true;
            }

            return false;
        }

        public void setVehicleCharge(int vehicleCharge)
        {
            this.vehicleCharge = vehicleCharge;
        }

        public int getVehicleCharge()
        {
            return vehicleCharge;
        }

    }
}
