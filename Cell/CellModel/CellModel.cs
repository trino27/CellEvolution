﻿using System.Diagnostics.Metrics;
using СellEvolution.WorldResources;
using static CellEvolution.Cell.GenAlg.CellGen;

namespace CellEvolution.Cell.NN
{
    public partial class CellModel
    {
        private Random random = new Random();

        private readonly NNCellBrain brain;
        private readonly World world;

        private readonly Guid id;
        private readonly int generationNum = 0;
        private int[] specie = new int[1];

        private Dictionary<CellAction, Action> actionDictionary;

        public int MaxClone = 4;
        public int AlreadyUseClone = 0;

        public int PositionX;
        public int PositionY;

        public int Energy = Constants.startCellEnergy;
        public int EnergyBank = 0;

        public int Initiation = 1;

        public int CurrentAge = 0;

        public ConsoleColor CellColor = Constants.newCellColor;

        public bool IsCorpseEaten = false;
        public bool IsDead = false;
        public bool IsSlip = false;
        public bool IsHide = false;

        public bool IsCreatingChildren = false;
        public bool IsCreatingClone = false;

        public CellModel(int positionX, int positionY, World world, int creationNum)
        {
            id = Guid.NewGuid();

            specie[0] = creationNum;

            this.world = world;

            brain = new NNCellBrain(this);
            brain.RandomFillWeightsParallel();

            ActionDictionaryInit();
            CellInit(positionX, positionY);
        }
        public CellModel(int positionX, int positionY, World world, CellModel original)
        {
            id = Guid.NewGuid();

            specie = original.specie;

            this.world = world;

            generationNum = original.generationNum + 1;

            brain = new NNCellBrain(this, original.brain);
            brain.Clone(original.brain);

            ActionDictionaryInit();
            CellInit(positionX, positionY, original.EnergyBank);
        }
        public CellModel(int positionX, int positionY, World world, CellModel mother, CellModel father)
        {
            id = Guid.NewGuid();

            CellModel mainParent;
            CellModel secondParent;

            if (random.Next(0, 2) == 0)
            {
                mainParent = mother;
                secondParent = father;
            }
            else
            {
                mainParent = father;
                secondParent = mother;
            }

            generationNum = mainParent.generationNum + 1;

            this.world = world;

            brain = new NNCellBrain(this, mother.brain, father.brain);
            brain.Clone(mainParent.brain, secondParent.brain);

            SpecieFromParentsInit(mother, father);
            ActionDictionaryInit();
            CellInit(positionX, positionY, mother.EnergyBank + father.EnergyBank + Constants.startCellEnergy * 2);
        }

        private void CellInit(int positionX, int positionY)
        {
            PositionX = positionX;
            PositionY = positionY;
        }
        private void CellInit(int positionX, int positionY, int energy)
        {
            PositionX = positionX;
            PositionY = positionY;

            Energy += energy;
        }
        private void SpecieFromParentsInit(CellModel mother, CellModel father)
        {
            int numSameSpecies = 0;
            for (int i = 0; i < mother.specie.Length; i++)
            {
                for (int j = 0; j < father.specie.Length; j++)
                {
                    if (mother.specie[i] == father.specie[j]) numSameSpecies++;
                }
            }

            specie = new int[father.specie.Length + mother.specie.Length - numSameSpecies];
            {
                int i = 0;
                for (int j = 0; j < father.specie.Length; j++)
                {
                    if (!specie.Contains(father.specie[j]))
                    {
                        specie[i] = father.specie[j];
                        i++;
                    }
                }
                for (int j = 0; j < mother.specie.Length; j++)
                {
                    if (!specie.Contains(mother.specie[j]))
                    {
                        specie[i] = mother.specie[j];
                        i++;
                    }
                }
            }
        }
        private void ActionDictionaryInit()
        {
            actionDictionary = new Dictionary<CellAction, Action>
        {
            { CellAction.MoveLeftUp, MoveLeftUp },
            { CellAction.MoveUp, MoveUp },
            { CellAction.MoveRightUp, MoveRightUp },
            { CellAction.MoveRight, MoveRight },
            { CellAction.MoveRightDown, MoveRightDown },
            { CellAction.MoveDown, MoveDown },
            { CellAction.MoveLeftDown, MoveLeftDown },
            { CellAction.MoveLeft, MoveLeft },
            { CellAction.JumpUp, JumpUp },
            { CellAction.JumpRight, JumpRight },
            { CellAction.JumpDown, JumpDown },
            { CellAction.JumpLeft, JumpLeft },
            { CellAction.BiteLeftUp, BiteLeftUp },
            { CellAction.BiteUp, BiteUp },
            { CellAction.BiteRightUp, BiteRightUp },
            { CellAction.BiteRight, BiteRight },
            { CellAction.BiteRightDown, BiteRightDown },
            { CellAction.BiteDown, BiteDown },
            { CellAction.BiteLeftDown, BiteLeftDown },
            { CellAction.BiteLeft, BiteLeft },
            { CellAction.Photosynthesis, Photosynthesis },
            { CellAction.Absorption, Absorption },
            { CellAction.Clone, Clone },
            { CellAction.Reproduction, Reproduction },
            { CellAction.Slip, Slip },
            { CellAction.Shout, Shout },
            { CellAction.Hide, Hide },
            { CellAction.Mine, Mine },
            { CellAction.GainInitiation, GainInitiation },
            { CellAction.GainMaxClone, GainMaxClone },
            { CellAction.GainEnergyBank, GainEnergyBank },
            { CellAction.DecEnergyBank, DecEnergyBank }
        };
        }

        public void MakeAction()
        {
            IsSlip = false;
            IsCreatingClone = false;
            IsHide = false;

            if (!IsCreatingChildren)
            {
                AlreadyUseClone = 0;
            }
            else if (AlreadyUseClone == MaxClone)
            {
                IsCreatingChildren = false;
                AlreadyUseClone = 0;
            }

            PerformAction(brain.ChooseAction());

            if (brain.IsErrorMove)
            {
                CellColor = Constants.errorCellColor;
            }

            brain.LearnWithTeacher();

            Energy -= IsSlip ? Constants.slipEnergyCost : Constants.actionEnergyCost;

            if (world.WorldArea.IsAreaPoisoned(PositionX, PositionY))
            {
                CurrentAge += Constants.poisonedDecLive;
            }

            if (CurrentAge >= Constants.liveTime)
            {
                IsDead = true;
            }
            else
            {
                if (!IsSlip)
                {
                    CurrentAge += Constants.actionLiveCost;
                }
                else
                {
                    CurrentAge += Constants.slipLiveCost;
                }

                IsDead = IsNoEnergy();
            }

            if (IsDead)
            {
                CellColor = Constants.deadCellColor;
            }
        }

        public List<int> GetWorldAroundInfo()
        {
            return world.WorldArea.GetInfoFromAreaToCellBrainInput(PositionX, PositionY);
        }

        public GenAction[] GetGenomeCycle()
        {
            return brain.GetGen().GenActionsCycle;
        }

        private void PerformAction(CellAction decidedAction) 
        {
            if (actionDictionary.TryGetValue(decidedAction, out var action))
            {
                action?.Invoke();
            }
        }

        private bool IsNoEnergy()
        {
            if (Energy < 0) return true;
            else return false;
        }

        //Evolving
        private void GainInitiation()
        {
            Initiation++;
            CellColor = Constants.evolvingCellColor;

        }
        private void GainMaxClone()
        {
            MaxClone++;
            CellColor = Constants.evolvingCellColor;
        }
        private void GainEnergyBank()
        {
            EnergyBank += Constants.energyBankChangeNum;
            CellColor = Constants.evolvingCellColor;
        }
        private void DecEnergyBank()
        {
            EnergyBank -= Constants.energyBankChangeNum;
            CellColor = Constants.evolvingCellColor;
        }

        //Move x0 y0 -------> areaMaxX
        //          |
        //          |
        //          |
        //          |
        //         \/areaMaxY
        private void MoveUp()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX, PositionY - 1)) PositionY--;

        }
        private void MoveDown()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX, PositionY + 1)) PositionY++;
        }
        private void MoveLeft()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX - 1, PositionY)) PositionX--;
        }
        private void MoveRight()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX + 1, PositionY)) PositionX++;
        }

        private void MoveLeftUp()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX - 1, PositionY - 1))
            {
                PositionX--;
                PositionY--;
            }
        }
        private void MoveRightUp()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX + 1, PositionY - 1))
            {
                PositionX++;
                PositionY--;
            }
        }
        private void MoveRightDown()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX + 1, PositionY + 1))
            {
                PositionX++;
                PositionY++;
            }
        }
        private void MoveLeftDown()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX - 1, PositionY + 1))
            {
                PositionX--;
                PositionY++;
            }
        }

        //Jump
        private void JumpUp()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX, PositionY - Constants.jumpDistance))
            {
                PositionY -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpRight()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX + Constants.jumpDistance, PositionY))
            {
                PositionX += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpDown()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX, PositionY + Constants.jumpDistance))
            {
                PositionY += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;

            }
        }
        private void JumpLeft()
        {
            if (world.cellActionHandler.IsMoveAvailable(PositionX - Constants.jumpDistance, PositionY))
            {
                PositionX -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }

        //Bite
        private void BiteLeftUp()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX - 1, PositionY - 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX - 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteUp()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX, PositionY - 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightUp()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX + 1, PositionY - 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX + 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRight()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX + 1, PositionY))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX + 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightDown()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX + 1, PositionY + 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX + 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteDown()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX, PositionY + 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeftDown()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX - 1, PositionY + 1))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX - 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeft()
        {
            if (world.cellActionHandler.IsVictimExists(PositionX - 1, PositionY))
            {
                world.cellActionHandler.CellHunt(this, world.GetCell(PositionX - 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
        }

        //Action
        private void Photosynthesis()
        {
            if (world.CurrentDayTime == World.DayTime.Day)
            {
                double proc = (double)world.Cells.Count * 100 / (double)((Constants.areaSizeX - 2) * (Constants.areaSizeY - 2));
                double addEnergy = Constants.minPhotosynthesis + Constants.maxPhotosynthesis / 100.0 * (100 - proc);

                int numOfCellsAround = world.WorldArea.GetNumOfLiveCellsAround(PositionX, PositionY);
                if (numOfCellsAround > Constants.availableCellNumAroundMax)
                {
                    addEnergy = addEnergy / ((numOfCellsAround - Constants.availableCellNumAroundMax) * (numOfCellsAround + 1 - Constants.availableCellNumAroundMax));
                }
                else if (numOfCellsAround < Constants.availableCellNumAroundMin)
                {
                    addEnergy += addEnergy / ((numOfCellsAround - Constants.availableCellNumAroundMin) * (numOfCellsAround - 1 - Constants.availableCellNumAroundMin));
                }
                Energy += (int)addEnergy;
                CellColor = Constants.photoCellColor;

            }
            else
            {
                double proc = (double)world.Cells.Count * 100 / (double)((Constants.areaSizeX - 2) * (Constants.areaSizeY - 2));
                double loseEnergy = Constants.minNightPhotosynthesisFine + Constants.maxNightPhotosynthesisFine / 100.0 * proc;
                Energy -= (int)loseEnergy;
                CellColor = Constants.photoCellColor;
            }
        }
        private void Absorption()
        {
            CellColor = Constants.absorbCellColor;
            world.cellActionHandler.CellAbsorb(this);
        }
        private void Mine()
        {
            CellColor = Constants.mineCellColor;
            world.cellActionHandler.CellMine(this);
        }

        private void Slip()
        {
            IsSlip = true;
            CellColor = Constants.slipCellColor;
        }
        private void Shout()
        {
            world.cellActionHandler.CellShout(this);
        }
        private void Hide()
        {
            IsHide = true;
            CellColor = Constants.hideCellColor;
        }

        //Reproduction
        private void Clone()
        {
            IsCreatingClone = true;
        }
        private void Reproduction()
        {
            IsCreatingChildren = !IsCreatingChildren;
        }
    }
}
