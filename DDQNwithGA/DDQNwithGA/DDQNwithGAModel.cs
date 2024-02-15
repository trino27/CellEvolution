using EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN;
using EvolutionNetwork.DDQNwithGA.Interfaces;
using EvolutionNetwork.GenAlg;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EvolutionNetwork.DDQNwithGA
{
    public class DDQNwithGAModel
    {
        private readonly Random random = new Random();

        private IDDQNwithGACritic critic;
        private IDDQNwithGACustomRewardCalculator rewardCalculator;
        public HyperparameterGen Gen;

        // DQN
        private List<DQNMemory> memory = new List<DQNMemory>();
        public uint maxMemoryCapacity;

        //NeuroEvolution
        private List<double[][][]> gradientsHistory = new List<double[][][]>();
        public uint GradientHistoryUpdatePeriod { get; private set; } = 0;

        private int currentGradientHistoryUpdatePhase = 0;
        private uint maxGradientsHistoryCapacity;

        private bool IsGradientHistoryWritingAllow = false;

        private double successWeightsCornerProc = 95;

        public List<(int layer, int neuron, int gradientIndex)> SuccessWeights { get; private set; } = new List<(int layer, int neuron, int gradientIndex)>();

        public bool IsSuccessWeightsContainsActualInfo { get; private set; } = false;
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

        public DDQNwithGAModel(int[] layerSizes, uint maxMemoryCapacity)
        {
            layersSizes = new int[layerSizes.Length];
            Array.Copy(layerSizes, layersSizes, layersSizes.Length);
            if (layersSizes.Length < 2)
            {
                throw new ArgumentException("You should have at least input and output layers");
            }
            Gen = new HyperparameterGen();

            InitNetworkLayers();
            InitVelocities();
            this.maxMemoryCapacity = maxMemoryCapacity;
            maxGradientsHistoryCapacity = maxMemoryCapacity;
            currentGradientHistoryUpdatePhase = (int)GradientHistoryUpdatePeriod + 1;
        }

        public DDQNwithGAModel(DDQNwithGAModel original)
        {
            Gen = new HyperparameterGen(original.Gen);

            layersSizes = new int[original.layersSizes.Length];
            Array.Copy(original.layersSizes, layersSizes, layersSizes.Length);
            InitNetworkLayers();

            maxMemoryCapacity = original.maxMemoryCapacity;
            maxGradientsHistoryCapacity = original.maxGradientsHistoryCapacity;
            GradientHistoryUpdatePeriod = original.GradientHistoryUpdatePeriod;

            InheritMemory(original);
            InheritVelocities(original);
            InheritWeights(original);

            if (original.critic != null)
            {
                critic = original.critic;
            }
            if (original.rewardCalculator != null)
            {
                rewardCalculator = original.rewardCalculator;
            }
            currentGradientHistoryUpdatePhase = (int)GradientHistoryUpdatePeriod + 1;
        }

        public DDQNwithGAModel(DDQNwithGAModel mother, DDQNwithGAModel father)
        {
            if (random.Next(0, 2) == 0)
            {
                Gen = new HyperparameterGen(mother.Gen, father.Gen);

                layersSizes = new int[mother.layersSizes.Length];
                Array.Copy(mother.layersSizes, layersSizes, layersSizes.Length);
                InitNetworkLayers();

                maxMemoryCapacity = mother.maxMemoryCapacity;
                maxGradientsHistoryCapacity = mother.maxGradientsHistoryCapacity;
                GradientHistoryUpdatePeriod = mother.GradientHistoryUpdatePeriod;

                

                InheritMemory(mother);

                if (random.NextDouble() > Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.elitism])
                {
                    InheritVelocities(mother, father);
                    InheritWeights(mother, father);
                }
                else
                {
                    InheritVelocities(mother);
                    InheritWeights(mother);
                }

                if (mother.critic != null)
                {
                    critic = mother.critic;
                }
                if (mother.rewardCalculator != null)
                {
                    rewardCalculator = mother.rewardCalculator;
                }
            }
            else
            {
                Gen = new HyperparameterGen(father.Gen, mother.Gen);

                layersSizes = new int[father.layersSizes.Length];
                Array.Copy(father.layersSizes, layersSizes, layersSizes.Length);
                InitNetworkLayers();

                maxMemoryCapacity = father.maxMemoryCapacity;
                maxGradientsHistoryCapacity = father.maxGradientsHistoryCapacity;
                GradientHistoryUpdatePeriod = father.GradientHistoryUpdatePeriod;

                InheritMemory(father);
                if (random.NextDouble() > Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.elitism])
                {
                    InheritVelocities(father, mother);
                    InheritWeights(father, mother);
                }
                else
                {
                    InheritVelocities(father);
                    InheritWeights(father);
                }

                if (father.critic != null)
                {
                    critic = father.critic;
                }
                if (father.rewardCalculator != null)
                {
                    rewardCalculator = father.rewardCalculator;
                }
            }
            currentGradientHistoryUpdatePhase = (int)GradientHistoryUpdatePeriod + 1;
        }

        public int ChooseAction(double[] currentState, double targetValue)
        {
            targetValueBeforeAction = targetValue;
            beforeActionState = currentState;
            totalActionsNum++;

            int decidedAction;
            //// Эпсилон-жадный выбор

            if (random.NextDouble() < Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.epsilon]) // Исследование: случайный выбор действия
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

            if (critic != null)
            {
                IsActionError = critic.IsDecidedActionError(decidedAction, beforeActionState);
            }

            return decidedAction;
        }

        public double[] CreateMemoryInput(int memoryInputLength)
        {
            double[] res = new double[memoryInputLength];

            if (memory.Count >= memoryInputLength)
            {
                for (int i = 0; i < memoryInputLength; i++)
                {
                    res[i] = memory[i].DecidedAction + 1;
                }
            }
            else
            {
                for (int i = 0; i < memoryInputLength; i++)
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

        public void RegisterLastActionResult(double[] afterActionState, double episodeRewardTarget, double rewardTarget, double bonusReward = 0)
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

        public void InitCritic(IDDQNwithGACritic critic)
        {
            this.critic = critic ?? throw new ArgumentNullException(nameof(critic));
        }
        public void InitCritic(IDDQNwithGACustomRewardCalculator rewardCalculator)
        {
            this.rewardCalculator = rewardCalculator ?? throw new ArgumentNullException(nameof(rewardCalculator));
        }

        private void InheritWeights(DDQNwithGAModel original)
        {
            CopyNNLayers(original);
        }
        private void InheritWeights(DDQNwithGAModel mainParent, DDQNwithGAModel secondParent)
        {
            CopyNNLayers(mainParent); // Предполагается, что это метод глубокого копирования весов из mainParent

            List<(int layer, int neuron, int gradientIndex)> successfulWeightsMainParent;
            List<(int layer, int neuron, int gradientIndex)> successfulWeightsSecondParent;

            // Получаем индексы успешных весов от обоих родителей на основе процентиля изменений градиентов
            if (mainParent.IsSuccessWeightsContainsActualInfo)
            {
                successfulWeightsMainParent = mainParent.SuccessWeights;
            }
            else
            {
                successfulWeightsMainParent = mainParent.GetSuccessfulWeightsIndicesParallel(mainParent.successWeightsCornerProc);
            }

            if(secondParent.IsSuccessWeightsContainsActualInfo)
            {
                successfulWeightsSecondParent = secondParent.SuccessWeights;
            }
            else
            {
                successfulWeightsSecondParent = secondParent.GetSuccessfulWeightsIndicesParallel(secondParent.successWeightsCornerProc);
            }
            // Применяем успешные веса от обоих родителей к потомку
            foreach (var (layer, i, j) in successfulWeightsSecondParent)
            {
                // Проверяем, относится ли успешный вес к второму родителю
                if (successfulWeightsSecondParent.Contains((layer, i, j)) && !successfulWeightsMainParent.Contains((layer, i, j)))
                {
                    onlineLayers[layer].weights[j, i] = secondParent.onlineLayers[layer].weights[j, i];
                }
                // В противном случае вес уже скопирован от mainParent
            }
        }

        public void CreateSuccessfulWeights()
        {
            SuccessWeights = GetSuccessfulWeightsIndicesParallel(successWeightsCornerProc);
            IsSuccessWeightsContainsActualInfo = true;
        }
        public void ClearSuccessfulWeights()
        {
            SuccessWeights = new List<(int layer, int neuron, int gradientIndex)>();
            IsSuccessWeightsContainsActualInfo = false;
        }
        private List<(int layer, int neuron, int gradientIndex)> GetSuccessfulWeightsIndicesParallel(double percentile)
        {
            var gradientMagnitudes = new ConcurrentBag<(double magnitude, int layer, int neuron, int gradientIndex)>();

            Parallel.For(0, gradientsHistory.Count, epochIndex =>
            {
                Parallel.For(0, gradientsHistory[epochIndex].Length, layerIndex =>
                {
                    Parallel.For(0, gradientsHistory[epochIndex][layerIndex].Length, neuronIndex =>
                    {
                        for (int gradientIndex = 0; gradientIndex < gradientsHistory[epochIndex][layerIndex][neuronIndex].Length; gradientIndex++)
                        {
                            double gradient = gradientsHistory[epochIndex][layerIndex][neuronIndex][gradientIndex];
                            gradientMagnitudes.Add((Math.Abs(gradient), layerIndex, neuronIndex, gradientIndex));
                        }
                    });
                });
            });


            // Определяем порог успешности
            double threshold = CalculatePercentileThreshold(gradientMagnitudes.Select(x => x.magnitude).ToList(), percentile);

            // Фильтруем индексы весов, которые превышают порог успешности
            List<(int layer, int neuron, int gradientIndex)> successfulIndices = gradientMagnitudes
                .Where(x => x.magnitude > threshold)
                .Select(x => (x.layer, x.neuron, x.gradientIndex))
                .ToList();

            return successfulIndices;
        }


        private double CalculatePercentileThreshold(List<double> gradients, double percentile)
        {
            gradients.Sort();
            int N = gradients.Count;
            double n = (N - 1) * percentile / 100.0 + 1;
            // Если n целое число, возвращаем значение по этому индексу
            if (Math.Floor(n) == n)
            {
                return gradients[(int)n - 1];
            }
            // Иначе интерполируем между ближайшими значениями
            else
            {
                int k = (int)n;
                double d = n - k;
                return gradients[k - 1] + d * (gradients[k] - gradients[k - 1]);
            }
        }
        private void InheritMemory(DDQNwithGAModel original)
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
        private void InheritVelocities(DDQNwithGAModel mainParent, DDQNwithGAModel secondParent)
        {
            velocitiesWeights = new double[mainParent.velocitiesWeights.Length][][];
            for (int layer = 0; layer < mainParent.velocitiesWeights.Length; layer++)
            {
                velocitiesWeights[layer] = new double[mainParent.velocitiesWeights[layer].Length][];
                for (int neuron = 0; neuron < mainParent.velocitiesWeights[layer].Length; neuron++)
                {
                    velocitiesWeights[layer][neuron] = new double[mainParent.velocitiesWeights[layer][neuron].Length];
                    for (int i = 0; i < mainParent.velocitiesWeights[layer][neuron].Length; i++)
                    {
                        velocitiesWeights[layer][neuron][i] = Math.Max(Math.Abs(mainParent.velocitiesWeights[layer][neuron][i]), Math.Abs(secondParent.velocitiesWeights[layer][neuron][i]));
                    }
                }
            }

            velocitiesBiases = new double[mainParent.velocitiesBiases.Length][];
            for (int layer = 0; layer < mainParent.velocitiesBiases.Length; layer++)
            {
                for (int neuron = 0; neuron < mainParent.velocitiesBiases[layer].Length; neuron++)
                {
                    velocitiesBiases[layer] = new double[mainParent.velocitiesBiases[layer].Length];
                    velocitiesBiases[layer][neuron] = Math.Max(Math.Abs(mainParent.velocitiesBiases[layer][neuron]), Math.Abs(secondParent.velocitiesBiases[layer][neuron]));
                }
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
                tdTarget = reward + Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.discountFactor] * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
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
        private void UpdateGradientHistory(double[][][] epochGradientsForHistory)
        {
            gradientsHistory.Add(epochGradientsForHistory);
            if (gradientsHistory.Count > maxGradientsHistoryCapacity)
            {
                gradientsHistory.RemoveAt(0); // Удаляем самый старый опыт, чтобы не превышать максимальную емкость
            }
        }


        public void SetSuccessWeightsCornerProc(double proc)
        {
            if(proc > 100)
            {
                proc = 100;
            }
            if(proc < 0)
            {
                proc = 0;
            }

            successWeightsCornerProc = proc;
        }
        public void InitMaxGradientHistoryCapacity(uint maxCapacity)
        {
            maxGradientsHistoryCapacity = maxCapacity;
        }
        public void SetGradientsHistoryUpdatePeriod(uint period)
        {
            GradientHistoryUpdatePeriod = period;
            currentGradientHistoryUpdatePhase = (int)GradientHistoryUpdatePeriod + 1;
        }

        public void GradientHistoryWritingHandler()
        {
            if (GradientHistoryUpdatePeriod <= currentGradientHistoryUpdatePhase)
            {
                IsGradientHistoryWritingAllow = true;
                currentGradientHistoryUpdatePhase = 0;
            }
            else
            {
                IsGradientHistoryWritingAllow = false;
                currentGradientHistoryUpdatePhase++;
            }
        }

        private void TeachDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = FeedForward(stateInput, onlineLayers);
            GradientHistoryWritingHandler();
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

            // Array to store gradients for current epoch
            double[][][] epochGradientsForHistory = null;
            if (IsGradientHistoryWritingAllow)
            {
                epochGradientsForHistory = new double[onlineLayers.Length - 1][][];
                
            }

            for (int layerIndex = onlineLayers.Length - 2; layerIndex >= 0; layerIndex--)
            {
                NNLayers currentLayer = onlineLayers[layerIndex];
                NNLayers nextLayer = onlineLayers[layerIndex + 1];
                double[] errorsNext = new double[currentLayer.size];

                // Array to store gradients for current layer
                double[][] layerGradientsForHistory = null;
                if (IsGradientHistoryWritingAllow)
                {
                    layerGradientsForHistory = new double[nextLayer.size][];
                }
                for (int i = 0; i < nextLayer.size; i++)
                {
                    double gradient = errors[i] * DSwishActivation(nextLayer.neurons[i]);

                    // Array to store gradients for current neuron
                    double[] neuronGradientsForHistory = null;
                    if (IsGradientHistoryWritingAllow)
                    {
                        neuronGradientsForHistory = new double[currentLayer.size];
                    }

                    for (int j = 0; j < currentLayer.size; j++)
                    {
                        // Вычисление "чистого" градиента (без учета L2 регуляризации)
                        double pureGradient = gradient * currentLayer.neurons[j];
                        if (IsGradientHistoryWritingAllow)
                        {
                            neuronGradientsForHistory[j] = pureGradient;
                        }

                        // Применение L2 регуляризации к градиенту
                        double weightGradientWithL2 = pureGradient - lambdaL2 * currentLayer.weights[j, i];

                        // Обновление скорости и веса с учетом L2 регуляризации
                        velocitiesWeights[layerIndex][j][i] = momentum * velocitiesWeights[layerIndex][j][i] + learningRate * weightGradientWithL2;
                        currentLayer.weights[j, i] += velocitiesWeights[layerIndex][j][i];

                    }
                    // Обновление смещения с учетом момента
                    velocitiesBiases[layerIndex + 1][i] = momentum * velocitiesBiases[layerIndex + 1][i] + learningRate * gradient;
                    nextLayer.biases[i] += velocitiesBiases[layerIndex + 1][i];
                    if (IsGradientHistoryWritingAllow)
                    {
                        layerGradientsForHistory[i] = neuronGradientsForHistory;
                    }
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
                if (IsGradientHistoryWritingAllow)
                {
                    epochGradientsForHistory[layerIndex] = layerGradientsForHistory;
                }
            }
            if (IsGradientHistoryWritingAllow)
            {
                UpdateGradientHistory(epochGradientsForHistory);
                IsSuccessWeightsContainsActualInfo = false;
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
            double beta = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            double sigmoid = 1.0 / (1.0 + Math.Exp(-beta * x));
            return sigmoid + beta * x * sigmoid * (1 - sigmoid);
        }

        private double SwishActivation(double x)
        {
            double beta = Gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta];
            return x / (1.0 + Math.Exp(-beta * x));
        }
    }
}