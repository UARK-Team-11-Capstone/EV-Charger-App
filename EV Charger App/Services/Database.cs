using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace EV_Charger_App.Services
{
    internal class Database
    {
        // Login information
        private readonly string endpoint = "database-1.c2crdg7hfqi3.us-east-1.rds.amazonaws.com";
        private readonly string databaseName = "Login1";
        private readonly string username = "admin";
        private readonly string password = "capstone11";
        private readonly int port = 3306;

        private MySqlConnection connection;

        public MySqlConnection Connection => connection;

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

        public void Disconnect()
        {
            connection?.Close();
            connection = null;
        }

        public void Query(String query)
        {
            if (Connect())
            {

            }
        }

        //Inserts a full record into the database
        public void InsertRecord(string tableName, string[] recordValues)
        {
            if (Connect())
            {
                string values = string.Join(",", recordValues);
                string query = $"INSERT INTO {tableName} VALUES ({values})";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }

        //Inserts a record with only the specified column values
        public void InsertRecordSpecific(string tableName, string[] columnNames, string[] recordValues)
        {
            if (Connect())
            {
                if (columnNames.Length == recordValues.Length)
                {
                    string columns = string.Join(",", columnNames);
                    string values = string.Join(",", recordValues);
                    string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateRecord(string tableName, string[] columnNames, string[] columnValues, string whereColumn, string whereValue)
        {
            if (Connect())
            {
                if (columnNames.Length == columnValues.Length)
                {
                    // Build the SET clause of the SQL query

                    List<string> setClauses = new List<string>();

                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        setClauses.Add($"{columnNames[i]} = '{columnValues[i]}'");
                    }

                    string setClause = string.Join(", ", setClauses);

                    // Build the WHERE clause of the SQL query
                    string whereClause = $"{whereColumn} = '{whereValue}'";

                    // Build the full SQL query
                    string query = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.ExecuteNonQuery();
                }
            }
        }

        //Returns a list of array of objects, where each array of objects is a record, and each individual object in the array is a value in the record
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
            }

            return results;
        }


        public bool RecordExists(string query)
        {
            List<object[]> results = GetQueryRecords(query);

            if (results.Count > 0)
            {
                return true;
            }

            return false;
        }

    }
}
