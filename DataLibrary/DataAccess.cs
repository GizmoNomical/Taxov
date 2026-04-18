using Dapper;
using MySql.Data;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary
{
    public class DataAccess : IDataAccess
    {
        public async Task<List<T>> LoadData<T, U>(string sql, U parameters, string connectionString)
        {
            using (IDbConnection connection = new MySqlConnection(connectionString))
            {
                var rows = await connection.QueryAsync<T>(sql, parameters);

                return rows.ToList();
            }
        }

        public Task SaveData<T>(string sql, T parameters, string connectionString)
        {
            using (IDbConnection connection = new MySqlConnection(connectionString))
            {
                return connection.ExecuteAsync(sql, parameters);
            }
        }

        public static async Task<Boolean> CheckConnection(string connectionString)
        {
            bool result = false;

            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();
                result = true;
                connection.Close();
            }
            catch( Exception ex )
            {
                Console.WriteLine("CheckConnection to DB Failed.  Server Offline.");
                Console.WriteLine(ex);
                result = false;
            }
            return result;
        }
    }
}
