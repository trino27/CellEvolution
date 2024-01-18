using System;

namespace CellEvolution.Cell.NN
{
    public struct NNCellBrain
    {
        public readonly Random random = new Random();

        private int[] layersSizes =
            {
                128,

                128,
                128,
                96,
                64,

                32
            };

        public NNLayers[] layers;

        public NNCellBrain()
        {
            random = new Random();
            NetworkInit();
        }

        private void NetworkInit()
        {
            layers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                if (i != layersSizes.Length - 1)
                {
                    layers[i] = new NNLayers(layersSizes[i], layersSizes[i + 1]);
                }
                else
                {
                    layers[i] = new NNLayers(layersSizes[i], 0);
                }
            }
        }

        public double[] FeedForward(double[] input)
        {
            layers[0].neurons = input;

            for (int i = 1; i < layers.Length; i++)
            {
                NNLayers l = layers[i - 1];
                NNLayers l1 = layers[i];

                for (int j = 0; j < l1.size; j++)
                {
                    l1.neurons[j] = 0;
                    for (int k = 0; k < l.size; k++)
                    {
                        l1.neurons[j] += l.neurons[k] * l.weights[k, j];
                    }
                    l1.neurons[j] += l1.biases[j];
                    l1.neurons[j] = SigmoidFunc(l1.neurons[j]);
                }
            }
            return layers[layers.Length - 1].neurons;
        }
        public double[] FeedForwardWithNoise(double[] input)
        {
            layers[0].neurons = input;

            for (int i = 1; i < layers.Length; i++)
            {
                NNLayers l = layers[i - 1];
                NNLayers l1 = layers[i];

                for (int j = 0; j < l1.size; j++)
                {
                    l1.neurons[j] = 0;
                    for (int k = 0; k < l.size; k++)
                    {
                        l1.neurons[j] += l.neurons[k] * l.weights[k, j];
                    }
                    l1.neurons[j] += l1.biases[j];

                    // Добавление шума
                    l1.neurons[j] += GenerateRandomNoise() * Constants.noiseIntensity;

                    // Применение дропаута
                    if (random.NextDouble() < Constants.dropoutProbability)
                    {
                        l1.neurons[j] = 0;
                    }
                    else
                    {
                        l1.neurons[j] = SigmoidFunc(l1.neurons[j]);
                    }
                }
            }
            return layers[layers.Length - 1].neurons;
        }

        private double GenerateRandomNoise()
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public void RandomFillWeightsParallel()
        {
            NNLayers[] layersTemp = layers;
            Parallel.For(0, layers.Length, i =>
            {
                ThreadLocal<Random> localRandom = new ThreadLocal<Random>(() => new Random());

                for (int j = 0; j < layersTemp[i].size; j++)
                {
                    layersTemp[i].biases[j] = localRandom.Value.NextDouble() * 2.0 - 1.0;

                    if (i != layersTemp.Length - 1)
                    {
                        for (int k = 0; k < layersTemp[i + 1].size; k++)
                        {
                            layersTemp[i].weights[j, k] = localRandom.Value.NextDouble() * 2.0 - 1.0;
                        }
                    }
                }
            });

            layers = layersTemp;
        }

        public void Clone(NNCellBrain original, double[]? randomInputToClone)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }
        }

        private void CopyNNLayers(NNCellBrain original)
        {
            for (int k = 0; k < layers.Length; k++)
            {
                Array.Copy(original.layers[k].weights, layers[k].weights, original.layers[k].weights.Length);
                Array.Copy(original.layers[k].neurons, layers[k].neurons, original.layers[k].neurons.Length);
                Array.Copy(original.layers[k].biases, layers[k].biases, original.layers[k].biases.Length);

                layers[k].size = original.layers[k].size;
                layers[k].nextSize = original.layers[k].nextSize;
            }
        }

        public void BackPropagation(double[] targets)
        {
            double learningRate = Constants.learningRate;

            int outputErrorSize = layers[layers.Length - 1].size;

            double[] outputErrors = new double[outputErrorSize];

            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - layers[layers.Length - 1].neurons[i];
            }

            for (int k = layers.Length - 2; k >= 0; k--)
            {
                NNLayers l = layers[k];
                NNLayers l1 = layers[k + 1];

                double[] errorsNext = new double[l.size];
                Task taskError = Task.Run(() =>
                { // Обновим веса текущего слоя
                    Parallel.For(0, l.size, i =>
                    {
                        double errorSum = 0;
                        for (int j = 0; j < l1.size; j++)
                        {
                            errorSum += l.weights[i, j] * outputErrors[j];
                        }
                        errorsNext[i] = errorSum;
                    });
                });

                double[] gradients = new double[l1.size];
                for (int i = 0; i < l1.size; i++)
                {
                    gradients[i] = outputErrors[i] * DsigmoidFunc(layers[k + 1].neurons[i]);
                    gradients[i] *= learningRate;
                }

                double[,] deltas = new double[l1.size, l.size];
                Task taskDeltas = Task.Run(() =>
                { // Обновим веса текущего слоя
                    Parallel.For(0, l1.size, i =>
                    {
                        for (int j = 0; j < l.size; j++)
                        {
                            deltas[i, j] = gradients[i] * l.neurons[j];
                        }
                    });
                });

                // Обновим смещения (biases) следующего слоя
                for (int i = 0; i < l1.size; i++)
                {
                    l1.biases[i] += gradients[i];
                }

                taskError.Wait();
                // Обновим ошибку для следующей итерации
                outputErrors = errorsNext;

                taskDeltas.Wait();
                Task taskUpdate = Task.Run(() =>
                { // Обновим веса текущего слоя
                    Parallel.For(0, l1.size, i =>
                    {
                        for (int j = 0; j < l.size; j++)
                        {
                            l.weights[j, i] += deltas[i, j];
                        }
                    });
                });

                taskUpdate.Wait();

                //RAdamOptimizerWithThreshold(l.weights, deltas, layers.Length - 2 - k);
            }
        }

        public void LearnFromExp(double[] inputs, int correctTarget)
        {
            double[] targets = new double[layers[^1].size];
            targets[correctTarget] = 1;

            FeedForward(inputs);
            BackPropagation(targets);
        }

        public void LearnErrorFromExp(double[] inputs, int[] errorTarget)
        {
            double[] targets = new double[layers[^1].size];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = 1;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                for (int j = 0; j < errorTarget.Length; j++)
                {
                    if (errorTarget[j] == i)
                    {
                        targets[i] = 0;
                    }
                }
            }

            FeedForward(inputs);
            BackPropagation(targets);
        }

        private double SigmoidFunc(double x) => 1.0 / (1.0 + Math.Exp(-x));
        private double DsigmoidFunc(double x) => x * (1.0 - x);

        private void RandomCloneNoise()
        {
            long NumOfAllWeights = 0;
            foreach (var l in layers)
            {
                NumOfAllWeights += l.weights.LongLength;
            }

            int NumOfChanging = random.Next(0, Convert.ToInt32(NumOfAllWeights / 10));

            for (int i = 0; i < NumOfChanging; i++)
            {
                int randLayer = random.Next(0, layers.Length - 1);
                int randWeightD1 = random.Next(0, layers[randLayer].size);
                int randWeightD2 = random.Next(0, layers[randLayer].nextSize);

                layers[randLayer].weights[randWeightD1, randWeightD2] = random.NextDouble();
            }
        }

        private void RAdamOptimizerWithThreshold(double[,] weights, double[,] deltas, int t)
        {
            double beta1 = 0.95;       // Увеличил для большего учета старых градиентов.
            double beta2 = 0.997;      // Увеличил для большего учета старых квадратов градиентов.
            double epsilon = 1e-6;     // Уменьшил для более высокой устойчивости и предотвращения деления на ноль.
            double clippingThreshold = 10.0; // Увеличил порог обрезки, чтобы более агрессивно справляться с выбросами.
            double rho = 0.95;         // Предложенное значение для адаптации, может потребовать дополнительной настройки в зависимости от данных.


            int rows = weights.GetLength(0);
            int cols = weights.GetLength(1);

            double[,] m = new double[rows, cols];
            double[,] v = new double[rows, cols];

            double beta1t = 1.0 - Math.Pow(beta1, t);
            double beta2t = 1.0 - Math.Pow(beta2, t);

            double adjustedLearningRate = Constants.learningRate * (1.0 - Constants.noiseIntensity);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    m[i, j] = beta1 * m[i, j] + (1.0 - beta1) * deltas[j, i];
                    v[i, j] = beta2 * v[i, j] + (1.0 - beta2) * Math.Pow(deltas[j, i], 2);
                }
            }

            // Смещенные оценки первого и второго моментов
            double mHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta1, t)) / (1.0 - beta1t);
            double vHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta2, t)) / (1.0 - beta2t);

            // Обновление весов 
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double mHat = m[i, j] / mHatCorrection;
                    double vHat = v[i, j] / vHatCorrection;

                    double rhoInf = 2.0 / (1.0 - rho) - 1.0;
                    double adapt = Math.Max(0, rhoInf - 2.0 * t * Math.Pow(beta2t, 2));

                    double stepSize = adjustedLearningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);
                    double denom = Math.Sqrt(vHat) + epsilon;
                    double update = stepSize * mHat / denom;

                    // Rectification term
                    update += adapt * stepSize * mHat;

                    // Добавляем робастность к выбросам
                    
                    if (update > clippingThreshold)
                    {
                        update = clippingThreshold;
                    }
                    else if (update < -clippingThreshold)
                    {
                        update = -clippingThreshold;
                    }

                    weights[i, j] -= update;
                }
            }
        }

        private void RAdamOptimizer(double[,] weights, double[,] deltas, int t)
        {
            double beta1 = 0.9;
            double beta2 = 0.999;
            double epsilon = 1e-8;

            int rows = weights.GetLength(0);
            int cols = weights.GetLength(1);

            double[,] m = new double[rows, cols];
            double[,] v = new double[rows, cols];

            double beta1t = 1.0 - Math.Pow(beta1, t);
            double beta2t = 1.0 - Math.Pow(beta2, t);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    m[i, j] = beta1 * m[i, j] + (1.0 - beta1) * deltas[j, i];
                    v[i, j] = beta2 * v[i, j] + (1.0 - beta2) * Math.Pow(deltas[j, i], 2);
                }
            }

            // Смещенные оценки первого и второго моментов
            double mHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta1, t)) / (1.0 - beta1t);
            double vHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta2, t)) / (1.0 - beta2t);

            // Обновление весов 
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double mHat = m[i, j] / mHatCorrection;
                    double vHat = v[i, j] / vHatCorrection;

                    double rho = 0.9; // Значение rho может быть настроено под ваши данные
                    double rhoInf = 2.0 / (1.0 - rho) - 1.0;
                    double adapt = Math.Max(0, rhoInf - 2.0 * t * Math.Pow(beta2t, 2));

                    double stepSize = Constants.learningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);
                    double denom = Math.Sqrt(vHat) + epsilon;
                    double update = stepSize * mHat / denom;

                    // Rectification term
                    update += adapt * stepSize * mHat;

                    weights[i, j] -= update;
                }
            }
        }
        private void AdamOptimizer(double[,] weights, double[,] deltas, int t)
        {
            double beta1 = 0.9;
            double beta2 = 0.999;
            double epsilon = 1e-8;

            int rows = weights.GetLength(0);
            int cols = weights.GetLength(1);

            double[,] m = new double[rows, cols];
            double[,] v = new double[rows, cols];

            double beta1t = 1.0 - Math.Pow(beta1, t);
            double beta2t = 1.0 - Math.Pow(beta2, t);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    m[i, j] = beta1 * m[i, j] + (1.0 - beta1) * deltas[j, i];
                    v[i, j] = beta2 * v[i, j] + (1.0 - beta2) * Math.Pow(deltas[j, i], 2);
                }
            }

            // Коррекция смещения моментов
            double correction = Constants.learningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);

            // Обновление весов 
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    weights[i, j] -= correction * m[i, j] / (Math.Sqrt(v[i, j]) + epsilon);
                }
            }
        }


    }
}
