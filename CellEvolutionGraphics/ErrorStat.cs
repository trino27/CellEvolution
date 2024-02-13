using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LiveCharts;
using LiveCharts.Wpf;

namespace CellEvolutionGraphics
{
    public partial class ErrorStat : Form
    {
        private List<StatModelError> ErrorNoGenSwish = new List<StatModelError>();
        //private List<StatModelError> ErrorNoGenTg = new List<StatModelError>();
        //private List<StatModelError> ErrorNoGenMish = new List<StatModelError>();
        //private List<StatModelError> ErrorNoGenSigmoid = new List<StatModelError>();
        //private List<StatModelError> ErrorNoGenLeakyReLU = new List<StatModelError>();


        private System.Windows.Forms.Timer timer;

        public ErrorStat()
        {
            Thread.Sleep(25000);
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
            //ErrorNoGenSigmoid = LoadStatsFromDatabase("ErrorNoGenSigmoid");
            //ErrorNoGenTg = LoadStatsFromDatabase("ErrorNoGenTg");
            ErrorNoGenSwish = LoadStatsFromDatabase("ErrorSwish");
            //ErrorNoGenMish = LoadStatsFromDatabase("ErrorNoGenMish");
            //ErrorNoGenLeakyReLU = LoadStatsFromDatabase("ErrorNoGenLeakyReLU");

            //var ErrorNoGenLeakyReLUSeries = new LineSeries
            //{
            //    Title = "ErrorNoGenLeakyReLU", // Заголовок для второго графика
            //    Values = new ChartValues<double>(ErrorNoGenLeakyReLU.ConvertAll(s => s.ErrorPoint)),
            //};
            ////var ErrorNoGenSigmoidSeries = new LineSeries
            ////{
            ////    Title = "ErrorNoGenSigmoid", // Заголовок для второго графика
            ////    Values = new ChartValues<double>(ErrorNoGenSigmoid.ConvertAll(s => s.ErrorPoint)),
            ////};
            //var ErrorNoGenMishSeries = new LineSeries
            //{
            //    Title = "ErrorNoGenMish", // Заголовок для второго графика
            //    Values = new ChartValues<double>(ErrorNoGenMish.ConvertAll(s => s.ErrorPoint)),
            //};
            //var ErrorNoGenTgSeries = new LineSeries
            //{
            //    Title = "ErrorNoGenTg", // Заголовок для второго графика
            //    Values = new ChartValues<double>(ErrorNoGenTg.ConvertAll(s => s.ErrorPoint)),
            //};
            var ErrorNoGenSwishSeries = new LineSeries
            {
                Title = "ErrorSwish", // Заголовок для второго графика
                Values = new ChartValues<double>(ErrorNoGenSwish.ConvertAll(s => s.ErrorPoint)),
            };
            cartesianChart1.Series.Clear();

            
            cartesianChart1.Series.Add(ErrorNoGenSwishSeries);
            //cartesianChart1.Series.Add(ErrorNoGenTgSeries);
            //cartesianChart1.Series.Add(ErrorNoGenLeakyReLUSeries);
            //cartesianChart1.Series.Add(ErrorNoGenSigmoidSeries);
            //cartesianChart1.Series.Add(ErrorNoGenMishSeries);
        }

        private List<StatModelError> LoadStatsFromDatabase(string tableName)
        {
            return StatModelError.LoadDataFromDatabase(tableName);
        }

        private void InitTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 120000; // Интервал в миллисекундах 
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
