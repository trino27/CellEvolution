using CellEvolution;
using static CellEvolution.Cell.NN.CellModel;
using System;
using System.Collections.Generic;
using System.Linq;
using СellEvolution.WorldResources.NN;

namespace СellEvolution.WorldResources.Cell.NN
{
    public class NNStaticCritic
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
                if (LastMovesInputs[156] < Normalizer.EnergyNormalize((Constants.cloneEnergyCost + Constants.startCellEnergy)))      
                    {
                    AllErrorMoves.Add(CellAction.Clone);
                    AllErrorMoves.Add(CellAction.Reproduction);

                    if (LastMovesInputs[156] < Normalizer.EnergyNormalize((Constants.actionEnergyCost * 2)));
                    {
                        //Actions
                        AllErrorMoves.Add(CellAction.Slip);
                        AllErrorMoves.Add(CellAction.Hide);
                    }
                }

                //Photo
                if (LastMovesInputs[153] != 1)
                {
                    AllErrorMoves.Add(CellAction.Photosynthesis);
                }

                //Absorb
                double energyVal = 0;
                for (int i = 144; i < 153; i++)
                {
                    energyVal += Normalizer.EnergyNormalize(LastMovesInputs[i]);
                }
                if (energyVal <= 0)
                {
                    AllErrorMoves.Add(CellAction.Absorption);
                }

                //Bite
                if (LastMovesInputs[16] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeftUp);
                }
                if (LastMovesInputs[23] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteUp);
                }
                if (LastMovesInputs[29] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRightUp);
                }
                if (LastMovesInputs[30] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRight);
                }
                if (LastMovesInputs[31] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteRightDown);
                }
                if (LastMovesInputs[24] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteDown);
                }
                if (LastMovesInputs[18] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeftDown);
                }
                if (LastMovesInputs[17] < Normalizer.CharNormalize(Constants.KnewCell))
                {
                    AllErrorMoves.Add(CellAction.BiteLeft);
                }

                //Move
                if (LastMovesInputs[16] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveLeftUp);
                }
                if (LastMovesInputs[23] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveUp);
                }
                if (LastMovesInputs[29] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveRightUp);
                }
                if (LastMovesInputs[30] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveRight);
                }
                if (LastMovesInputs[31] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveRightDown);
                }
                if (LastMovesInputs[24] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveDown);
                }
                if (LastMovesInputs[18] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveLeftDown);
                }
                if (LastMovesInputs[17] != Normalizer.CharNormalize(Constants.Kempty))
                {
                    AllErrorMoves.Add(CellAction.MoveLeft);
                }

                //Jump
                if (LastMovesInputs[21] != Normalizer.CharNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.EnergyNormalize(Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpUp);
                }
                if (LastMovesInputs[44] != Normalizer.CharNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.EnergyNormalize(Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpRight);
                }
                if (LastMovesInputs[26] != Normalizer.CharNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.EnergyNormalize(Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpDown);
                }
                if (LastMovesInputs[3] != Normalizer.CharNormalize(Constants.Kempty) || LastMovesInputs[156] < Normalizer.EnergyNormalize(Constants.jumpEnergyCost))
                {
                    AllErrorMoves.Add(CellAction.JumpLeft);
                }
            }
            return AllErrorMoves;
        }
    }
}
