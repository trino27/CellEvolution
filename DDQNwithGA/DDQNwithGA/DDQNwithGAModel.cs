using EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN;
using EvolutionNetwork.GenAlg;
using System;


namespace EvolutionNetwork.DDQNwithGA
{
    public class DDQNwithGAModel
    {
        private readonly Random random = new Random();

        //private readonly NNStaticCritic teacher = new NNStaticCritic();
        public HyperparameterGen gen;

        // NN
        private NNLayers[] onlineLayers;
        private NNLayers[] targetLayers;
        private int[] layersSizes;

        //SGDMomentum
        private double[][][] velocitiesWeights;
        private double[][] velocitiesBiases;

        // DQN
        private List<DQNMemory> memory = new List<DQNMemory>();

        private double totalReward = 0;
        private double totalActionsNum = 0;

        public bool IsErrorMove = false;

        private double[] afterActionState;
        private double[] beforeActionState;
        private double targetValueBeforeAction;
        private int action;

        public uint maxMemoryCapacity;

        public DDQNwithGAModel(int[] layerSizes, uint maxMemoryCapacity)
        {
            layersSizes = new int[layerSizes.Length];
            Array.Copy(layerSizes, layersSizes, layersSizes.Length);
            if (layersSizes.Length < 2)
            {
                throw new ArgumentException("You should have at least input and output layers");
            }
            gen = new HyperparameterGen();

            InitNetworkLayers();
            InitVelocities();
            this.maxMemoryCapacity = maxMemoryCapacity;
        }

        public DDQNwithGAModel(DDQNwithGAModel original)
        {
            layersSizes = new int[original.layersSizes.Length];
            Array.Copy(original.layersSizes, layersSizes, layersSizes.Length);

            gen = new HyperparameterGen(original.gen);

            maxMemoryCapacity = original.maxMemoryCapacity;

            InitNetworkLayers();
            InitMemory(original);
            InitVelocities(original);
            Clone(original);
        }

        public DDQNwithGAModel(DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            gen = new HyperparameterGen(mother.gen, father.gen);

            if (random.Next(0, 2) == 0)
            {
                layersSizes = new int[mother.layersSizes.Length];
                Array.Copy(mother.layersSizes, layersSizes, layersSizes.Length);
                InitNetworkLayers();

                maxMemoryCapacity = mother.maxMemoryCapacity;

                InitMemory(mother);
                InitVelocities(mother);
                Clone(mother, father);
            }
            else
            {
                layersSizes = new int[father.layersSizes.Length];
                Array.Copy(father.layersSizes, layersSizes, layersSizes.Length);
                InitNetworkLayers();

                maxMemoryCapacity = father.maxMemoryCapacity;

                InitMemory(father);
                InitVelocities(father);
                Clone(father, mother);
            }

        }

        private void InitMemory(DDQNwithGAModel original)
        {
            memory = original.memory.Select(m => (DQNMemory)m.Clone()).ToList();
        }
        private void InitNetworkLayers()
        {
            onlineLayers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                onlineLayers[i] = new NNLayers(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0);
                onlineLayers[i].InitializeWeightsHe(); // Инициализация весов для онлайн слоев
            }

            targetLayers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                targetLayers[i] = new NNLayers(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0);
                targetLayers[i].InitializeWeightsHe(); // Инициализация весов для целевых слоев
            }
        }

        private void InitVelocities()
        {
            velocitiesWeights = new double[onlineLayers.Length - 1][][];
            velocitiesBiases = new double[onlineLayers.Length][];

            for (int i = 0; i < onlineLayers.Length; i++)
            {
                if (i < onlineLayers.Length - 1)
                {
                    velocitiesWeights[i] = new double[onlineLayers[i].size][];
                    for (int j = 0; j < onlineLayers[i].size; j++)
                    {
                        velocitiesWeights[i][j] = new double[onlineLayers[i + 1].size];
                        for (int k = 0; k < onlineLayers[i + 1].size; k++)
                        {
                            velocitiesWeights[i][j][k] = 0;
                        }
                    }
                }

                velocitiesBiases[i] = new double[onlineLayers[i].size];
                for (int j = 0; j < onlineLayers[i].size; j++)
                {
                    velocitiesBiases[i][j] = 0;
                }
            }
        }
        private void InitVelocities(DDQNwithGAModel original)
        {
            velocitiesWeights = new double[original.velocitiesWeights.Length][][];
            for (int layer = 0; layer < original.velocitiesWeights.Length; layer++)
            {
                velocitiesWeights[layer] = new double[original.velocitiesWeights[layer].Length][];
                for (int neuron = 0; neuron < original.velocitiesWeights[layer].Length; neuron++)
                {
                    velocitiesWeights[layer][neuron] = new double[original.velocitiesWeights[layer][neuron].Length];
                    Array.Copy(original.velocitiesWeights[layer][neuron], velocitiesWeights[layer][neuron], original.velocitiesWeights[layer][neuron].Length);
                }
            }

            velocitiesBiases = new double[original.velocitiesBiases.Length][];
            for (int layer = 0; layer < original.velocitiesBiases.Length; layer++)
            {
                velocitiesBiases[layer] = new double[original.velocitiesBiases[layer].Length];
                Array.Copy(original.velocitiesBiases[layer], velocitiesBiases[layer], original.velocitiesBiases[layer].Length);
            }
        }

        private void UpdateTargetNetwork()
        {
            for (int i = 0; i < onlineLayers.Length; i++)
            {
                Array.Copy(onlineLayers[i].weights, targetLayers[i].weights, onlineLayers[i].weights.Length);
                Array.Copy(onlineLayers[i].biases, targetLayers[i].biases, onlineLayers[i].biases.Length);
            }
        }

        public int ChooseAction(double[] currentState, double targetValue)
        {
            targetValueBeforeAction = targetValue;
            beforeActionState = currentState;
            totalActionsNum++;

            int decidedAction;
            //// Эпсилон-жадный выбор

            if (random.NextDouble() < gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.epsilon]) // Исследование: случайный выбор действия
            {
                int randomIndex = random.Next(layersSizes[^1]);
                decidedAction = randomIndex;
            }
            else
            {
                double[] qValuesOutput = FeedForward(beforeActionState, onlineLayers);
                decidedAction = FindMaxIndexForFindAction(qValuesOutput);

            }
            action = decidedAction;

            //IsErrorMove = teacher.IsDecidedMoveError(decidedAction, beforeActionState);

            return decidedAction;
        }
        private int FindMaxIndexForFindAction(double[] array)
        {
            int maxIndex = random.Next(0, layersSizes[^1]);
            double maxWeight = array[maxIndex];

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > maxWeight)
                {
                    maxWeight = array[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        public double[] CreateMemoryInput()
        {
            double[] res = new double[maxMemoryCapacity];

            if (memory.Count == maxMemoryCapacity)
            {
                for (int i = 0; i < maxMemoryCapacity; i++)
                {
                    res[i] = memory[i].DecidedAction + 1;
                }
            }
            else
            {
                for (int i = 0; i < maxMemoryCapacity; i++)
                {
                    res[i] = 0;
                }
                for (int i = 0; i < memory.Count; i++)
                {
                    res[i] = memory[i].DecidedAction + 1;
                }
            }

            return res;
        }

        public void RegisterLastActionResult(double[] afterActionState, double episodeSuccessValue, double targetValue, double correctActionBonus = 0)
        {
            this.afterActionState = afterActionState;

            bool done = false;
            if (episodeSuccessValue > 0)
            {
                done = true;
            }

            // Применение нормализации награды
            double reward = CulcReward(done, episodeSuccessValue, targetValue, correctActionBonus);
            reward = NormalizeReward(reward);

            if (done)
            {
                ActionHandler(reward, done);
                UpdateTargetNetwork();

            }
            else
            {
                ActionHandler(reward, done);
            }
        }

        private double CulcReward(bool done, double episodeSuccessValue, double targetValue, double correctActionBonus = 0)
        {
            double reward = 0;
            if (done)
            {
                reward = totalReward / totalActionsNum;
                if (reward > 0)
                {
                    double bonus = 0;
                    for (int i = 0; i < episodeSuccessValue; i++)
                    {
                        bonus += reward * gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.genDoneBonusA] / Math.Pow(gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.genDoneBonusB], i);
                    }
                    reward = bonus;
                }
                else
                {
                    reward = 0;
                }
                totalReward = 0;
            }
            else
            {
                reward = targetValue - targetValueBeforeAction;

                if (IsErrorMove)
                {

                    reward -= gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.errorCost];
                }
                else
                {
                    reward += correctActionBonus;
                }

                totalReward += reward;
            }
            return reward;
        }
        private void ActionHandler(double reward, bool done)
        {
            // Используем текущее значение action, установленное в ChooseAction

            // Получаем текущие Q-значения для начального состояния с использованием онлайн-сети
            double[] currentQValuesOnline = FeedForwardWithNoise(beforeActionState, onlineLayers);

            double tdTarget;
            if (!done)
            {
                // Получаем Q-значения для следующего состояния с использованием онлайн-сети для выбора действия
                double[] nextQValuesOnline = FeedForward(afterActionState, onlineLayers);
                int nextAction = Array.IndexOf(nextQValuesOnline, nextQValuesOnline.Max()); // Выбор действия

                double[] nextQValuesTarget = FeedForward(afterActionState, targetLayers);
                tdTarget = reward + gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.discountFactor] * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
            }
            else
            {
                tdTarget = reward; // Если эпизод завершен, цель равна полученной награде
            }

            // Подготовка массива целевых Q-значений для обучения
            double[] targetQValues = new double[currentQValuesOnline.Length];
            Array.Copy(currentQValuesOnline, targetQValues, currentQValuesOnline.Length);
            targetQValues[action] = tdTarget; // Обновляем Q-значение для выбранного действия

            // Обучаем онлайн-сеть с обновленными Q-значениями
            TeachDQNModel(beforeActionState, targetQValues);

            // Обновляем память, добавляя новый опыт
            UpdateMemory(beforeActionState, action, reward, afterActionState, done);
        }

        // Обновление памяти новым опытом и обеспечение ее ограниченного размера
        private void UpdateMemory(double[] beforeMoveState, int action, double reward, double[] afterMoveState, bool done)
        {
            memory.Add(new DQNMemory(beforeMoveState, action, reward, afterMoveState, done));
            if (memory.Count > maxMemoryCapacity)
            {
                memory.RemoveAt(0); // Удаляем самый старый опыт, чтобы не превышать максимальную емкость
            }
        }

        private void TeachDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = FeedForward(stateInput, onlineLayers);
            BackPropagationSGDM(predictedQValues, targetQValues);
        }

        private void BackPropagationSGDM(double[] predicted, double[] targets)
        {
            double learningRate = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.learningRate];
            double lambdaL2 = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.lambdaL2]; // Коэффициент L2 регуляризации
            double momentum = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.momentumCoefficient]; // Коэффициент момента

            double[] errors = new double[predicted.Length];
            for (int i = 0; i < predicted.Length; i++)
            {
                errors[i] = targets[i] - predicted[i];
            }

            for (int layerIndex = onlineLayers.Length - 2; layerIndex >= 0; layerIndex--)
            {
                NNLayers currentLayer = onlineLayers[layerIndex];
                NNLayers nextLayer = onlineLayers[layerIndex + 1];
                double[] errorsNext = new double[currentLayer.size];

                for (int i = 0; i < nextLayer.size; i++)
                {
                    double gradient = errors[i] * DSwishActivation(nextLayer.neurons[i]);

                    for (int j = 0; j < currentLayer.size; j++)
                    {
                        // Вычисление градиента с учетом L2 регуляризации и обновление скорости
                        double weightGradient = gradient * currentLayer.neurons[j] - lambdaL2 * currentLayer.weights[j, i];
                        velocitiesWeights[layerIndex][j][i] = momentum * velocitiesWeights[layerIndex][j][i] + learningRate * weightGradient;
                        currentLayer.weights[j, i] += velocitiesWeights[layerIndex][j][i]; // Обновление веса с учетом скорости
                    }
                    // Обновление смещения с учетом момента
                    velocitiesBiases[layerIndex + 1][i] = momentum * velocitiesBiases[layerIndex + 1][i] + learningRate * gradient;
                    nextLayer.biases[i] += velocitiesBiases[layerIndex + 1][i];
                }

                for (int i = 0; i < currentLayer.size; i++)
                {
                    errorsNext[i] = 0;
                    for (int j = 0; j < nextLayer.size; j++)
                    {
                        errorsNext[i] += currentLayer.weights[i, j] * errors[j];
                    }
                }
                errors = errorsNext;
            }
        }

        private double[] FeedForward(double[] input, NNLayers[] layers)
        {
            layers[0].neurons = input; // Инициализация входного слоя

            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i].size; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < layers[i - 1].size; k++)
                    {
                        sum += layers[i - 1].neurons[k] * layers[i - 1].weights[k, j];
                    }
                    layers[i].neurons[j] = SwishActivation(sum + layers[i].biases[j]);
                }
            }

            return layers[layers.Length - 1].neurons;
        }

        private double[] FeedForwardWithNoise(double[] input, NNLayers[] layers)
        {
            layers[0].neurons = input; // Инициализация входного слоя

            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i].size; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < layers[i - 1].size; k++)
                    {
                        sum += layers[i - 1].neurons[k] * layers[i - 1].weights[k, j];
                    }
                    double activation = SwishActivation(sum + layers[i].biases[j]);
                    // Добавление шума к результату активации
                    layers[i].neurons[j] = activation + GenerateRandomNoise() * gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.noiseIntensity];
                }

                layers[i].neurons = ApplyDropout(layers[i].neurons, i);
            }

            return layers[layers.Length - 1].neurons;
        }

        private double[] ApplyDropout(double[] activations, int layerIndex)
        {
            // Применяем дропаут только к скрытым слоям
            if (layerIndex > 0 && layerIndex < layersSizes.Length - 1)
            {
                for (int i = 0; i < activations.Length; i++)
                {
                    if (random.NextDouble() < gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.dropoutRate])
                    {
                        activations[i] = 0;
                    }
                }
            }
            return activations;
        }
        private double GenerateRandomNoise()
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public void Clone(DDQNwithGAModel original)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.cloneNoiseProbability])
            {
                RandomCloneNoise();
            }
        }
        public void Clone(DDQNwithGAModel mainParent, DDQNwithGAModel secondParent)
        {
            double key = random.NextDouble();

            CopyNNLayers(mainParent);

            if (key < gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.cloneNoiseProbability])
            {
                RandomCloneNoise();
            }

            gen = new HyperparameterGen(mainParent.gen, secondParent.gen);
        }

        private void RandomCloneNoise()
        {
            foreach (var l in onlineLayers)
            {
                for (int i = 0; i < l.size; i++)
                {
                    for (int j = 0; j < l.nextSize; j++)
                    {
                        if (random.Next(2) == 0)
                        {
                            l.weights[i, j] += gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.cloneNoiseWeightsRate];
                        }
                        else
                        {
                            l.weights[i, j] -= gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.cloneNoiseWeightsRate];
                        }
                    }
                }
            }
        }

        private void CopyNNLayers(DDQNwithGAModel original)
        {
            for (int k = 0; k < onlineLayers.Length; k++)
            {
                Array.Copy(original.onlineLayers[k].weights, onlineLayers[k].weights, original.onlineLayers[k].weights.Length);
                Array.Copy(original.onlineLayers[k].neurons, onlineLayers[k].neurons, original.onlineLayers[k].neurons.Length);
                Array.Copy(original.onlineLayers[k].biases, onlineLayers[k].biases, original.onlineLayers[k].biases.Length);

                onlineLayers[k].size = original.onlineLayers[k].size;
                onlineLayers[k].nextSize = original.onlineLayers[k].nextSize;
            }

            for (int k = 0; k < targetLayers.Length; k++)
            {
                Array.Copy(original.targetLayers[k].weights, targetLayers[k].weights, original.targetLayers[k].weights.Length);
                Array.Copy(original.targetLayers[k].neurons, targetLayers[k].neurons, original.targetLayers[k].neurons.Length);
                Array.Copy(original.targetLayers[k].biases, targetLayers[k].biases, original.targetLayers[k].biases.Length);

                targetLayers[k].size = original.targetLayers[k].size;
                targetLayers[k].nextSize = original.targetLayers[k].nextSize;
            }
        }

        private double DSwishActivation(double x)
        {
            double beta = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            double sigmoid = 1.0 / (1.0 + Math.Exp(-beta * x));
            return sigmoid + beta * x * sigmoid * (1 - sigmoid);
        }

        private double SwishActivation(double x)
        {
            double beta = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            return x / (1.0 + Math.Exp(-beta * x));
        }

        private double NormalizeReward(double reward)
        {
            return Math.Tanh(reward);
        }

        public double MinMaxNormalize(double value, double minValue, double maxValue)
        {
            if (value >= maxValue) return 1;
            if (value <= minValue) return 0;
            else return (value - minValue) / (maxValue - minValue);
        }

        public double MinMaxDenormalize(double normalizedValue, double minValue, double maxValue)
        {
            return normalizedValue * (maxValue - minValue) + minValue;
        }
    }
}