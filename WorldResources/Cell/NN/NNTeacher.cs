using CellEvolution;
using static CellEvolution.Cell.NN.CellModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace СellEvolution.WorldResources.Cell.NN
{
    public class NNTeacher
    {
        private readonly Random random = new Random();
        private readonly NNCellBrain brain;

        private readonly double[][,] m;
        private readonly double[][,] v;
        private int t = 0;

        public NNTeacher(NNCellBrain brain)
        {
            this.brain = brain;
            m = new double[brain.layers.Length][,];
            v = new double[brain.layers.Length][,];

            for (int i = 0; i < brain.layers.Length; i++)
            {
                int rows = brain.layers[i].weights.GetLength(0);
                int cols = brain.layers[i].weights.GetLength(1);
                m[i] = new double[rows, cols];
                v[i] = new double[rows, cols];
            }
        }

        public void UseExpToLearn(bool IsErrorMove, double[][] LastMovesInputs, int[] LastMovesDecidedActionsNum, bool[] ErrorMoves)
        {
            if (IsErrorMove)
            {
                double[] inputs = LastMovesInputs[0];
                
                if (LastMovesDecidedActionsNum[0] != -1)
                {
                    // Learn from an imitation of other cells' moves
                    CellAction[] errorTarget = LookingForErrorMovesAtTurn(inputs).ToArray();
                    LearnErrorFromExp(inputs, errorTarget);
                }
            }
            else
            {
                double[] inputs = LastMovesInputs[0];
                int correctActionNum = LastMovesDecidedActionsNum[0];
                CellAction correctAction = (CellAction)correctActionNum;
                if (LastMovesDecidedActionsNum[0] != -1)
                {
                    // Learn from the non-error experience
                    LearnFromExp(inputs, correctAction);
                }
            }
        }

        public bool IsDecidedMoveError(CellAction decidedAction, double[] LastInput)
        {
            List<CellAction> AllErrorMoves = LookingForErrorMovesAtTurn(LastInput);
            return AllErrorMoves.Contains(decidedAction);
        }

        private void LearnFromExp(double[] inputs, CellAction correctTarget)
        {
            double[] targets = new double[brain.layers[^1].size];
            if ((int)correctTarget < brain.layers[^1].size)
            {
                targets[(int)correctTarget] = 1;

                brain.FeedForward(inputs);
                BackPropagation(targets);
            }
        }

        private void LearnErrorFromExp(double[] inputs, CellAction[] errorTarget)
        {
            double[] targets = new double[brain.layers[^1].size];
            int imitationRes = Imitation(inputs, errorTarget);
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
                } while (errorTarget.Contains((CellAction)i));

                targets[i] = 1;
            }

            brain.FeedForward(inputs);
            BackPropagation(targets);
        }

        private int Imitation(double[] LastMovesInput, CellAction[] errorTarget)
        {
            List<double> OtherCellsMovesAround = GetInfoFromMemoriesAboutCellsMove(LastMovesInput);

            for (int i = 0; i < OtherCellsMovesAround.Count; i++)
            {
                switch (OtherCellsMovesAround[i])
                {
                    case Constants.KabsorbCell:
                        if (!errorTarget.Contains(CellAction.Absorption))
                        {
                            return (int)CellAction.Absorption;
                        }
                        break;
                    case Constants.KphotoCell:
                        if (!errorTarget.Contains(CellAction.Photosynthesis))
                        {
                            return (int)CellAction.Photosynthesis;
                        }
                        break;
                    case Constants.KslipCell:
                        if (!errorTarget.Contains(CellAction.Slip))
                        {
                            return (int)CellAction.Slip;
                        }
                        break;
                    case Constants.KbiteCell:
                        List<CellAction> availableMoves = new List<CellAction>();
                        for (int j = (int)CellAction.BiteLeftUp; j <= (int)CellAction.BiteLeft; j++)
                        {
                            if (!errorTarget.Contains((CellAction)j))
                            {
                                availableMoves.Add((CellAction)j);
                            }
                        }
                        if (availableMoves.Count > 0)
                        {
                            return (int)availableMoves[random.Next(0, availableMoves.Count)];
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
            foreach (var cell in CellsAround)
            {
                res.Add(cell.CellK);
            }

            return res;
        }

        private List<CellAction> LookingForErrorMovesAtTurn(double[] LastMovesInputs) //Input
        {
            List<CellAction> AllErrorMoves = new List<CellAction>();
            if (LastMovesInputs != null)
            {
                //Reproduction
                if (LastMovesInputs[110] < Constants.cloneEnergyCost)
                {
                    AllErrorMoves.Add(CellAction.Clone);
                    AllErrorMoves.Add(CellAction.Reproduction);

                    if (LastMovesInputs[110] < Constants.actionEnergyCost * 2)
                    {
                        //Actions
                        AllErrorMoves.Add(CellAction.Slip);
                        AllErrorMoves.Add(CellAction.Shout);
                    }
                }

                //Photo
                if (LastMovesInputs[109] != 1 * Constants.brainInputDayNightPoweredK)
                {
                    AllErrorMoves.Add(CellAction.Photosynthesis);
                }

                //Absorb
                double energyVal = 0;
                for (int i = 96; i < 105; i++)
                {
                    energyVal += LastMovesInputs[i];
                }
                if (energyVal <= 0)
                {
                    AllErrorMoves.Add(CellAction.Absorption);
                }

                //Bite
                if (LastMovesInputs[16] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteLeftUp);
                }
                if (LastMovesInputs[23] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteUp);
                }
                if (LastMovesInputs[29] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteRightUp);
                }
                if (LastMovesInputs[30] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteRight);
                }
                if (LastMovesInputs[31] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteRightDown);
                }
                if (LastMovesInputs[24] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteDown);
                }
                if (LastMovesInputs[18] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteLeftDown);
                }
                if (LastMovesInputs[17] < Constants.KnewCell)
                {
                    AllErrorMoves.Add(CellAction.BiteLeft);
                }

                //Move
                if (LastMovesInputs[16] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveLeftUp);
                }
                if (LastMovesInputs[23] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveUp);
                }
                if (LastMovesInputs[29] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveRightUp);
                }
                if (LastMovesInputs[30] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveRight);
                }
                if (LastMovesInputs[31] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveRightDown);
                }
                if (LastMovesInputs[24] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveDown);
                }
                if (LastMovesInputs[18] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveLeftDown);
                }
                if (LastMovesInputs[17] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2)
                {
                    AllErrorMoves.Add(CellAction.MoveLeft);
                }

                //Jump
                if (LastMovesInputs[21] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(CellAction.JumpUp);
                }
                if (LastMovesInputs[44] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(CellAction.JumpRight);
                }
                if (LastMovesInputs[26] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(CellAction.JumpDown);
                }
                if (LastMovesInputs[3] != Constants.Kempty || LastMovesInputs[114] < Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost)
                {
                    AllErrorMoves.Add(CellAction.JumpLeft);
                }
            }
            return AllErrorMoves;
        }

        private void BackPropagation(double[] targets)
        {
            double learningRate = Constants.learningRate;
            int outputErrorSize = brain.layers[^1].size;
            double[] outputErrors = new double[outputErrorSize];

            // Расчет ошибки на выходном слое
            for (int i = 0; i < outputErrorSize; i++)
            {
                outputErrors[i] = targets[i] - brain.layers[^1].neurons[i];
            }

            // Обратное распространение ошибки
            for (int k = brain.layers.Length - 2; k >= 0; k--)
            {
                NNLayers RightLayer = brain.layers[k + 1];
                NNLayers LeftLayer = brain.layers[k];

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
                        gradient *= learningRate;

                        // L2-регуляризация
                        LeftLayer.weights[i, j] -= gradient * RightLayer.neurons[j] + Constants.l2RegularizationLambda * LeftLayer.weights[i, j];

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
                    LeftLayer.biases[i] += outputErrors[i] * learningRate;
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

        private double DsigmoidFunc(double x) => x * (1.0 - x);
    }
}
