using log4net;
using System;
using System.Data.SqlClient;

namespace СellEvolution
{
    internal class Stat
    {
        private readonly ILog log = LogManager.GetLogger(typeof(Stat));

        private string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True"; // Подставьте свою строку подключения к MS SQL
        private string tableName = "TableCellBrainStatNoGenAdam2fixed";

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
                string createTableQuery = $"CREATE TABLE {tableName} (Day INT, TotalErrorPoint FLOAT)";
                using (SqlCommand cmdCreateTable = new SqlCommand(createTableQuery, connection))
                {
                    cmdCreateTable.ExecuteNonQuery();
                }
            }
        }

        public bool InsertData(int lineNumber, double value)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"INSERT INTO {tableName} (Day, TotalErrorPoint) VALUES (@Day, @TotalErrorPoint)";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Day", lineNumber);
                        cmd.Parameters.AddWithValue("@TotalErrorPoint", value);

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
