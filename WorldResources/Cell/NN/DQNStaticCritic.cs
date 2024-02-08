using CellEvolution;
using static CellEvolution.Cell.NN.CellModel;
using System;
using System.Collections.Generic;
using System.Linq;
using СellEvolution.WorldResources.NN;

namespace СellEvolution.WorldResources.Cell.NN
{
    public class DQNStaticCritic
    {
        public bool IsDecidedMoveError(CellAction decidedAction, double[] LastInput)
        {
            List<CellAction> AllErrorMoves = LookingForErrorMovesAtTurn(LastInput);
            return AllErrorMoves.Contains(decidedAction);
        }

        private List<CellAction> LookingForErrorMovesAtTurn(double[] LastMovesInputs) //Input
        {
            List<CellAction> AllErrorMoves = new List<CellAction>();
            if (LastMovesInputs != null)
            {
                //Reproduction
                if (LastMovesInputs[156] < Normalizer.LogNormalize(Constants.cloneEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.Clone);
                    AllErrorMoves.Add(CellAction.Reproduction);

                    if (LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                    {
                        //Actions
                        AllErrorMoves.Add(CellAction.Slip);
                        AllErrorMoves.Add(CellAction.Hide);
                    }
                }

                //Photo
                if (LastMovesInputs[153] != Normalizer.LogNormalize(1 * Constants.brainInputDayNightPoweredK))
                {
                    AllErrorMoves.Add(CellAction.Photosynthesis);
                }

                //Absorb
                double energyVal = 0;
                for (int i = 144; i < 153; i++)
                {
                    energyVal += Normalizer.LogDenormalize(LastMovesInputs[i]);
                }
                if (energyVal <= 0)
                {
                    AllErrorMoves.Add(CellAction.Absorption);
                }

                //Bite
                if (LastMovesInputs[16] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeftUp);
                }
                if (LastMovesInputs[23] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteUp);
                }
                if (LastMovesInputs[29] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRightUp);
                }
                if (LastMovesInputs[30] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRight);
                }
                if (LastMovesInputs[31] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRightDown);
                }
                if (LastMovesInputs[24] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteDown);
                }
                if (LastMovesInputs[18] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeftDown);
                }
                if (LastMovesInputs[17] < Normalizer.LogNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeft);
                }

                //Move
                if (LastMovesInputs[16] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveLeftUp);
                }
                if (LastMovesInputs[23] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveUp);
                }
                if (LastMovesInputs[29] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveRightUp);
                }
                if (LastMovesInputs[30] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveRight);
                }
                if (LastMovesInputs[31] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveRightDown);
                }
                if (LastMovesInputs[24] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveDown);
                }
                if (LastMovesInputs[18] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveLeftDown);
                }
                if (LastMovesInputs[17] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2))
                {
                    AllErrorMoves.Add(CellAction.MoveLeft);
                }

                //Jump
                if (LastMovesInputs[21] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpUp);
                }
                if (LastMovesInputs[44] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpRight);
                }
                if (LastMovesInputs[26] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpDown);
                }
                if (LastMovesInputs[3] != Normalizer.LogNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.LogNormalize(Constants.actionEnergyCost * 2 + Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpLeft);
                }
            }
            return AllErrorMoves;
        }
    }
}
