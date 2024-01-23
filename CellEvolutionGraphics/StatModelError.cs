using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CellEvolutionGraphics
{
    public class StatModelError
    {
        public int Day { get; set; }
        public double ErrorPoint { get; set; }

        // Метод для загрузки данных из базы данных
        public static List<StatModelError> LoadDataFromDatabase(string tableName)
        {
            List<StatModelError> stats = new List<StatModelError>();

            string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Выбираем данные из базы данных
                string query = "SELECT Day, ErrorPoint FROM " + tableName;
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StatModelError stat = new StatModelError
                            {
                                Day = Convert.ToInt32(reader["Day"]),
                                ErrorPoint = Convert.ToDouble(reader["ErrorPoint"])
                            };
                            stats.Add(stat);
                        }
                    }
                }
            }

            return stats;
        }
    }
}
