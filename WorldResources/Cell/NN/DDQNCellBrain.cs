using CellEvolution.Cell.GenAlg;
using CellEvolution.Cell.NN;
using СellEvolution.WorldResources.Cell.NN;
using static CellEvolution.Cell.NN.CellModel;

namespace CellEvolution.WorldResources.Cell.NN
{
    public class DDQNCellBrain
    {
        private readonly Random random = new Random();

        private readonly DQNStaticCritic teacher = new DQNStaticCritic();
        private readonly CellModel cell;
        private CellGen gen;

        // Нейронная сеть
        private NNLayers[] onlineLayers;
        private NNLayers[] targetLayers;

        private double[][,] eWeights; // Следы элигибилити для весов
        private double[][] eBiases; // Следы элигибилити для смещений

        private readonly int[] layersSizes = { 128, 256, 256, 128, 128, 30 };

        //Adam
        private double[][,] m;
        private double[][,] v;
        private int t = 0;

        // Воспроизведение опыта
        private List<DQNMemory> memory = new List<DQNMemory>();

        private readonly double discountFactor = 0.9; // Коэффициент дисконтирования
        private double lambda = 0.9;

        private double totalReward = 0;
        private double totalMovesNum = 0;

        public bool IsErrorMove = false;

        private double[] afterMoveState;
        private double[] beforeMoveState;
        private double energyBeforeMove;
        private int action;

        public DDQNCellBrain(CellModel cell)
        {
            this.cell = cell;
            gen = new CellGen();

            InitNetwork();
        }

        public DDQNCellBrain(CellModel cell, DDQNCellBrain original)
        {
            this.cell = cell;
            gen = new CellGen(original.gen);

            InitNetwork();
            InitMemory(original);
        }

        public DDQNCellBrain(CellModel cell, DDQNCellBrain mother, DDQNCellBrain father)
        {
            this.cell = cell;
            gen = new CellGen(mother.gen, father.gen);

            InitNetwork();

            if (random.Next(0, 2) == 0)
            {
                InitMemory(mother);
            }
            else
            {
                InitMemory(father);
            }
        }

        private void InitMemory(DDQNCellBrain original)
        {
            memory = original.memory.Select(m => (DQNMemory)m.Clone()).ToList();
        }
        private void InitNetwork()
        {
            onlineLayers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                onlineLayers[i] = new NNLayers(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0);
            }

            targetLayers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                targetLayers[i] = new NNLayers(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0);
            }
            UpdateTargetNetwork();

            eWeights = new double[onlineLayers.Length][,];
            eBiases = new double[onlineLayers.Length][];

            for (int i = 0; i < onlineLayers.Length; i++)
            {
                int rows = onlineLayers[i].weights.GetLength(0);
                int cols = onlineLayers[i].weights.GetLength(1);
                eWeights[i] = new double[rows, cols];
                eBiases[i] = new double[cols]; // Предполагая, что размер биасов равен количеству нейронов в слое

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        eWeights[i][row, col] = 0; // Инициализация нулями
                    }
                }
                for (int j = 0; j < cols; j++)
                {
                    eBiases[i][j] = 0; // Инициализация нулями
                }
            }

            m = new double[onlineLayers.Length][,];
            v = new double[onlineLayers.Length][,];

            for (int i = 0; i < onlineLayers.Length; i++)
            {
                int rows = onlineLayers[i].weights.GetLength(0);
                int cols = onlineLayers[i].weights.GetLength(1);
                m[i] = new double[rows, cols];
                v[i] = new double[rows, cols];
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

        public CellAction ChooseAction()
        {
            energyBeforeMove = cell.Energy;
            beforeMoveState = CreateBrainInput();
            totalMovesNum++;

            List<CellAction> availableActions = new List<CellAction>();

            switch (gen.GetCurrentGenAction())
            {
                case CellGen.GenAction.Move:
                    {
                        for (int i = (int)CellAction.MoveLeftUp; i <= (int)CellAction.JumpLeft; i++)
                        {
                            availableActions.Add((CellAction)i);
                        }
                    }
                    break;
                case CellGen.GenAction.Hunt:
                    {
                        for (int i = (int)CellAction.BiteLeftUp; i <= (int)CellAction.BiteLeft; i++)
                        {
                            availableActions.Add((CellAction)i);
                        }
                    }
                    break;
                case CellGen.GenAction.Photosynthesis:
                    {
                        availableActions.Add(CellAction.Photosynthesis);
                    }
                    break;
                case CellGen.GenAction.Absorption:
                    {
                        availableActions.Add(CellAction.Absorption);
                    }
                    break;
                case CellGen.GenAction.Reproduction:
                    {
                        availableActions.Add(CellAction.Reproduction);
                        availableActions.Add(CellAction.Clone);
                    }
                    break;

                case CellGen.GenAction.Actions:
                    {
                        availableActions.Add(CellAction.Slip);
                        availableActions.Add(CellAction.Hide);
                    }
                    break;
                case CellGen.GenAction.Mine:
                    {
                        availableActions.Add(CellAction.MineTop);
                        availableActions.Add(CellAction.MineRightSide);
                        availableActions.Add(CellAction.MineBottom);
                        availableActions.Add(CellAction.MineLeftSide);
                    }
                    break;

                case CellGen.GenAction.All:
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            availableActions.Add((CellAction)i);
                        }
                    }
                    break;
            }

            double[] qValuesOutput = FeedForwardWithNoise(beforeMoveState, onlineLayers);
            CellAction decidedAction = FindMaxIndexForFindAction(qValuesOutput, availableActions);
            action = (int)decidedAction;

            IsErrorMove = teacher.IsDecidedMoveError(decidedAction, beforeMoveState);

            return decidedAction;
        }
        private CellAction FindMaxIndexForFindAction(double[] array, List<CellAction> availableActions)
        {
            int maxIndex = (int)availableActions[random.Next(0, availableActions.Count)];
            double maxWeight = array[maxIndex];

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > maxWeight && availableActions.Contains((CellAction)i))
                {
                    maxWeight = array[i];
                    maxIndex = i;
                }
            }

            return (CellAction)maxIndex;
        }

        private double[] CreateBrainInput()
        {
            double[] inputsBrain = new double[onlineLayers[0].size];
            List<int> areaInfo = cell.GetWorldAroundInfo();
            double[] inputsMemory = CreateMemoryInput();
            double[] inputsFuture = gen.FutureGenActions(Constants.futureActionsInputLength);
            int j = 0;
            for (int i = 0; i < areaInfo.Count; i++) //48+48+9+1+1 = 0-47 48-95 96-104 105 106 
            {
                inputsBrain[j] = areaInfo[i];
                j++;
            }

            inputsBrain[j] = cell.Energy; //107
            j++;

            for (int i = 0; i < Constants.maxMemoryCapacity; i++) //108 - 123
            {
                inputsBrain[j] = inputsMemory[i];
                j++;
            }

            for (int i = 0; i < Constants.futureActionsInputLength; i++) //124 - 127
            {
                inputsBrain[j] = inputsFuture[i];
                j++;
            }

            return inputsBrain.ToArray();
        }
        private double[] CreateMemoryInput()
        {
            double[] res = new double[Constants.maxMemoryCapacity];

            if (memory.Count == Constants.maxMemoryCapacity)
            {
                for (int i = 0; i < Constants.maxMemoryCapacity; i++)  
                {
                    res[i] = (memory[i].DecidedAction + 1) * Constants.brainLastMovePoweredK;
                }
            }
            else
            {
                for (int i = 0; i < Constants.maxMemoryCapacity; i++)  
                {
                    res[i] = 0;
                }
                for (int i = 0; i < memory.Count; i++)  
                {
                        res[i] = (memory[i].DecidedAction + 1) * Constants.brainLastMovePoweredK;
                }
            }

            return res;
        }

        public void RegisterLastActionResult(int alreadyUseClones)
        {
            afterMoveState = CreateBrainInput();

            bool done = false;
            if (cell.IsCreatingClone || cell.IsCreatingChildren)
            {
                done = true;
            }

            if (done)
            {
                ActionHandler(CulcReward(done, alreadyUseClones), done);
                UpdateTargetNetwork();

            }
            else
            {
                ActionHandler(CulcReward(done, 0), done);
            }
        }

        private double CulcReward(bool done, int numOfClones)
        {
            double reward = 0;
            if (done)
            {
                reward = (totalReward / totalMovesNum) * numOfClones;

                totalReward = 0;
            }
            else
            {
                reward = cell.Energy - energyBeforeMove;

                if (IsErrorMove)
                {

                    reward -= Constants.errorCost;
                }
                else
                {
                    reward += Constants.actionEnergyCost;
                }
                totalReward += reward;
            }
            return reward;
        }
        private void ActionHandler(double reward, bool done)
        {
            // Используем текущее значение action, установленное в ChooseAction

            // Получаем текущие Q-значения для начального состояния с использованием онлайн-сети
            var currentQValuesOnline = FeedForwardWithNoise(beforeMoveState, onlineLayers);

            double tdTarget;
            if (!done)
            {
                // Получаем Q-значения для следующего состояния с использованием онлайн-сети для выбора действия
                var nextQValuesOnline = FeedForwardWithNoise(afterMoveState, onlineLayers);
                int nextAction = Array.IndexOf(nextQValuesOnline, nextQValuesOnline.Max()); // Выбор действия
                // Используем целевую сеть для оценки Q-значения для действия, выбранного с помощью онлайн-сети
                var nextQValuesTarget = FeedForward(afterMoveState, targetLayers);
                tdTarget = reward + discountFactor * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
            }
            else
            {
                tdTarget = reward; // Если эпизод завершен, цель равна полученной награде
            }

            // Подготовка массива целевых Q-значений для обучения
            var targetQValues = new double[currentQValuesOnline.Length];
            Array.Copy(currentQValuesOnline, targetQValues, currentQValuesOnline.Length);
            targetQValues[action] = tdTarget; // Обновляем Q-значение для выбранного действия

            // Обучаем онлайн-сеть с обновленными Q-значениями
            TeachDQNModel(beforeMoveState, targetQValues);

            // Обновляем память, добавляя новый опыт
            UpdateMemory(beforeMoveState, action, reward, afterMoveState, done);
        }

        // Обновление памяти новым опытом и обеспечение ее ограниченного размера
        private void UpdateMemory(double[] beforeMoveState, int action, double reward,  double[] afterMoveState, bool done)
        {
            memory.Add(new DQNMemory(beforeMoveState, action, reward, afterMoveState, done));
            if (memory.Count > Constants.maxMemoryCapacity)
            {
                memory.RemoveAt(0); // Удаляем самый старый опыт, чтобы не превышать максимальную емкость
            }
        }

        private void TeachDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = FeedForward(stateInput, onlineLayers); 
            BackPropagationWithAdam(predictedQValues, targetQValues);
        }
        private void BackPropagationWithAdam(double[] predicted, double[] targets)
        {
            int outputErrorSize = onlineLayers[^1].size;
            double[] outputErrors = new double[outputErrorSize];

            // Расчет ошибки на выходном слое
            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - predicted[i];
            }

            // Обратное распространение ошибки
            for (int k = onlineLayers.Length - 2; k >= 0; k--)
            {
                NNLayers RightLayer = onlineLayers[k + 1];
                NNLayers LeftLayer = onlineLayers[k];

                double[] errorsNext = new double[LeftLayer.size];

                for (int i = 0; i < LeftLayer.size; i++)
                {
                    double errorSum = 0;
                    for (int j = 0; j < RightLayer.size; j++)
                    {
                        errorSum += LeftLayer.weights[i, j] * outputErrors[j];
                    }
                    errorsNext[i] = errorSum * DsigmoidFunc(LeftLayer.neurons[i]);
                }
                outputErrors = errorsNext;

                // Градиенты и обновление весов
                for (int i = 0; i < LeftLayer.size; i++)
                {
                    for (int j = 0; j < RightLayer.size; j++)
                    {
                        double gradient = outputErrors[i] * DsigmoidFunc(LeftLayer.neurons[i]);
                        gradient *= Constants.learningRate;

                        LeftLayer.weights[i, j] -= gradient * RightLayer.neurons[j] * LeftLayer.weights[i, j];

                        // Обновление весов
                        double delta = gradient * RightLayer.neurons[j];
                        LeftLayer.weights[i, j] -= delta;

                        // Обновление для AdamOptimizer
                        UpdateAdamOptimizer(k, LeftLayer.weights, i, j, delta);
                    }
                }

                // Обновление смещений (biases)
                for (int i = 0; i < LeftLayer.size; i++)
                {
                    LeftLayer.biases[i] += outputErrors[i] * Constants.learningRate;
                }
            }

            for (int layer = onlineLayers.Length - 1; layer >= 0; layer--)
            {
                int nextLayerSize = layer == onlineLayers.Length - 1 ? 0 : onlineLayers[layer + 1].neurons.Length;
                for (int i = 0; i < onlineLayers[layer].neurons.Length; i++)
                {
                    double derivativeOfActivation = DsigmoidFunc(onlineLayers[layer].neurons[i]);

                    for (int j = 0; j < nextLayerSize; j++)
                    {
                        double error = outputErrors[j]; // Это ошибка следующего (правого) слоя, должна быть рассчитана заранее

                        // Обновление элигибилити трейсов
                        eWeights[layer][i, j] = lambda * eWeights[layer][i, j] + error * derivativeOfActivation * onlineLayers[layer].neurons[i];

                        // Обновление весов используя трейсы
                        onlineLayers[layer].weights[i, j] -= Constants.learningRate * eWeights[layer][i, j];
                    }

                    // Обновление элигибилити трейсов для биасов (если применимо)
                    eBiases[layer][i] = lambda * eBiases[layer][i] + outputErrors[i] * derivativeOfActivation; // Предполагая, что outputErrors[i] доступна для текущего слоя

                    // Обновление биасов используя трейсы
                    onlineLayers[layer].biases[i] -= Constants.learningRate * eBiases[layer][i];
                }

                // Пересчитать outputErrors для текущего слоя, используя веса и ошибки следующего слоя, если это не последний слой
                if (layer > 0)
                {
                    for (int i = 0; i < onlineLayers[layer - 1].neurons.Length; i++)
                    {
                        double errorSum = 0;
                        for (int j = 0; j < nextLayerSize; j++)
                        {
                            errorSum += onlineLayers[layer].weights[i, j] * outputErrors[j];
                        }
                        outputErrors[i] = errorSum * DsigmoidFunc(onlineLayers[layer - 1].neurons[i]);
                    }
                }
            }
        }
        private void UpdateAdamOptimizer(int layerIndex, double[,] weights, int i, int j, double delta)
        {
            double beta1 = 0.9;
            double beta2 = 0.999;
            double epsilon = 1e-8;

            m[layerIndex][i, j] = beta1 * m[layerIndex][i, j] + (1.0 - beta1) * delta;
            v[layerIndex][i, j] = beta2 * v[layerIndex][i, j] + (1.0 - beta2) * Math.Pow(delta, 2);

            t++;
            double beta1t = Math.Pow(beta1, t);
            double beta2t = Math.Pow(beta2, t);

            double correction = Constants.learningRate * Math.Sqrt(1.0 - beta2t) / (1.0 - beta1t);
            weights[i, j] -= correction * m[layerIndex][i, j] / (Math.Sqrt(v[layerIndex][i, j]) + epsilon);
        }

        public double[] FeedForward(double[] input, NNLayers[] layers)
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
        public double[] FeedForwardWithNoise(double[] input, NNLayers[] layers)
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
            NNLayers[] layersTemp = onlineLayers;
            Parallel.For(0, onlineLayers.Length, i =>
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

            onlineLayers = layersTemp;
        }
        public void Clone(DDQNCellBrain original)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }
        }
        public void Clone(DDQNCellBrain mainParent, DDQNCellBrain secondParent)
        {
            double key = random.NextDouble();

            CopyNNLayers(mainParent);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }

            gen = new CellGen(mainParent.gen, secondParent.gen);
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
                            l.weights[i, j] += Constants.cloneNoiseWeightsRate;
                        }
                        else
                        {
                            l.weights[i, j] -= Constants.cloneNoiseWeightsRate;
                        }
                    }
                }
            }
        }
        private void CopyNNLayers(DDQNCellBrain original)
        {
            for (int k = 0; k < onlineLayers.Length; k++)
            {
                Array.Copy(original.onlineLayers[k].weights, onlineLayers[k].weights, original.onlineLayers[k].weights.Length);
                Array.Copy(original.onlineLayers[k].neurons, onlineLayers[k].neurons, original.onlineLayers[k].neurons.Length);
                Array.Copy(original.onlineLayers[k].biases, onlineLayers[k].biases, original.onlineLayers[k].biases.Length);

                onlineLayers[k].size = original.onlineLayers[k].size;
                onlineLayers[k].nextSize = original.onlineLayers[k].nextSize;
            }
        }

        public CellGen GetGen()
        {
            return gen;
        }

        private double SigmoidFunc(double x) => 1.0 / (1.0 + Math.Exp(-x));
        private double DsigmoidFunc(double x) => x * (1.0 - x);
    }
}
