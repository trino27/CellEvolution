using log4net;
using System.Data.SqlClient;

namespace CellEvolution
{
    public class StatDatabase
    {
        private readonly ILog log = LogManager.GetLogger(typeof(StatDatabase));

        private string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True"; // Подставьте свою строку подключения к MS SQL
        private string tableErrorMoves = "ErrorSwish";

        public StatDatabase()
        {
            InitializeDatabase();
        }

        public void UpdateStat(int Day, double DayErrorValue)
        {
            InsertDataError(Day, DayErrorValue);
        }

        public bool InsertDataError(int lineNumber, double value)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"INSERT INTO {tableErrorMoves} (Day, ErrorPoint) VALUES (@Day, @ErrorPoint)";

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

        private void InitializeDatabase()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Удаляем существующую таблицу
                string dropTableQuery = $"IF OBJECT_ID('{tableErrorMoves}', 'U') IS NOT NULL DROP TABLE {tableErrorMoves}";
                using (SqlCommand cmdDropTable = new SqlCommand(dropTableQuery, connection))
                {
                    cmdDropTable.ExecuteNonQuery();
                }

                // Создаем новую таблицу
                string createTableQuery = $"CREATE TABLE {tableErrorMoves} (Day INT, ErrorPoint FLOAT)";
                using (SqlCommand cmdCreateTable = new SqlCommand(createTableQuery, connection))
                {
                    cmdCreateTable.ExecuteNonQuery();
                }
            }
        }
    }
}
