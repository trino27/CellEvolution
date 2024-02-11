using System;

namespace СellEvolution.WorldResources.Cell.NN
{
    public struct NNLayers
    {
        public int size { get; set; }
        public int nextSize { get; set; }
        public double[] neurons { get; set; }
        public double[] biases { get; set; }
        public double[,] weights { get; set; }
        public double[] errors { get; set; }

        public NNLayers(int size, int nextSize)
        {
            this.size = size;
            this.nextSize = nextSize;
            neurons = new double[size];
            biases = new double[size];
            weights = new double[size, nextSize];
            errors = new double[size];

            InitializeWeightsHe();
        }

        // Метод инициализации весов с использованием инициализации Хе
        public void InitializeWeightsHe()
        {
            Random random = new Random();
            double std = Math.Sqrt(2.0 / size); // Стандартное отклонение для инициализации Хе

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < nextSize; j++)
                {
                    weights[i, j] = random.NextDouble() * std * 2.0 - std; // Инициализация весов согласно инициализации Хе
                }
            }
        }
    }
}
