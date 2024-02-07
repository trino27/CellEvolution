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
        private readonly int[] layersSizes = { 177, 256, 256, 128, 128, 30 };

        //SGDMomentum
        private double[][][] velocitiesWeights;
        private double[][] velocitiesBiases;

        // Воспроизведение опыта
        private List<DQNMemory> memory = new List<DQNMemory>();

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
            InitVelocities();
        }

        public DDQNCellBrain(CellModel cell, DDQNCellBrain original)
        {
            this.cell = cell;
            gen = new CellGen(original.gen);

            InitNetwork();
            InitMemory(original);
            InitVelocities(original);
        }

        public DDQNCellBrain(CellModel cell, DDQNCellBrain mother, DDQNCellBrain father)
        {
            this.cell = cell;
            gen = new CellGen(mother.gen, father.gen);

            InitNetwork();

            if (random.Next(0, 2) == 0)
            {
                InitMemory(mother);
                InitVelocities(mother);
            }
            else
            {
                InitMemory(father);
                InitVelocities(father);
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
        }


        private void InitVelocities()
        {
            velocitiesWeights = new double[onlineLayers.Length - 1][][];
            velocitiesBiases = new double[onlineLayers.Length][];

            for (int i = 0; i < onlineLayers.Length; i++)
            {
                if (i < onlineLayers.Length - 1)
                {
                    velocitiesWeights[i] = new double[layersSizes[i]][];
                    for (int j = 0; j < layersSizes[i]; j++)
                    {
                        velocitiesWeights[i][j] = new double[layersSizes[i + 1]];
                        for (int k = 0; k < layersSizes[i + 1]; k++)
                        {
                            velocitiesWeights[i][j][k] = 0; // Инициализация нулями
                        }
                    }
                }

                velocitiesBiases[i] = new double[layersSizes[i]];
                for (int j = 0; j < layersSizes[i]; j++)
                {
                    velocitiesBiases[i][j] = 0; // Инициализация нулями
                }
            }
        }
        private void InitVelocities(DDQNCellBrain original)
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

            CellAction decidedAction;
            //// Эпсилон-жадный выбор
            if (random.NextDouble() < gen.HyperparameterChromosome[CellGen.GenHyperparameter.epsilon]) // Исследование: случайный выбор действия
            {
                int randomIndex = random.Next(availableActions.Count);
                decidedAction = availableActions[randomIndex];
            }
            else
            {
                double[] qValuesOutput = FeedForwardWithNoise(beforeMoveState, onlineLayers);
                decidedAction = FindMaxIndexForFindAction(qValuesOutput, availableActions);
            }
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
                if ((array[i] > maxWeight) && availableActions.Contains((CellAction)i))
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
            for (int i = 0; i < areaInfo.Count; i++) //0-47(-48)(areaChar) 48-95(-48)(cellsGen) 96-143(-48)(cellsEnergy) 144-152(-9)(energyArea) 153(-1)(DayTime) 154(-1)(Photosynthesis) 155(-1)(IsPoison)     
            {
                inputsBrain[j] = areaInfo[i];
                j++;
            }

            inputsBrain[j] = cell.Energy; //156
            j++;

            for (int i = 0; i < Constants.maxMemoryCapacity; i++) //157 - 172
            {
                inputsBrain[j] = inputsMemory[i];
                j++;
            }

            for (int i = 0; i < Constants.futureActionsInputLength; i++) //173 - 176
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
            if (cell.IsCreatingClone || cell.IsCreatingChildren && alreadyUseClones > 0)
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
                reward = (totalReward / totalMovesNum);
                if(reward > 0)
                {
                    double bonus = 0;
                    for(int i = 0; i < numOfClones; i++)
                    {
                        bonus +=  reward * gen.HyperparameterChromosome[CellGen.GenHyperparameter.genDoneBonusA] / Math.Pow(gen.HyperparameterChromosome[CellGen.GenHyperparameter.genDoneBonusB], i);
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
                reward = cell.Energy - energyBeforeMove;

                if (IsErrorMove)
                {

                    reward -= gen.HyperparameterChromosome[CellGen.GenHyperparameter.errorCost];
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
            double[] currentQValuesOnline = FeedForwardWithNoise(beforeMoveState, onlineLayers);

            double tdTarget;
            if (!done)
            {
                // Получаем Q-значения для следующего состояния с использованием онлайн-сети для выбора действия
                double[] nextQValuesOnline = FeedForwardWithNoise(afterMoveState, onlineLayers);
                int nextAction = Array.IndexOf(nextQValuesOnline, nextQValuesOnline.Max()); // Выбор действия
                // Используем целевую сеть для оценки Q-значения для действия, выбранного с помощью онлайн-сети
                double[] nextQValuesTarget = FeedForward(afterMoveState, targetLayers);
                tdTarget = reward + gen.HyperparameterChromosome[CellGen.GenHyperparameter.discountFactor] * nextQValuesTarget[nextAction]; // Используем уже выбранное действие
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
            TeachDQNModel(beforeMoveState, targetQValues);

            // Обновляем память, добавляя новый опыт
            UpdateMemory(beforeMoveState, action, reward, afterMoveState, done);
        }

        // Обновление памяти новым опытом и обеспечение ее ограниченного размера
        private void UpdateMemory(double[] beforeMoveState, int action, double reward, double[] afterMoveState, bool done)
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
            BackPropagationSGD(predictedQValues, targetQValues);
        }

        protected void BackPropagationSGD(double[] predicted, double[] targets)
        {
            int outputErrorSize = onlineLayers[onlineLayers.Length - 1].size;
            double[] outputErrors = new double[outputErrorSize];

            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - predicted[i];
            }

            for (int k = onlineLayers.Length - 2; k >= 0; k--)
            {
                NNLayers currentL = onlineLayers[k];
                NNLayers nextL = onlineLayers[k + 1];

                double[] errorsNext = new double[currentL.size];
                for (int i = 0; i < currentL.size; i++)
                {
                    double errorSum = 0;
                    for (int j = 0; j < nextL.size; j++)
                    {
                        errorSum += currentL.weights[i, j] * outputErrors[j];
                    }
                    errorsNext[i] = errorSum;
                }

                double[] gradients = new double[nextL.size];
                for (int i = 0; i < nextL.size; i++)
                {
                    gradients[i] = outputErrors[i] * DsigmoidFunc(nextL.neurons[i]);
                    gradients[i] *= gen.HyperparameterChromosome[CellGen.GenHyperparameter.learningRate];

                    // L2 regularization for biases is not typically applied, so biases are updated as before
                    velocitiesBiases[k + 1][i] = gen.HyperparameterChromosome[CellGen.GenHyperparameter.momentumCoefficient] * velocitiesBiases[k + 1][i] + gradients[i];
                    nextL.biases[i] += velocitiesBiases[k + 1][i];
                }

                for (int i = 0; i < currentL.size; i++)
                {
                    for (int j = 0; j < nextL.size; j++)
                    {
                        double weightGradient = gradients[j] * currentL.neurons[i];

                        // Applying L2 regularization to the weight update
                        weightGradient -= gen.HyperparameterChromosome[CellGen.GenHyperparameter.lambdaL2] * currentL.weights[i, j];  // Subtract the regularization term

                        // Update velocities with L2 regularization
                        velocitiesWeights[k][i][j] = gen.HyperparameterChromosome[CellGen.GenHyperparameter.momentumCoefficient] * velocitiesWeights[k][i][j] + weightGradient;
                        currentL.weights[i, j] += velocitiesWeights[k][i][j];
                    }
                }

                outputErrors = errorsNext;
            }
        }

        protected void BackPropagation(double[] predicted, double[] targets)
        {
            int outputErrorSize = onlineLayers[onlineLayers.Length - 1].size;

            double[] outputErrors = new double[outputErrorSize];

            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - predicted[i];
            }

            for (int k = onlineLayers.Length - 2; k >= 0; k--)
            {
                NNLayers currentL = onlineLayers[k];
                NNLayers nextL = onlineLayers[k + 1];

                double[] errorsNext = new double[currentL.size];
                // Обновим веса текущего слоя
                for (int i = 0; i < currentL.size; i++)
                {
                    double errorSum = 0;
                    for (int j = 0; j < nextL.size; j++)
                    {
                        errorSum += currentL.weights[i, j] * outputErrors[j];
                    }
                    errorsNext[i] = errorSum;
                }

                double[] gradients = new double[nextL.size];
                for (int i = 0; i < nextL.size; i++)
                {
                    gradients[i] = outputErrors[i] * DsigmoidFunc(onlineLayers[k + 1].neurons[i]);
                    gradients[i] *= gen.HyperparameterChromosome[CellGen.GenHyperparameter.learningRate];
                }

                double[,] deltas = new double[currentL.size, nextL.size];
                for (int i = 0; i < currentL.size; i++)
                {
                    for (int j = 0; j < nextL.size; j++)
                    {
                        deltas[i, j] = gradients[j] * currentL.neurons[i];
                    }

                }

                // Обновим смещения (biases) следующего слоя
                for (int i = 0; i < nextL.size; i++)
                {
                    nextL.biases[i] += gradients[i];
                }


                // Обновим ошибку для следующей итерации
                outputErrors = errorsNext;

                for (int i = 0; i < currentL.size; i++)
                {
                    for (int j = 0; j < nextL.size; j++)
                    {
                        currentL.weights[i, j] += deltas[i, j];
                    }
                }

            }
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

        public double[] FeedForwardWithNoise(double[] input, NNLayers[] layers) //!!
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
                    l1.neurons[j] += GenerateRandomNoise() * gen.HyperparameterChromosome[CellGen.GenHyperparameter.noiseIntensity];

                    l1.neurons[j] = SigmoidFunc(l1.neurons[j]);
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

            if (key < gen.HyperparameterChromosome[CellGen.GenHyperparameter.cloneNoiseProbability])
            {
                RandomCloneNoise();
            }
        }
        public void Clone(DDQNCellBrain mainParent, DDQNCellBrain secondParent)
        {
            double key = random.NextDouble();

            CopyNNLayers(mainParent);

            if (key < gen.HyperparameterChromosome[CellGen.GenHyperparameter.cloneNoiseProbability])
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
                            l.weights[i, j] += gen.HyperparameterChromosome[CellGen.GenHyperparameter.cloneNoiseWeightsRate];
                        }
                        else
                        {
                            l.weights[i, j] -= gen.HyperparameterChromosome[CellGen.GenHyperparameter.cloneNoiseWeightsRate];
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