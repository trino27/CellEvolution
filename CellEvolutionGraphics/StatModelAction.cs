using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellEvolutionGraphics
{
    public class StatModelAction
    {
        public int Day { get; set; }
        public double Procent { get; set; }

        // Метод для загрузки данных из базы данных
        public static List<StatModelAction> LoadDataFromDatabase(string tableName)
        {
            List<StatModelAction> stats = new List<StatModelAction>();

            string connectionString = "Data Source=DESKTOP-C4JUPMT;Initial Catalog=CellEvolution;Encrypt=False;Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Выбираем данные из базы данных
                string query = "SELECT Day, Procent FROM " + tableName;
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            StatModelAction stat = new StatModelAction
                            {
                                Day = Convert.ToInt32(reader["Day"]),
                                Procent = Convert.ToDouble(reader["Procent"])
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
