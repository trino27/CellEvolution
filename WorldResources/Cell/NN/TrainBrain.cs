using CellEvolution;
using CellEvolution.Cell.GenAlg;
using CellEvolution.Cell.NN;
using static CellEvolution.Cell.NN.CellModel;

namespace СellEvolution.WorldResources.Cell.NN
{
    public class NNCellBrainTest
    {
        public readonly Random random = new Random();

        public readonly NNTeacherTest teacher;
        private readonly CellModel cell;
        private CellGen gen;

        public bool IsErrorMove = false;

        private double[] inputs;

        public int[] LastMovesDecidedActionsNum = new int[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];
        public double[][] LastMovesInputs = new double[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime][];
        public bool[] ErrorMoves = new bool[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];

        private int[] layersSizes =
            {
                127,

                256,
                256,
                128,

                32
            };

        public NNLayers[] layers;

        public NNCellBrainTest(CellModel cell)
        {
            NetworkInit();
            InitMemory();

            this.cell = cell;
            gen = new CellGen();
            teacher = new NNTeacherTest(this);
        }

        public NNCellBrainTest(CellModel cell, NNCellBrainTest original)
        {
            NetworkInit();
            InitMemory();

            this.cell = cell;
            gen = new CellGen(original.gen);
            teacher = new NNTeacherTest(this);
        }

        public NNCellBrainTest(CellModel cell, NNCellBrainTest mother, NNCellBrainTest father)
        {
            NetworkInit();
            InitMemory();

            this.cell = cell;
            gen = new CellGen(mother.gen, father.gen);
            teacher = new NNTeacherTest(this);
        }

        private void InitMemory()
        {
            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                LastMovesDecidedActionsNum[i] = -1;
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

        public CellAction ChooseAction()
        {
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

            inputs = CreateBrainInput();
            RegisterInput(inputs);

            double[] outputs = FeedForwardWithNoise(inputs);
            CellAction decidedAction = FindMaxIndexForFindAction(outputs, availableActions);
            RegisterDecidedAction(decidedAction);
            RegisterErrorMove(teacher.IsDecidedMoveError(decidedAction, inputs));

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

        private double[] NormalizeInputs(double[] inputs)
        {
            double minValue = inputs.Min();
            double maxValue = inputs.Max();

            double[] normalizedInputs = new double[inputs.Length];

            for (int i = 0; i < inputs.Length; i++)
            {
                normalizedInputs[i] = (inputs[i] - minValue) / (maxValue - minValue);
            }

            return normalizedInputs;
        }
        private double[] CreateBrainInput()
        {
            double[] inputsBrain = new double[layers[0].size];
            List<int> areaInfo = cell.GetWorldAroundInfo();

            int j = 0;
            for (int i = 0; i < areaInfo.Count; i++) //48+48+9+4+1 = 0-47 48-95 96-104 105-108 109
            {
                inputsBrain[j] = areaInfo[i];
                j++;
            }
            inputsBrain[j] = cell.Energy; //110
            j++;

            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)  //111 - 126
            {
                inputsBrain[j] = (LastMovesDecidedActionsNum[i] + 1) * Constants.brainLastMovePoweredK;
                j++;
            }

            return NormalizeInputs(inputsBrain.ToArray());
        }

        public void RegisterDecidedAction(CellAction decidedAction)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                LastMovesDecidedActionsNum[i] = LastMovesDecidedActionsNum[i - 1];
            }

            LastMovesDecidedActionsNum[0] = (int)decidedAction;
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

        public void Clone(NNCellBrainTest original)
        {
            double key = random.NextDouble();

            CopyNNLayers(original);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }
        }
        public void Clone(NNCellBrainTest mainParent, NNCellBrainTest secondParent)
        {
            double key = random.NextDouble();

            CopyNNLayers(mainParent);

            if (key < Constants.cloneNoiseProbability)
            {
                RandomCloneNoise();
            }

            gen = new CellGen(mainParent.gen, secondParent.gen);
        }

        private void CopyNNLayers(NNCellBrainTest original)
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

    }
}
