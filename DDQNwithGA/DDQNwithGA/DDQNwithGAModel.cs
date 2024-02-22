using EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN;
using EvolutionNetwork.DDQNwithGA.Interfaces;
using EvolutionNetwork.GenAlg;
using static EvolutionNetwork.GenAlg.HyperparameterGen;

namespace EvolutionNetwork.DDQNwithGA
{
    public class DDQNwithGAModel
    {
        private readonly Random random = new Random();

        private IDDQNwithGACritic critic;
        private IDDQNwithGACustomRewardCalculator rewardCalculator;
        private IDDQNwithGACustomRemindExperiencesDefinder remindExperiencesDefinder;
        public HyperparameterGen Gen;

        // DQN
        private List<DQNMemory> memory = new List<DQNMemory>();
        private uint maxMemoryCapacity;
        private uint minMemoryToStartMemoryReplayLearning;
        private bool IsMemoryEnough = false;
        private uint batchMemorySize;

        // NN
        private NNLayers[] onlineLayers;
        private NNLayers[] targetLayers;
        private int[] layersSizes;

        //SGDMomentum
        private double[][][] velocitiesWeights;
        private double[][] velocitiesBiases;

        private double totalReward = 0;
        private double totalActionsNum = 0;

        private double[] afterActionState;
        private double[] beforeActionState;
        private double targetValueBeforeAction;
        private int action;

        //Critic
        public bool IsActionError = false;

        public DDQNwithGAModel(int[] layerSizes, Dictionary<GenHyperparameter, double> startHyperparameterScatterDict, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning = 0)
        {
            InitializeModel(layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
            Gen = new HyperparameterGen();
            Gen.StartHyperparameterScatter(startHyperparameterScatterDict);
            
        }

        public DDQNwithGAModel(int[] layerSizes, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning = 0, double startHyperparameterScatter = 0)
        {
            InitializeModel(layerSizes, maxMemoryCapacity, batchMemorySize, minMemoryToStartMemoryReplayLearning);
            Gen = new HyperparameterGen();
            Gen.StartHyperparameterScatter(startHyperparameterScatter);
        }

        public DDQNwithGAModel(DDQNwithGAModel original)
        {
            CopyFrom(original);
        }

        public DDQNwithGAModel(DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            // Выбор родителя, от которого будет взята основа конфигурации
            DDQNwithGAModel baseParent = random.Next(0, 2) == 0 ? mother : father;
            DDQNwithGAModel otherParent = baseParent == mother ? father : mother;

            MergeFrom(baseParent, otherParent);
        }

        // Вспомогательный метод для инициализации модели
        private void InitializeModel(int[] layerSizes, uint maxMemoryCapacity, uint batchMemorySize, uint minMemoryToStartMemoryReplayLearning)
        {
            ValidateLayerSizes(layerSizes);
            this.layersSizes = new int[layerSizes.Length];
            Array.Copy(layerSizes, this.layersSizes, layerSizes.Length);
            InitNetworkLayers();
            InitVelocities();
            this.maxMemoryCapacity = maxMemoryCapacity;
            this.batchMemorySize = batchMemorySize;
            this.minMemoryToStartMemoryReplayLearning = minMemoryToStartMemoryReplayLearning;
        }

        private void ValidateLayerSizes(int[] layerSizes)
        {
            if (layerSizes.Length < 2)
            {
                throw new ArgumentException("You should have at least input and output layers");
            }
        }

        // Вспомогательный метод для копирования
        private void CopyFrom(DDQNwithGAModel original)
        {
            Gen = new HyperparameterGen(original.Gen);
            layersSizes = (int[])original.layersSizes.Clone();

            batchMemorySize = original.batchMemorySize;
            maxMemoryCapacity = original.maxMemoryCapacity;
            minMemoryToStartMemoryReplayLearning = original.minMemoryToStartMemoryReplayLearning;

            // Переиспользование существующих методов для копирования состояний
            InheritMemory(original);
            InheritVelocities(original);
            InheritNetworkLayers(original);

            // Копирование ссылок на внешние зависимости
            critic = original.critic;
            rewardCalculator = original.rewardCalculator;
            remindExperiencesDefinder = original.remindExperiencesDefinder;

            IsMemoryEnough = original.IsMemoryEnough;

            // Обновление и обучение, если достаточно памяти
            UpdateAndTrainIfNeeded();
        }

        // Вспомогательный метод для слияния
        private void MergeFrom(DDQNwithGAModel baseParent, DDQNwithGAModel otherParent)
        {
            // Слияние генетических параметров
            Gen = new HyperparameterGen(baseParent.Gen, otherParent.Gen);
            layersSizes = (int[])baseParent.layersSizes.Clone();

            batchMemorySize = baseParent.batchMemorySize;
            maxMemoryCapacity = baseParent.maxMemoryCapacity;
            minMemoryToStartMemoryReplayLearning = baseParent.minMemoryToStartMemoryReplayLearning;

            // Наследование сети и скоростей
            InheritVelocities(baseParent);
            InheritNetworkLayers(baseParent);

            // Выбор зависимостей из основного родителя
            critic = baseParent.critic;
            rewardCalculator = baseParent.rewardCalculator;
            remindExperiencesDefinder = baseParent.remindExperiencesDefinder;

            IsMemoryEnough = baseParent.IsMemoryEnough;

            // Слияние памяти от обоих родителей
            InheritMemory(baseParent, otherParent);

            // Обновление и обучение, если достаточно памяти
            UpdateAndTrainIfNeeded();
        }

        // Вспомогательный метод для обновления сети и обучения
        private void UpdateAndTrainIfNeeded()
        {
            UpdateTargetNetwork();
            if (IsMemoryEnough)
            {
                TrainFromMemoryReplay();
            }
        }

        public int ChooseAction(double[] currentState, double targetValue)
        {
            targetValueBeforeAction = targetValue;
            beforeActionState = currentState;
            totalActionsNum++;

            const double remindProbability = 0.2;
            if (IsMemoryEnough && random.NextDouble() < remindProbability)
            {
                RemindSimilarExperiences(currentState);
            }


            int decidedAction = -1;
            //// Эпсилон-жадный выбор

            if (random.NextDouble() < Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.exploration]) // Исследование
            {
                double[] qValuesOutput = FeedForward(beforeActionState, onlineLayers);
                //// Эпсилон-жадный выбор
                if (decidedAction == -1)
                {
                    int randomIndex = random.Next(layersSizes[^1]);
                    decidedAction = randomIndex;
                }
                action = decidedAction;
            }
            else
            {
                double[] qValuesOutput = FeedForward(beforeActionState, onlineLayers);
                decidedAction = FindMaxIndexForFindAction(qValuesOutput);
            }

            if (critic != null)
            {
                IsActionError = critic.IsDecidedActionError(decidedAction, beforeActionState);
            }

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

        public void ActionResultHandler(double[] afterActionState, double episodeRewardTarget, double rewardTarget, double bonusReward = 0)
        {
            this.afterActionState = afterActionState;

            bool done = false;
            if (episodeRewardTarget > 0)
            {
                done = true;
            }


            double reward = 0;
            if (rewardCalculator == null)
            {
                reward += CalculateReward(done, episodeRewardTarget, rewardTarget, bonusReward);
            }
            else
            {
                reward += rewardCalculator.CalculateReward(done, episodeRewardTarget, rewardTarget, bonusReward);
            }
            reward = Normalizer.TanhNormalize(reward);


            if (memory.Count > minMemoryToStartMemoryReplayLearning)
            {
                IsMemoryEnough = true;
            }

            TrainOnline(reward, done);

            if (done)
            {

                if (!IsMemoryEnough)
                {
                    UpdateTargetNetwork();
                }
            }

            RegisterActionResult(reward, done);

        }
        private void RegisterActionResult(double reward, bool done)
        {
            UpdateMemory(beforeActionState, action, reward, afterActionState, done);
        }
        private void TrainFromMemoryReplay()
        {
            var random = new Random();
            var sampledExperiences = new List<DQNMemory>();

            for (int i = 0; i < batchMemorySize; i++)
            {
                int index = random.Next(memory.Count);
                sampledExperiences.Add(memory[index]);
            }

            foreach (var experience in sampledExperiences)
            {
                double[] state = experience.BeforeActionState;

                double[] targetQValues = TDLearningDDQN(experience.BeforeActionState, experience.Done, experience.Reward, experience.DecidedAction, experience.AfterActionState);

                TeachDDQNModel(state, targetQValues);
            }
        }
        private void TrainOnline(double reward, bool done)
        {
            double[] targetQValues = TDLearningDDQN(beforeActionState, done, reward, action, afterActionState);
            TeachDDQNModel(beforeActionState, targetQValues);
        }

        private double[] TDLearningDDQN(double[] state, bool done, double reward, int decidedAction, double[] nextState)
        {
            //TD Learning + DDQN
            double tdTarget;
            if (!done)
            {
                // Получаем Q-значения для следующего состояния с использованием онлайн-сети для выбора действия
                double[] nextQValuesOnline = FeedForward(nextState, onlineLayers);
                int nextAction = Array.IndexOf(nextQValuesOnline, nextQValuesOnline.Max()); // Выбор действия

                double[] nextQValuesTarget = FeedForward(nextState, targetLayers);
                tdTarget = reward + Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.discountFactor] * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
            }
            else
            {
                tdTarget = reward; // Если эпизод завершен, цель равна полученной награде
            }

            // Получаем текущие Q-значения для начального состояния с использованием онлайн-сети
            double[] currentQValuesOnline = FeedForwardWithNoise(state, onlineLayers);
            // Подготовка массива целевых Q-значений для обучения
            double[] targetQValues = new double[currentQValuesOnline.Length];
            Array.Copy(currentQValuesOnline, targetQValues, currentQValuesOnline.Length);
            targetQValues[decidedAction] = tdTarget; // Обновляем Q-значение для выбранного действия
            return targetQValues;
        }

        private void UpdateMemory(double[] beforeMoveState, int action, double reward, double[] afterMoveState, bool done)
        {
            memory.Add(new DQNMemory(beforeMoveState, action, reward, afterMoveState, done));
            if (memory.Count > maxMemoryCapacity)
            {
                memory.RemoveAt(0); // Удаляем самый старый опыт, чтобы не превышать максимальную емкость
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

        private void TeachDDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = FeedForward(stateInput, onlineLayers);
            BackPropagationSGDM(predictedQValues, targetQValues);
        }
        private void BackPropagationSGDM(double[] predicted, double[] targets)
        {
            double learningRate = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.learningRate];
            double lambdaL2 = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.lambdaL2]; // Коэффициент L2 регуляризации
            double momentum = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.momentumCoefficient]; // Коэффициент момента

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
                        // Вычисление "чистого" градиента (без учета L2 регуляризации)
                        double pureGradient = gradient * currentLayer.neurons[j];

                        // Применение L2 регуляризации к градиенту
                        double weightGradientWithL2 = pureGradient - lambdaL2 * currentLayer.weights[j, i];

                        // Обновление скорости и веса с учетом L2 регуляризации
                        velocitiesWeights[layerIndex][j][i] = momentum * velocitiesWeights[layerIndex][j][i] + learningRate * weightGradientWithL2;
                        currentLayer.weights[j, i] += velocitiesWeights[layerIndex][j][i];

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
                    layers[i].neurons[j] = activation + GenerateRandomNoise() * Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.noiseIntensity];
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
                    if (random.NextDouble() < Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.dropoutRate])
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

        private double DSwishActivation(double x)
        {
            double beta = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            double sigmoid = 1.0 / (1.0 + Math.Exp(-beta * x));
            return sigmoid + beta * x * sigmoid * (1 - sigmoid);
        }

        private double SwishActivation(double x)
        {
            double beta = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            return x / (1.0 + Math.Exp(-beta * x));
        }

        private double CalculateReward(bool done, double episodeSuccessValue, double targetValue, double bonus)
        {
            double reward = 0;
            if (done)
            {
                reward = totalReward / totalActionsNum;
                if (reward > 0)
                {
                    double episodeReward = 0;
                    for (int i = 0; i < episodeSuccessValue; i++)
                    {
                        episodeReward += reward * Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.genDoneBonusA] / Math.Pow(Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.genDoneBonusB], i);
                    }
                    reward = episodeReward;
                }
                else
                {
                    reward = 0;
                }
                reward += bonus;
                totalReward = 0;
                totalActionsNum = 0;
            }
            else
            {
                reward = targetValue - targetValueBeforeAction;

                if (IsActionError)
                {

                    reward -= Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.errorFine];
                }
                else
                {
                    reward += Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.correctBonus];
                }

                reward += bonus;

                totalReward += reward;
            }
            return reward;
        }

        // Метод для расчёта Евклидова расстояния между текущим и предыдущим состояниями
        private double CalculateEuclideanDistance(double[] currentState, double[] previousState)
        {
            double sum = 0.0;
            for (int i = 0; i < currentState.Length; i++)
            {
                sum += Math.Pow(currentState[i] - previousState[i], 2);
            }
            return Math.Sqrt(sum);
        }

        // Метод для расчёта Евклидова расстояния между текущим и предыдущим состояниями

        public void RemindSimilarExperiences(double[] currentState) // const
        {
            const double percentageOfSimilarExperiences = 0.02;

            int experiencesToConsider = (int)(memory.Count * percentageOfSimilarExperiences);

            if (experiencesToConsider > 0)
            {
                List<DQNMemory> mostSimilarExperiences = new List<DQNMemory>();
                if (remindExperiencesDefinder != null)
                {
                    mostSimilarExperiences = remindExperiencesDefinder.CustomRemindExperiencesDefinder(memory, currentState)
                        .OrderBy(experience => remindExperiencesDefinder.CustomCalculateSimilarityState(currentState, experience.BeforeActionState))
                        .Take(experiencesToConsider)
                        .ToList();
                }
                else
                {
                    mostSimilarExperiences = memory
                        .OrderBy(experience => CalculateEuclideanDistance(currentState, experience.BeforeActionState))
                        .Take(experiencesToConsider)
                        .ToList();
                }

                if (mostSimilarExperiences.Count > 0)
                {
                    int i = random.Next(0, mostSimilarExperiences.Count);
                    // Вычисляем и применяем обучение на основе целевых Q-значений, полученных из выбранных воспоминаний
                    double[] targetQValues = TDLearningDDQN(mostSimilarExperiences[i].BeforeActionState, mostSimilarExperiences[i].Done, mostSimilarExperiences[i].Reward, mostSimilarExperiences[i].DecidedAction, mostSimilarExperiences[i].AfterActionState);
                    // Обучаем модель, используя состояние до действия из воспоминания и вычисленные целевые Q-значения
                    TeachDDQNModel(mostSimilarExperiences[i].BeforeActionState, targetQValues);
                }
            }
        }

        public double[] CreateMemoryInput(int memoryInputLength)
        {
            double[] res = new double[memoryInputLength];

            if (memory.Count >= memoryInputLength)
            {
                for (int i = memoryInputLength - 1; i >= 0; i--)
                {
                    res[i] = memory[i].DecidedAction + 1;
                }
            }
            else
            {
                for (int i = memoryInputLength - 1; i >= 0; i--)
                {
                    res[i] = 0;
                }
                for (int i = memory.Count - 1; i >= 0; i--)
                {
                    res[i] = memory[i].DecidedAction + 1;
                }
            }

            return res;
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

        public void InitCritic(IDDQNwithGACritic critic)
        {
            this.critic = critic ?? throw new ArgumentNullException(nameof(critic));
        }
        public void InitRewardCalculator(IDDQNwithGACustomRewardCalculator rewardCalculator)
        {
            this.rewardCalculator = rewardCalculator ?? throw new ArgumentNullException(nameof(rewardCalculator));
        }
        public void InitRemindExperiencesDefinder(IDDQNwithGACustomRemindExperiencesDefinder remindExperiencesDefinder)
        {
            this.remindExperiencesDefinder = remindExperiencesDefinder ?? throw new ArgumentNullException(nameof(remindExperiencesDefinder));
        }

        private void InheritNetworkLayers(DDQNwithGAModel original)
        {
            InitNetworkLayers();
            CopyNNLayers(original);
        }

        private void InheritMemory(DDQNwithGAModel original)
        {
            memory = original.memory.Select(m => (DQNMemory)m.Clone()).ToList();
        }
        private void InheritMemory(DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            if (mother.memory.Count > father.memory.Count)
            {
                memory = mother.memory.Select(m => (DQNMemory)m.Clone()).ToList();
                for (int i = 0; i < father.memory.Count; i++)
                {
                    if (random.Next(0, 2) == 0)
                    {
                        if (memory.Count < i)
                        {
                            memory[i] = (DQNMemory)father.memory[i].Clone();
                        }
                        else
                        {
                            DQNMemory tamp = (DQNMemory)father.memory[i].Clone();
                            UpdateMemory(tamp.BeforeActionState, tamp.DecidedAction, tamp.Reward, tamp.AfterActionState, tamp.Done);
                        }
                    }
                }
            }
            else
            {
                memory = father.memory.Select(m => (DQNMemory)m.Clone()).ToList();
                for (int i = 0; i < mother.memory.Count; i++)
                {
                    if (random.Next(0, 2) == 0)
                    {
                        if (memory.Count < i)
                        {
                            memory[i] = (DQNMemory)mother.memory[i].Clone();
                        }
                        else
                        {
                            DQNMemory tamp = (DQNMemory)mother.memory[i].Clone();
                            UpdateMemory(tamp.BeforeActionState, tamp.DecidedAction, tamp.Reward, tamp.AfterActionState, tamp.Done);
                        }
                    }
                }
            }
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
        private void InheritVelocities(DDQNwithGAModel original)
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
    }
}