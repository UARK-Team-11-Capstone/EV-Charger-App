using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


namespace EV_Charger_App.Services
{
    public class Database
    {
        // Login information
        private readonly string endpoint = "database-1.c2crdg7hfqi3.us-east-1.rds.amazonaws.com";
        private readonly string databaseName = "Login1";
        private readonly string username = "admin";
        private readonly string password = "capstone11";

        private MySqlConnection connection;

        public bool Connect()
        {
            try
            {
                if (connection == null)
                {
                    string connectionString = string.Format("Server={0}; database={1}; UID={2}; password={3}", endpoint, databaseName, username, password);

                    connection = new MySqlConnection(connectionString);

                    connection.Open();
                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// Diconnects from the database
        /// </summary>
        public void Disconnect()
        {
            connection?.Close();
            connection = null;
        }


        public void ExecuteRawNonQuery(string query)
        {
            if (Connect())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Returns float rating of charger given its name
        /// </summary>
        /// <param name="chargerName"></param>
        /// <returns></returns>
        public float GetChargerRating(string chargerName)
        {
            int sum = 0;
            int count = 0;

            string query = "SELECT * FROM Reviews WHERE chargerName = '" + chargerName + "'";

            if (Connect())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sum += reader.GetInt32(2);
                            count++;
                        }
                    }
                }

                Disconnect();

            }

            //To prevent dividing by zero in the case no queries are found
            if (count == 0)
            {
                count++;
            }

            return (float)sum / count;
        }

        /// <summary>
        /// Inserts a full record into the database
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="recordValues"></param>
        public void InsertRecord(string tableName, string[] recordValues)
        {
            if (Connect())
            {
                // Create placeholders for values in the query
                string valuePlaceholders = string.Join(",", Enumerable.Range(0, recordValues.Length).Select(i => $"@value{i}"));

                // Build the full SQL query
                string query = $"INSERT INTO {tableName} VALUES ({valuePlaceholders})";

                // Create the parameter list
                List<MySqlParameter> parameters = new List<MySqlParameter>();
                for (int i = 0; i < recordValues.Length; i++)
                {
                    parameters.Add(new MySqlParameter($"@value{i}", recordValues[i]));
                }

                // Create and execute the command
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    command.ExecuteNonQuery();
                }

                Disconnect();
            }
        }

        /// <summary>
        /// Inserts a record with only the specified column values
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnNames"></param>
        /// <param name="recordValues"></param>
        public void InsertRecordSpecific(string tableName, string[] columnNames, string[] recordValues)
        {
            if (Connect())
            {
                if (columnNames.Length == recordValues.Length)
                {
                    // Build the column list and value placeholders of the SQL query
                    string columns = string.Join(",", columnNames);
                    string valuePlaceholders = string.Join(",", columnNames.Select(c => $"@{c}"));

                    // Build the full SQL query
                    string query = $"INSERT INTO {tableName} ({columns}) VALUES ({valuePlaceholders})";

                    // Create the parameter list
                    List<MySqlParameter> parameters = new List<MySqlParameter>();
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        parameters.Add(new MySqlParameter($"@{columnNames[i]}", recordValues[i]));
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddRange(parameters.ToArray());
                    command.ExecuteNonQuery();
                }

                Disconnect();

            }
        }

        /// Updates the record in the database

        public void UpdateRecord(string tableName, string[] columnNames, string[] columnValues, string whereColumn, string whereValue)
        {
            if (Connect())
            {
                if (columnNames.Length == columnValues.Length)
                {
                    // Build the SET clause of the SQL query
                    List<string> setClauses = new List<string>();
                    List<MySqlParameter> parameters = new List<MySqlParameter>();

                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        string parameterName = $"@{columnNames[i]}";
                        setClauses.Add($"{columnNames[i]} = {parameterName}");
                        parameters.Add(new MySqlParameter(parameterName, columnValues[i]));
                    }

                    string setClause = string.Join(", ", setClauses);

                    // Build the WHERE clause of the SQL query
                    string whereClause = $"{whereColumn} = @whereValue";
                    parameters.Add(new MySqlParameter("@whereValue", whereValue));

                    // Build the full SQL query
                    string query = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddRange(parameters.ToArray());
                    command.ExecuteNonQuery();
                }

                Disconnect();

            }


        }

        /// Returns a list of array of objects, where each array of objects is a record, and each individual object in the array is a value in the record

        public List<object[]> GetQueryRecords(string query)
        {
            List<object[]> results = new List<object[]>();

            if (Connect())
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    object[] record = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        record[i] = reader.GetValue(i);
                    }

                    results.Add(record);
                }

                Disconnect();

            }

            return results;
        }

        public bool RecordExists(string query, params MySqlParameter[] parameters)
        {
            int recordCount = 0;

            if (Connect())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // Add parameters to the command
                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    // Execute the SELECT statement and count the number of records returned
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recordCount++;
                        }
                    }
                }

                Disconnect();

            }

            // Return true if any records were found, false otherwise
            return recordCount > 0;
        }

        /// <summary>
        /// Hashes the given password and returns the hash
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Gets the API key from the database for the GoogleAPI
        /// </summary>
        /// <returns></returns>
        public string GetGoogleAPIKey()
        {
            string key = "";

            if (Connect())
            {
                string query = "SELECT * FROM APIKeys WHERE KeyName = 'Google API Key'";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            key = reader.GetString(1);
                        }
                    }
                }

                Disconnect();

            }

            return key;
        }

        public string GetDOEAPIKey()
        {
            string key = "";

            if (Connect())
            {
                string query = "SELECT * FROM APIKeys WHERE KeyName = 'DOE API Key'";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            key = reader.GetString(1);
                        }
                    }
                }

                Disconnect();

            }

            return key;
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // check for right formatting
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GetAccessibilityInfo(string chargerName)
        {
            int count = 0;
            int sum = 0;
            string query = "SELECT * FROM Reviews WHERE chargerName = '" + chargerName + "'";

            if (Connect())
            {
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sum += reader.GetInt32(5);
                            count++;
                        }
                    }
                }

                Disconnect();

            }

            float avg = (float)sum / count;

            if (avg >= .75)
            {
                return "1";
            }
            else
            {
                return "0";
            }

        }

    }
}
