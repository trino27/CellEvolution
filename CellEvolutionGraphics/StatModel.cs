using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace CellEvolutionGraphics
{
    public class StatModel
    {
        public int Day { get; set; }
        public double TotalErrorPoint { get; set; }

        // Метод для загрузки данных из базы данных
        public static List<StatModel> LoadDataFromDatabase()
        {
            List<StatModel> stats = new List<StatModel>();

            string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Выбираем данные из базы данных
                string query = "SELECT Day, TotalErrorPoint FROM TableClearCellBrainStat1";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StatModel stat = new StatModel
                            {
                                Day = Convert.ToInt32(reader["Day"]),
                                TotalErrorPoint = Convert.ToDouble(reader["TotalErrorPoint"])
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
