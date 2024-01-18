using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LiveCharts;
using LiveCharts.Wpf;

namespace CellEvolutionGraphics
{
    public partial class Form1 : Form
    {
        private List<StatModel> stats = new List<StatModel>();
        private System.Windows.Forms.Timer timer;

        public Form1()
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
            cartesianChart1.LegendLocation = LiveCharts.LegendLocation.Left;
        }

        public void LoadData()
        {
            stats = LoadStatsFromDatabase();

            cartesianChart1.Series.Clear();

            // Создаем график для отображения данных
            var series = new LineSeries
            {
                Title = "Total Error Points",
                Values = new ChartValues<double>(stats.ConvertAll(s => s.TotalErrorPoint)),
            };

            cartesianChart1.Series.Add(series);
        }

        private List<StatModel> LoadStatsFromDatabase()
        {
            return StatModel.LoadDataFromDatabase();
        }

        private void InitTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 60000; // Интервал в миллисекундах (1 мин)
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
