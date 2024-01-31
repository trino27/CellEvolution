using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using LiveCharts.WinForms;

namespace CellEvolutionGraphics
{
    public partial class ActionsStat : Form
    {
        private List<StatModelAction> AllActionDQN = new List<StatModelAction>();
        private List<StatModelAction> AllActionNN = new List<StatModelAction>();

        private System.Windows.Forms.Timer timer;

        public ActionsStat()
        {
            InitializeComponent();
            InitChart();
            InitTimer();
        }

        public void InitChart()
        {
            cartesianChart1.AxisX.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Day",
            });
            cartesianChart1.AxisY.Add(new LiveCharts.Wpf.Axis
            {
                Title = "Value",
            });
            cartesianChart1.LegendLocation = LiveCharts.LegendLocation.Bottom;
        }

        public void LoadData()
        {
            AllActionDQN = LoadStatsFromDatabase("AllActionDQN");
            AllActionNN = LoadStatsFromDatabase("AllActionNN");

            var AllActionDQNSeries = new LineSeries
            {
                Title = "AllActionDQN", // Заголовок для второго графика
                Values = new ChartValues<double>(AllActionDQN.ConvertAll(s => s.Procent)),
            };
            var AllActionNNSeries = new LineSeries
            {
                Title = "AllActionNN", // Заголовок для второго графика
                Values = new ChartValues<double>(AllActionNN.ConvertAll(s => s.Procent)),
            };

            cartesianChart1.Series.Clear();

            cartesianChart1.Series.Add(AllActionDQNSeries);
            cartesianChart1.Series.Add(AllActionNNSeries);
        }

        private List<StatModelAction> LoadStatsFromDatabase(string tableName)
        {
            return StatModelAction.LoadDataFromDatabase(tableName);
        }

        private void InitTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 60000; // Интервал в миллисекундах 
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            LoadData(); // Обновляем данные при срабатывании таймера
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadData(); // Загружаем данные при загрузке формы
        }
    }
}
