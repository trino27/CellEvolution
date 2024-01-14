namespace CellEvolution.Cell.NN
{
    public struct NNCellBrain
    {
        public readonly Random random = new Random();

        private int[] layersSizes =
            {
                128,

                256,
                128,
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
        public void RandomFillWeights()
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

            if(key < 0.04)
            {
                RandomChangingWeights();
            }
            else if(key > 0.04 && key < 0.07)
            {
                BackPropagationRandomTarget();
            }
            else if (key > 0.07 && key < 0.1)
            {
                RandomChangingWeights();
                BackPropagationRandomTarget();
            }
            else if (key > 0.1 && key < 0.13 && randomInputToClone != null)
            {
                FeedForward(randomInputToClone);
                BackPropagation(randomInputToClone);
            }
            else if (key > 0.13 && key < 0.16 && randomInputToClone != null)
            {
                FeedForward(randomInputToClone);
                RandomChangingWeights();
                BackPropagation(randomInputToClone);
            }
            else if (key > 0.16 && key < 0.19 && randomInputToClone != null)
            {
                FeedForward(randomInputToClone);
                BackPropagationRandomTarget();
            }
            else if (key > 0.19 && key < 0.22 && randomInputToClone != null)
            {
                FeedForward(randomInputToClone);
                RandomChangingWeights();
                BackPropagationRandomTarget();
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

        private void BackPropagationRandomTarget()
        {
            double learningRate = Constants.learningRateConst;

            double[] targets = new double[layers[^1].size];
            targets[random.Next(0, layers[^1].size)] = 1;

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
            }
        }
        public void BackPropagation(double[] targets)
        {
            double learningRate = Constants.learningRateConst;

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
            }
        }
        public void LearnFromExp(double[] inputs, int correctTarget)
        {
            double[] targets = new double[layers[^1].size];
            targets[correctTarget] = 1;

            FeedForward(inputs);
            BackPropagation(targets);

        }

        private double SigmoidFunc(double x) => 1.0 / (1.0 + Math.Exp(-x));
        private double DsigmoidFunc(double x) => x * (1.0 - x);
        
        private void RandomChangingWeights()
        {
            long NumOfAllWeights = 0;
            foreach (var l in layers)
            {
                NumOfAllWeights += l.weights.LongLength;
            }

            int NumOfChanging = random.Next(0, Convert.ToInt32(NumOfAllWeights / 10));

            for(int i = 0; i < NumOfChanging; i++)
            {
                int randLayer = random.Next(0, layers.Length - 1);
                int randWeightD1 = random.Next(0, layers[randLayer].size);
                int randWeightD2 = random.Next(0, layers[randLayer].nextSize);

                    layers[randLayer].weights[randWeightD1, randWeightD2] = random.NextDouble();
                
            }
        }
    }
}
