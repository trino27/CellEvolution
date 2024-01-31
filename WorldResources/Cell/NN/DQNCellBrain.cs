using CellEvolution.Cell.GenAlg;
using CellEvolution.Cell.NN;
using СellEvolution.WorldResources.Cell.NN;
using static CellEvolution.Cell.NN.CellModel;

namespace CellEvolution.WorldResources.Cell.NN
{
    public class DQNCellBrain
    {
        private readonly Random random = new Random();
        private readonly CellModel cell;
        private CellGen gen;

        // Нейронная сеть
        private NNLayers[] layers;
        private readonly int[] layersSizes = { 127, 256, 256, 128, 32 };

        //Adam
        private double[][,] m;
        private double[][,] v;
        private int t = 0;

        // Воспроизведение опыта
        private List<DQNMemory> memory = new List<DQNMemory>();

        private readonly double discountFactor = 0.9; // Коэффициент дисконтирования


        NNTeacher teacher = new NNTeacher();
        public bool IsErrorMove = false;



        private double[] afterMoveState;
        private double[] beforeMoveState;
        private double energyBeforeMove;
        private int action;

        public DQNCellBrain(CellModel cell)
        {
            this.cell = cell;
            gen = new CellGen();

            InitNetwork();
        }

        public DQNCellBrain(CellModel cell, DQNCellBrain original)
        {
            this.cell = cell;
            gen = new CellGen(original.gen);

            InitNetwork();
            InitMemory(original);
        }

        public DQNCellBrain(CellModel cell, DQNCellBrain mother, DQNCellBrain father)
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

        private void InitMemory(DQNCellBrain original)
        {
            memory = original.memory;
        }
        private void InitNetwork()
        {
            layers = new NNLayers[layersSizes.Length];
            for (int i = 0; i < layersSizes.Length; i++)
            {
                layers[i] = new NNLayers(layersSizes[i], i < layersSizes.Length - 1 ? layersSizes[i + 1] : 0);
            }
            m = new double[layers.Length][,];
            v = new double[layers.Length][,];

            for (int i = 0; i < layers.Length; i++)
            {
                int rows = layers[i].weights.GetLength(0);
                int cols = layers[i].weights.GetLength(1);
                m[i] = new double[rows, cols];
                v[i] = new double[rows, cols];
            }
        }

        public CellAction ChooseAction()
        {
            energyBeforeMove = cell.Energy;
            beforeMoveState = CreateBrainInput();

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
                        availableActions.Add(CellAction.Shout);
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
                        for (int i = 0; i < 32; i++)
                        {
                            availableActions.Add((CellAction)i);
                        }
                    }
                    break;
            }

            double[] qValuesOutput = FeedForwardWithNoise(beforeMoveState);
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
            double[] inputsBrain = new double[layers[0].size];
            List<int> areaInfo = cell.GetWorldAroundInfo();
            double[] inputsMemory = CreateMemoryInput();

            int j = 0;
            for (int i = 0; i < areaInfo.Count; i++) //48+48+9+4+1 = 0-47 48-95 96-104 105-108 109
            {
                inputsBrain[j] = areaInfo[i];
                j++;
            }

            inputsBrain[j] = cell.Energy; //110
            j++;

            for (int i = 0; i < Constants.maxMemoryCapacity; i++) //111 - 126
            {
                inputsBrain[j] = inputsMemory[i];
                j++;
            }


            return inputsBrain.ToArray();
        }
        private double[] CreateMemoryInput()
        {
            double[] res = new double[Constants.maxMemoryCapacity];

            if (memory.Count == Constants.maxMemoryCapacity)
            {
                for (int i = 0; i < Constants.maxMemoryCapacity; i++)  //111 - 126
                {
                    res[i] = (memory[i].DecidedAction + 1) * Constants.brainLastMovePoweredK;
                }
            }
            else
            {
                for (int i = 0; i < Constants.maxMemoryCapacity; i++)  //111 - 126
                {
                    res[i] = 0;
                }
                for (int i = 0; i < memory.Count; i++)  //111 - 126
                {
                    res[i] = (memory[i].DecidedAction + 1) * Constants.brainLastMovePoweredK;
                }
            }

            return res;
        }

        public void RegisterActionResult()
        {
            afterMoveState = CreateBrainInput();

            bool done = false;
            if (cell.IsCreatingClone || cell.IsCreatingChildren)
            {
                done = true;
            }

            ActionHandler(cell.Energy - energyBeforeMove, true);
        }

        private void ActionHandler(double reward, bool done)
        {
            // Расчет целевых Q-значений
            var targetQValues = FeedForward(beforeMoveState); // Предполагаемые Q-значения для текущего состояния

            double target;
            if (done)
            {
                // Если эпизод завершен, то целевое Q-значение равно только полученной награде
                target = reward;
            }
            else
            {
                // В противном случае, учитываем будущую награду и Q-значения следующего состояния
                var nextQValues = FeedForward(afterMoveState);
                target = reward + discountFactor * nextQValues.Max();
            }

            // Обновляем целевое Q-значение только для выбранного действия
            targetQValues[action] = target;

            // Обучение модели
            TeachDQNModel(beforeMoveState, targetQValues);

            // Добавляем опыт в список memory
            memory.Add(new DQNMemory(beforeMoveState, action, reward, afterMoveState, done));
            // Убедитесь, что memory не превышает заданную емкость
            if (memory.Count > Constants.maxMemoryCapacity)
            {
                memory.RemoveAt(0); // Удаляем самый старый опыт, если список переполнен
            }
        }

        private void TeachDQNModel(double[] stateInput, double[] targetQValues)
        {
            double[] predictedQValues = FeedForward(stateInput); // Получаем предсказанные Q-значения
            BackPropagation(predictedQValues, targetQValues); // Передаем и предсказанные, и целевые значения в BackPropagation
        }
        private void BackPropagation(double[] predicted, double[] targets)
        {
            int outputErrorSize = layers[^1].size;
            double[] outputErrors = new double[outputErrorSize];

            // Расчет ошибки на выходном слое
            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - predicted[i];
            }

            // Обратное распространение ошибки
            for (int k = layers.Length - 2; k >= 0; k--)
            {
                NNLayers RightLayer = layers[k + 1];
                NNLayers LeftLayer = layers[k];

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

                        // L2-регуляризация
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
        public void Clone(DQNCellBrain original)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }
        }
        public void Clone(DQNCellBrain mainParent, DQNCellBrain secondParent)
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
            foreach (var l in layers)
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
        private void CopyNNLayers(DQNCellBrain original)
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


        public CellGen GetGen()
        {
            return gen;
        }

        private double SigmoidFunc(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private double DsigmoidFunc(double x) => x * (1.0 - x);
    }
}
