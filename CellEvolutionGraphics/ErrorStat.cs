using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LiveCharts;
using LiveCharts.Wpf;

namespace CellEvolutionGraphics
{
    public partial class ErrorStat : Form
    {
        private List<StatModelError> ErrorNN = new List<StatModelError>();
        private List<StatModelError> ErrorChangeAdam = new List<StatModelError>();

        private System.Windows.Forms.Timer timer;

        public ErrorStat()
        {
            Thread.Sleep(5000);
            InitializeComponent();
            InitChart();
            InitTimer();

            ActionsStat actionsStat = new ActionsStat();
            actionsStat.Show();
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
            ErrorNN = LoadStatsFromDatabase("ErrorNN");
            ErrorChangeAdam = LoadStatsFromDatabase("ErrorNNTestChangeAdam");

            var ErrorNNSeries = new LineSeries
            {
                Title = "Errors", // Заголовок для второго графика
                Values = new ChartValues<double>(ErrorNN.ConvertAll(s => s.ErrorPoint)),
            };
            var ErrorTrainSeries = new LineSeries
            {
                Title = "ErrorsTrain", // Заголовок для второго графика
                Values = new ChartValues<double>(ErrorChangeAdam.ConvertAll(s => s.ErrorPoint)),
            };
            cartesianChart1.Series.Clear();
            
            cartesianChart1.Series.Add(ErrorTrainSeries);
            cartesianChart1.Series.Add(ErrorNNSeries);
        }

        private List<StatModelError> LoadStatsFromDatabase(string tableName)
        {
            return StatModelError.LoadDataFromDatabase(tableName);
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
