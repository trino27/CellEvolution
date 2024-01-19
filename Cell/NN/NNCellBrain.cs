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

        private void BackPropagation(double[] targets)
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

                AdamOptimizer(l.weights, deltas, layers.Length - 2 - k);
            }
        }

        public void UseExpToLearn(bool IsErrorMove, int numOfMoves, double[][] LastMovesInputs, int[] LastMovesDecidedActionsNum, bool[] ErrorMoves)
        {
            if (IsErrorMove)
            {
                List<int> AllErrorMoves = LookingForErrorMovesAtTurn(LastMovesInputs[0]);
                LearnErrorFromExp(LastMovesInputs[0], AllErrorMoves.ToArray());
            }
            else if (numOfMoves % (Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime) == 0)
            {
                //List<int> AlreadyLearn = new List<int>();
                for (int i = 0; i < LastMovesInputs.Length; i++)
                {
                    List<int> AllErrorMoves = LookingForErrorMovesAtTurn(LastMovesInputs[i]);
                    if (random.NextDouble() < Constants.learnFromExpProbability && !ErrorMoves[i] &&
                        /*!AlreadyLearn.Contains(LastMovesDecidedActionsNum[i]) &&*/ !AllErrorMoves.Contains(LastMovesDecidedActionsNum[i]))
                    {
                        LearnFromExp(LastMovesInputs[i], LastMovesDecidedActionsNum[i]);
                        //AlreadyLearn.Add(LastMovesDecidedActionsNum[i]);
                    }
                }
            }
        }

        private void LearnFromExp(double[] inputs, int correctTarget)
        {
            double[] targets = new double[layers[^1].size];
            targets[correctTarget] = 1;

            FeedForward(inputs);
            BackPropagation(targets);
        }

        private void LearnErrorFromExp(double[] inputs, int[] errorTarget)
        {
            double[] targets = new double[layers[^1].size];
            int i = 0;
            do
            {
                i = random.Next(0, 32);
            } while (errorTarget.Contains(i));

            targets[i] = 1;

            FeedForward(inputs);
            BackPropagation(targets);
        }

        private List<int> LookingForErrorMovesAtTurn(double[] LastMovesInputs)
        {
            List<int> AllErrorMoves = new List<int>();
            if (LastMovesInputs != null)
            {
                //Photo
                if (LastMovesInputs[112] != 1)
                {
                    AllErrorMoves.Add(20);
                }

                //Absorb
                double energyVal = 0;
                for (int i = 48; i < 56; i++)
                {
                    energyVal += LastMovesInputs[i];
                }
                if (energyVal <= 0)
                {
                    AllErrorMoves.Add(21);
                }

                //Bite
                if (LastMovesInputs[16] <= 3)
                {
                    AllErrorMoves.Add(12);
                }
                if (LastMovesInputs[23] <= 3)
                {
                    AllErrorMoves.Add(13);
                }
                if (LastMovesInputs[29] <= 3)
                {
                    AllErrorMoves.Add(14);
                }
                if (LastMovesInputs[30] <= 3)
                {
                    AllErrorMoves.Add(15);
                }
                if (LastMovesInputs[31] <= 3)
                {
                    AllErrorMoves.Add(16);
                }
                if (LastMovesInputs[24] <= 3)
                {
                    AllErrorMoves.Add(17);
                }
                if (LastMovesInputs[18] <= 3)
                {
                    AllErrorMoves.Add(18);
                }
                if (LastMovesInputs[17] <= 3)
                {
                    AllErrorMoves.Add(19);
                }

                //Move
                if (LastMovesInputs[16] != 1)
                {
                    AllErrorMoves.Add(0);
                }
                if (LastMovesInputs[23] != 1)
                {
                    AllErrorMoves.Add(1);
                }
                if (LastMovesInputs[29] != 1)
                {
                    AllErrorMoves.Add(2);
                }
                if (LastMovesInputs[30] != 1)
                {
                    AllErrorMoves.Add(3);
                }
                if (LastMovesInputs[31] != 1)
                {
                    AllErrorMoves.Add(4);
                }
                if (LastMovesInputs[24] != 1)
                {
                    AllErrorMoves.Add(5);
                }
                if (LastMovesInputs[18] != 1)
                {
                    AllErrorMoves.Add(6);
                }
                if (LastMovesInputs[17] != 1)
                {
                    AllErrorMoves.Add(7);
                }

                //Jump
                if (LastMovesInputs[21] != 1)
                {
                    AllErrorMoves.Add(8);
                }
                if (LastMovesInputs[44] != 1)
                {
                    AllErrorMoves.Add(9);
                }
                if (LastMovesInputs[26] != 1)
                {
                    AllErrorMoves.Add(10);
                }
                if (LastMovesInputs[3] != 1)
                {
                    AllErrorMoves.Add(11);
                }
            }
            return AllErrorMoves;
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
            double beta1 = 0.9;
            double beta2 = 0.999;
            double epsilon = 1e-6;
            double rho = 0.9;
            double clippingThreshold = 5.0;

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

            double mHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta1, t)) / (1.0 - beta1t);
            double vHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta2, t)) / (1.0 - beta2t);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double mHat = m[i, j] / mHatCorrection;
                    double vHat = v[i, j] / Math.Sqrt(vHatCorrection);

                    double rhoInf = 2.0 / (1.0 - rho) - 1.0;
                    double adapt = Math.Max(0, rhoInf - 2.0 * t * Math.Pow(beta2t, 2));

                    double stepSize = adjustedLearningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);
                    double denom = Math.Sqrt(vHat) + epsilon;
                    double update = stepSize * mHat / denom;

                    update += adapt * stepSize * mHat;

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
            double rho = 0.9;

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

            double mHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta1, t)) / (1.0 - beta1t);
            double vHatCorrection = Math.Sqrt(1.0 - Math.Pow(beta2, t)) / (1.0 - beta2t);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double mHat = m[i, j] / mHatCorrection;
                    double vHat = v[i, j] / Math.Sqrt(vHatCorrection);

                    double rhoInf = 2.0 / (1.0 - rho) - 1.0;
                    double adapt = Math.Max(0, rhoInf - 2.0 * t * Math.Pow(beta2t, 2));

                    double stepSize = Constants.learningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);
                    double denom = Math.Sqrt(vHat) + epsilon;
                    double update = stepSize * mHat / denom;

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

            double correction = Constants.learningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);

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
