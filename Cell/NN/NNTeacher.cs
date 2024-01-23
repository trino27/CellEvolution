using CellEvolution;
using CellEvolution.Cell.NN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.Cell.NN
{
    public class NNTeacher
    {
        public readonly Random random = new Random();

        public readonly NNCellBrain brain;
        private int lastExpLearning = 0;

        public NNTeacher(NNCellBrain brain)
        {
            this.brain = brain;
        }

        public void UseExpToLearn(bool IsErrorMove, double[][] LastMovesInputs, int[] LastMovesDecidedActionsNum, bool[] ErrorMoves)
        {
            lastExpLearning++;
            if (IsErrorMove)
            {
                List<int> AllErrorMoves = LookingForErrorMovesAtTurn(LastMovesInputs[0]);
                LearnErrorFromExp(LastMovesInputs[0], AllErrorMoves.ToArray(), LastMovesInputs[0]);
            }
            if (lastExpLearning >= 16)
            {
                for (int i = 0; i < LastMovesInputs.Length; i++)
                {
                    List<int> AllErrorMoves = LookingForErrorMovesAtTurn(LastMovesInputs[i]);
                    if (random.NextDouble() < Constants.learnFromExpProbability && !ErrorMoves[i] &&
                         !AllErrorMoves.Contains(LastMovesDecidedActionsNum[i]))
                    {
                        LearnFromExp(LastMovesInputs[i], LastMovesDecidedActionsNum[i]);
                    }
                }
                lastExpLearning = 0;
            }
        }

        private void LearnFromExp(double[] inputs, int correctTarget)
        {
            double[] targets = new double[brain.layers[^1].size];
            targets[correctTarget] = 1;

            brain.FeedForward(inputs);
            BackPropagation(targets);
        }

        private void LearnErrorFromExp(double[] inputs, int[] errorTarget, double[] LastMovesInputs)
        {
            double[] targets = new double[brain.layers[^1].size];
            int imitationRes = Imitation(LastMovesInputs, errorTarget);
            if (imitationRes != -1)
            {
                targets[imitationRes] = 1;
            }
            else
            {
                int i = 0;
                do
                {
                    i = random.Next(0, 32);
                } while (errorTarget.Contains(i));

                targets[i] = 1;
            }

            brain.FeedForward(inputs);
            BackPropagation(targets);
        }

        private int Imitation(double[] LastMovesInput, int[] errorTarget) // MovesCode
        {
            List<double> OtherCellsMovesAround = GetInfoFromMemoriesAboutCellsMove(LastMovesInput);

            for (int i = 0; i < OtherCellsMovesAround.Count; i++)
            {
                switch (OtherCellsMovesAround[i])
                {
                    case Constants.KabsorbCell:
                        {
                            if (!errorTarget.Contains(21))
                            {
                                return 21;
                            }
                        }
                        break;
                    case Constants.KphotoCell:
                        {
                            if (!errorTarget.Contains(20))
                            {
                                return 20;
                            }
                        }
                        break;
                    case Constants.KslipCell:
                        {
                            if (!errorTarget.Contains(24))
                            {
                                return 24;
                            }
                        }
                        break;
                    case Constants.KevolvingCell:
                        {
                            List<int> availableMoves = new List<int>();
                            for (int j = 28; j < 32; j++)
                            {
                                if (!errorTarget.Contains(j))
                                {
                                    availableMoves.Add(j);
                                }
                            }
                            if (availableMoves.Count > 1)
                            {
                                return availableMoves[random.Next(0, availableMoves.Count)];
                            }
                        }
                        break;
                    case Constants.KbiteCell:
                        {
                            List<int> availableMoves = new List<int>();
                            for (int j = 12; j < 20; j++)
                            {
                                if (!errorTarget.Contains(j))
                                {
                                    availableMoves.Add(j);
                                }
                            }
                            if (availableMoves.Count > 1)
                            {
                                return availableMoves[random.Next(0, availableMoves.Count)];
                            }
                        }
                        break;
                }
            }


            return -1;
        }

        private List<double> GetInfoFromMemoriesAboutCellsMove(double[] LastMovesInput)
        {
            List<(double CellK, double genSimilarity)> CellsAround = new List<(double CellK, double genSimilarity)>();

            int visionDistAreaNum = (Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1;

            for (int i = 0; i < visionDistAreaNum; i++)
            {
                if (LastMovesInput[i] >= Constants.KnewCell && LastMovesInput[i] < Constants.KdeadCell)
                {
                    (double, double) temp = (LastMovesInput[i], LastMovesInput[i + visionDistAreaNum]);
                    CellsAround.Add(temp);
                }
            }

            CellsAround = CellsAround.OrderByDescending(item => item.genSimilarity).ToList();
            List<double> res = new List<double>();
            for (int i = 0; i < CellsAround.Count; i++)
            {
                res.Add(CellsAround[i].CellK);
            }


            return res;
        }

        private List<int> LookingForErrorMovesAtTurn(double[] LastMovesInputs) //Input
        {
            List<int> AllErrorMoves = new List<int>();
            if (LastMovesInputs != null)
            {
                //Reproduction
                if (LastMovesInputs[114] < Constants.cloneEnergyCost)
                {
                    AllErrorMoves.Add(22);
                    AllErrorMoves.Add(23);

                    if (LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                    {
                        //Actions
                        AllErrorMoves.Add(24);
                        AllErrorMoves.Add(25);

                        //Evolving
                        AllErrorMoves.Add(28);
                        AllErrorMoves.Add(29);
                        AllErrorMoves.Add(30);
                        AllErrorMoves.Add(31);
                    }
                }

                //Photo
                if (LastMovesInputs[112] != 1 * Constants.brainInputDayNightPoweredK)
                {
                    AllErrorMoves.Add(20);
                }

                //Absorb
                double energyVal = 0;
                for (int i = 96; i < 105; i++)
                {
                    energyVal += LastMovesInputs[i];
                }
                if (energyVal <= 0)
                {
                    AllErrorMoves.Add(21);
                }

                //Bite
                if (LastMovesInputs[16] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(12);
                }
                if (LastMovesInputs[23] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(13);
                }
                if (LastMovesInputs[29] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(14);
                }
                if (LastMovesInputs[30] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(15);
                }
                if (LastMovesInputs[31] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(16);
                }
                if (LastMovesInputs[24] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(17);
                }
                if (LastMovesInputs[18] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(18);
                }
                if (LastMovesInputs[17] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(19);
                }

                //Move
                if (LastMovesInputs[16] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(0);
                }
                if (LastMovesInputs[23] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(1);
                }
                if (LastMovesInputs[29] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(2);
                }
                if (LastMovesInputs[30] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(3);
                }
                if (LastMovesInputs[31] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(4);
                }
                if (LastMovesInputs[24] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(5);
                }
                if (LastMovesInputs[18] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(6);
                }
                if (LastMovesInputs[17] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(7);
                }

                //Jump
                if (LastMovesInputs[21] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(8);
                }
                if (LastMovesInputs[44] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(9);
                }
                if (LastMovesInputs[26] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(10);
                }
                if (LastMovesInputs[3] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(11);
                }
            }
            return AllErrorMoves;
        }

        public bool IsDecidedMoveError(int decidedAction, double[] LastInput)
        {
            List<int> AllErrorMoves = LookingForErrorMovesAtTurn(LastInput);
            return AllErrorMoves.Contains(decidedAction);
        }

        private void BackPropagation(double[] targets)
        {
            double learningRate = Constants.learningRate;

            int outputErrorSize = brain.layers[brain.layers.Length - 1].size;

            double[] outputErrors = new double[outputErrorSize];

            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - brain.layers[brain.layers.Length - 1].neurons[i];
            }

            for (int k = brain.layers.Length - 2; k >= 0; k--)
            {
                NNLayers l = brain.layers[k];
                NNLayers l1 = brain.layers[k + 1];

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
                    gradients[i] = outputErrors[i] * DsigmoidFunc(brain.layers[k + 1].neurons[i]);
                    gradients[i] *= learningRate;
                }

                ApplyL2Regularization(gradients, l1.weights);

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


                AdamOptimizer(l.weights, deltas, brain.layers.Length - 2 - k);

            }
        }

        private void ApplyL2Regularization(double[] gradients, double[,] weights)
        {
            double lambda = 0.1;

            int weightsSize = weights.GetLength(1);
            if (weightsSize != 0)
            {
                // Применение L2 регуляризации к градиентам
                for (int i = 0; i < gradients.Length; i++)
                {
                    gradients[i] += lambda * weights[i / weightsSize, i % weightsSize];
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

        private double DsigmoidFunc(double x) => x * (1.0 - x);

       
    }
}
