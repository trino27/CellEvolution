using CellEvolution.Cell.GenAlg;
using System.Diagnostics.Metrics;
using СellEvolution.Cell.NN;

namespace CellEvolution.Cell.NN
{
    public class NNCellBrain
    {
        public readonly Random random = new Random();

        public readonly NNTeacher teacher;
        private readonly CellModel cell; 
        private CellGen gen;

        public bool IsErrorMove = false;

        private double[] inputs;

        public int[] LastMovesDecidedActionsNum = new int[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];
        public double[][] LastMovesInputs = new double[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime][];
        public bool[] ErrorMoves = new bool[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];

        private int[] layersSizes =
            {
                128,

                256,
                128,
                64,

                32
            }; //128 128 96 64 32

        public NNLayers[] layers;

        public NNCellBrain(CellModel cell)
        {
            NetworkInit();
            InitMemory();

            this.cell = cell; 
            gen = new CellGen();
            teacher = new NNTeacher(this);
        }

        private void InitMemory()
        {
            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                this.LastMovesDecidedActionsNum[i] = -1;
            }
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

        public CellGen GetGen()
        {
            return gen;
        }

        public void LearnWithTeacher()
        {
            teacher.UseExpToLearn(IsErrorMove, LastMovesInputs, LastMovesDecidedActionsNum, ErrorMoves);
        }

        private double GenerateRandomNoise()
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        public int ChooseAction() //MovesCode
        {
            List<int> availableActions = new List<int>();

            switch (gen.GetCurrentGenAction())
            {
                case CellGen.GenActions.Move:
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            availableActions.Add(i);
                        }
                    }
                    break;
                case CellGen.GenActions.Hunt:
                    {
                        for (int i = 12; i < 20; i++)
                        {
                            availableActions.Add(i);
                        }
                    }
                    break;
                case CellGen.GenActions.Photosynthesis:
                    {
                        availableActions.Add(20);
                    }
                    break;
                case CellGen.GenActions.Absorption:
                    {
                        availableActions.Add(21);
                    }
                    break;
                case CellGen.GenActions.Reproduction:
                    {
                        availableActions.Add(22);
                        availableActions.Add(23);
                    }
                    break;

                case CellGen.GenActions.Actions:
                    {
                        availableActions.Add(24);
                        availableActions.Add(25);
                    }
                    break;
                case CellGen.GenActions.Evolving:
                    {
                        for (int i = 28; i < 32; i++)
                        {
                            availableActions.Add(i);
                        }
                    }
                    break;

                case CellGen.GenActions.All:
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            availableActions.Add(i);
                        }
                    }
                    break;
            }

            inputs = CreateBrainInput();
            RegisterInput(inputs);
            
            double[] outputs = FeedForwardWithNoise(inputs);
            int decidedAction = FindMaxIndexForFindAction(outputs, availableActions);
            RegisterDecidedAction(decidedAction);
            RegisterErrorMove(teacher.IsDecidedMoveError(decidedAction, inputs));

            return decidedAction;
        }

        private int FindMaxIndexForFindAction(double[] array, List<int> availableActions)
        {
            int maxIndex = availableActions[random.Next(0, availableActions.Count)];
            double maxWeight = array[maxIndex];

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > maxWeight && availableActions.Contains(i))
                {
                    maxWeight = array[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }

        private double[] CreateBrainInput()
        {
            double[] inputsBrain = new double[layers[0].size];
            List<int> areaInfo = cell.GetWorldAroundInfo();

            int j = 0;
            for (int i = 0; i < areaInfo.Count; i++) //48+48+9+7 = 0-47 48-95 96-104 105-111 112
            {
                inputsBrain[j] = areaInfo[i];
                j++;
            }
            inputsBrain[j] = (cell.Initiation) * Constants.brainInputInitioationPoweredK; //113
            j++;
            inputsBrain[j] = (cell.Energy); //114
            j++;
            inputsBrain[j] = (cell.MaxClone) * Constants.brainInputMaxClonePoweredK; //115
            j++;
            inputsBrain[j] = (cell.CurrentAge) * Constants.brainInputCurrentAgePoweredK;  //116
            j++;
            inputsBrain[j] = (cell.EnergyBank) * Constants.brainInputEnergyBankPoweredK;  //117
            j++;
            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)  //127
            {
                inputsBrain[j] = LastMovesDecidedActionsNum[i];
                j++;
            }

            return inputsBrain.ToArray();
        }

        public void RegisterDecidedAction(int decidedAction)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                LastMovesDecidedActionsNum[i] = LastMovesDecidedActionsNum[i - 1];
            }

            LastMovesDecidedActionsNum[0] = decidedAction;
        }
        public void RegisterInput(double[] input)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                LastMovesInputs[i] = LastMovesInputs[i - 1];
            }

            LastMovesInputs[0] = input;
        }
        public void RegisterErrorMove(bool res)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                ErrorMoves[i] = ErrorMoves[i - 1];
            }
            IsErrorMove = res;
            ErrorMoves[0] = res;
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

        public void Clone(NNCellBrain original)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }

            gen = new CellGen(original.gen);
        }
        public void Clone(NNCellBrain mainParent, NNCellBrain secondParent)
        {
            double key = random.NextDouble();

            CopyNNLayers(mainParent);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }

            gen = new CellGen(mainParent.gen, secondParent.gen);
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

        private double SigmoidFunc(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private void RandomCloneNoise()
        {
            long NumOfAllWeights = 0;
            foreach (var l in layers)
            {
                NumOfAllWeights += l.weights.LongLength;
            }

            int NumOfChanging = random.Next(0, Convert.ToInt32(NumOfAllWeights * Constants.cloneNoiseWeightsChangeProc));

            for (int i = 0; i < NumOfChanging; i++)
            {
                int randLayer = random.Next(0, layers.Length - 1);
                int randWeightD1 = random.Next(0, layers[randLayer].size);
                int randWeightD2 = random.Next(0, layers[randLayer].nextSize);

                layers[randLayer].weights[randWeightD1, randWeightD2] = random.NextDouble();
            }
        }
    }
}
