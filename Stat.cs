using log4net;
using System;
using System.Data.SqlClient;

namespace СellEvolution
{
    internal class Stat
    {
        private readonly ILog log = LogManager.GetLogger(typeof(Stat));

        private string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True"; // Подставьте свою строку подключения к MS SQL
        private string tableName = "ErrorAdam";
        private string tableName2 = "AllAction";

        public Stat()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Удаляем существующую таблицу
                string dropTableQuery = $"IF OBJECT_ID('{tableName}', 'U') IS NOT NULL DROP TABLE {tableName}";
                using (SqlCommand cmdDropTable = new SqlCommand(dropTableQuery, connection))
                {
                    cmdDropTable.ExecuteNonQuery();
                }

                // Создаем новую таблицу
                string createTableQuery = $"CREATE TABLE {tableName} (Day INT, ErrorPoint FLOAT)";
                using (SqlCommand cmdCreateTable = new SqlCommand(createTableQuery, connection))
                {
                    cmdCreateTable.ExecuteNonQuery();
                }

                // Удаляем существующую таблицу
                string dropTableQuery2 = $"IF OBJECT_ID('{tableName2}', 'U') IS NOT NULL DROP TABLE {tableName2}";
                using (SqlCommand cmdDropTable = new SqlCommand(dropTableQuery2, connection))
                {
                    cmdDropTable.ExecuteNonQuery();
                }

                // Создаем новую таблицу
                string createTableQuery2 = $"CREATE TABLE {tableName2} (Day INT, Procent FLOAT)";
                using (SqlCommand cmdCreateTable = new SqlCommand(createTableQuery2, connection))
                {
                    cmdCreateTable.ExecuteNonQuery();
                }
            }
        }

        public bool InsertDataError(int lineNumber, double value)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"INSERT INTO {tableName} (Day, ErrorPoint) VALUES (@Day, @ErrorPoint)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Day", lineNumber);
                        cmd.Parameters.AddWithValue("@ErrorPoint", value);

                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error inserting data: {ex.Message}");
                return false;
            }
        }

        public bool InsertDataAllActionsProc(int lineNumber, double value)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"INSERT INTO {tableName2} (Day, Procent) VALUES (@Day, @Procent)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Day", lineNumber);
                        cmd.Parameters.AddWithValue("@Procent", value);

                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error inserting data: {ex.Message}");
                return false;
            }
        }
    }
}
