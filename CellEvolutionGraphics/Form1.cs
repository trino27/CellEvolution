using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LiveCharts;
using LiveCharts.Wpf;

namespace CellEvolutionGraphics
{
    public partial class Form1 : Form
    {
        private List<StatModel> stats1 = new List<StatModel>();
        private List<StatModel> stats2 = new List<StatModel>();
        private List<StatModel> stats3 = new List<StatModel>();
        private List<StatModel> stats4 = new List<StatModel>();
        private List<StatModel> stats6 = new List<StatModel>();
        private List<StatModel> stats7 = new List<StatModel>();
        private List<StatModel> stats8 = new List<StatModel>();
        private List<StatModel> stats9 = new List<StatModel>();
        private List<StatModel> stats10 = new List<StatModel>();


        private List<StatModel> ClearErrorRandTrueTarget_TrueProb1AllLearn = new List<StatModel>();
        private List<StatModel> TableCellBrainStatNoGenAdam = new List<StatModel>();
        private List<StatModel> TableCellBrainStatNoGenAdam2fixed = new List<StatModel>();

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
            cartesianChart1.LegendLocation = LiveCharts.LegendLocation.Bottom;
        }

        public void LoadData()
        {
            ClearErrorRandTrueTarget_TrueProb1AllLearn = LoadStatsFromDatabase("TableCellBrainStatNoGenClear5");
            TableCellBrainStatNoGenAdam = LoadStatsFromDatabase("TableCellBrainStatNoGenAdam");

            var ClearErrorRandTrueTarget_TrueProb1AllLearnSeries = new LineSeries
            {
                Title = "ClearErrorRandTrueTarget_TrueProb1AllLearn", // Заголовок для второго графика
                Values = new ChartValues<double>(ClearErrorRandTrueTarget_TrueProb1AllLearn.ConvertAll(s => s.TotalErrorPoint)),
            };

            var TableCellBrainStatNoGenAdamSeries = new LineSeries
            {
                Title = "TableCellBrainStatNoGenAdam", // Заголовок для второго графика
                Values = new ChartValues<double>(TableCellBrainStatNoGenAdam.ConvertAll(s => s.TotalErrorPoint)),
            };

            var TableCellBrainStatNoGenAdam2fixedSeries = new LineSeries
            {
                Title = "TableCellBrainStatNoGenAdam2fixed", // Заголовок для второго графика
                Values = new ChartValues<double>(TableCellBrainStatNoGenAdam2fixed.ConvertAll(s => s.TotalErrorPoint)),
            };

            cartesianChart1.Series.Clear();

            cartesianChart1.Series.Add(ClearErrorRandTrueTarget_TrueProb1AllLearnSeries);
            cartesianChart1.Series.Add(TableCellBrainStatNoGenAdamSeries);
            cartesianChart1.Series.Add(TableCellBrainStatNoGenAdam2fixedSeries);










            // stats1 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear");
            // stats2 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear2");
            // stats3 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear3");
            // stats4 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear4");

            // stats6 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear6");
            // stats7 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear7");
            // stats8 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear8");
            // stats9 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear9");
            // stats10 = LoadStatsFromDatabase("TableCellBrainStatNoGenClear10");

            // cartesianChart1.Series.Clear();

            //var series = new LineSeries
            //{
            //    Title = "ClearError0",
            //    Values = new ChartValues<double>(stats1.ConvertAll(s => s.TotalErrorPoint)),
            //};

            // var series2 = new LineSeries
            // {
            //     Title = "ClearError0_TrueProb1AlreadyLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats2.ConvertAll(s => s.TotalErrorPoint)),
            // };

            // var series3 = new LineSeries
            // {
            //     Title = "ClearTrueProb1AlreadyLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats3.ConvertAll(s => s.TotalErrorPoint)),
            // };

            // var series4 = new LineSeries
            // {
            //     Title = "ClearErrorRandTrueTarget_TrueProb1AlreadyLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats4.ConvertAll(s => s.TotalErrorPoint)),
            // };

            // var series5 = new LineSeries
            // {
            //     Title = "ClearErrorRandTrueTarget_TrueProb1AllLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats5.ConvertAll(s => s.TotalErrorPoint)),
            // };

            // var series6 = new LineSeries
            // {
            //     Title = "ClearError0_TrueProb1AllLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats6.ConvertAll(s => s.TotalErrorPoint)),
            // };
            // var series7 = new LineSeries
            // {
            //     Title = "ClearError0_TrueProb_0_1_AllLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats7.ConvertAll(s => s.TotalErrorPoint)),
            // };
            // var series8 = new LineSeries
            // {
            //     Title = "ClearError0_TrueProb_0_1_AlreadyLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats8.ConvertAll(s => s.TotalErrorPoint)),
            // };
            // var series9 = new LineSeries
            // {
            //     Title = "ClearErrorRandTrueTarget", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats9.ConvertAll(s => s.TotalErrorPoint)),
            // };
            // var series10 = new LineSeries
            // {
            //     Title = "ClearErrorRandTrueTarget_TrueProb_0_1_AlreadyLearn", // Заголовок для второго графика
            //     Values = new ChartValues<double>(stats10.ConvertAll(s => s.TotalErrorPoint)),
            // };


            // cartesianChart1.Series.Add(series);
            // cartesianChart1.Series.Add(series2);
            // cartesianChart1.Series.Add(series3);
            // cartesianChart1.Series.Add(series4);

            // cartesianChart1.Series.Add(series6);
            // cartesianChart1.Series.Add(series7);
            // cartesianChart1.Series.Add(series8);
            // cartesianChart1.Series.Add(series9);
            // cartesianChart1.Series.Add(series10);
        }

        private List<StatModel> LoadStatsFromDatabase(string tableName)
        {
            return StatModel.LoadDataFromDatabase(tableName);
        }

        private void InitTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 30000; // Интервал в миллисекундах 
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
