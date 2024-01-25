using log4net;
using System.Data.SqlClient;

namespace СellEvolution
{
    public class StatDatabase
    {
        private readonly ILog log = LogManager.GetLogger(typeof(StatDatabase));

        private string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True"; // Подставьте свою строку подключения к MS SQL
        private string tableErrorMoves = "ErrorAdam";
        private string tableGenAll = "AllAction";

        public StatDatabase()
        {
            InitializeDatabase();
        }

        public void UpdateStat(int Day, double DayErrorValue, double ActionsInCellGensProc)
        {
            InsertDataError(Day, DayErrorValue);
            InsertDataAllActionsProc(Day, ActionsInCellGensProc);
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
        public bool InsertDataAllActionsProc(int lineNumber, double value)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = $"INSERT INTO {tableGenAll} (Day, Procent) VALUES (@Day, @Procent)";

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

                // Удаляем существующую таблицу
                string dropTableQuery2 = $"IF OBJECT_ID('{tableGenAll}', 'U') IS NOT NULL DROP TABLE {tableGenAll}";
                using (SqlCommand cmdDropTable = new SqlCommand(dropTableQuery2, connection))
                {
                    cmdDropTable.ExecuteNonQuery();
                }

                // Создаем новую таблицу
                string createTableQuery2 = $"CREATE TABLE {tableGenAll} (Day INT, Procent FLOAT)";
                using (SqlCommand cmdCreateTable = new SqlCommand(createTableQuery2, connection))
                {
                    cmdCreateTable.ExecuteNonQuery();
                }
            }
        }
    }
}
