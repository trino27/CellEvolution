using System.Diagnostics.Metrics;
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
        private int[] spiecie = new int[1];

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

        public bool IsCreatingChildren = false;
        public bool IsCreatingClone = false;

        public CellModel(int positionX, int positionY, World map, int creationNum)
        {
            id = Guid.NewGuid();

            spiecie[0] = creationNum;

            world = map;

            brain = new NNCellBrain(this);
            brain.RandomFillWeightsParallel();

            ActionDictionaryInit();
            CellInit(positionX, positionY);
        }
        public CellModel(int positionX, int positionY, World map, CellModel original)
        {
            id = Guid.NewGuid();

            spiecie = original.spiecie;

            world = map;

            generationNum = original.generationNum + 1;

            brain = new NNCellBrain(this);
            brain.Clone(original.brain);

            ActionDictionaryInit();
            CellInit(positionX, positionY, original.EnergyBank);
        }
        public CellModel(int positionX, int positionY, World map, CellModel mother, CellModel father)
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

            world = map;

            brain = new NNCellBrain(this);
            brain.Clone(mainParent.brain, secondParent.brain);

            SpiecieFromParentsInit(mother, father);
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
        private void SpiecieFromParentsInit(CellModel mother, CellModel father)
        {
            int numSameSpecies = 0;
            for (int i = 0; i < mother.spiecie.Length; i++)
            {
                for (int j = 0; j < father.spiecie.Length; j++)
                {
                    if (mother.spiecie[i] == father.spiecie[j]) numSameSpecies++;
                }
            }

            spiecie = new int[father.spiecie.Length + mother.spiecie.Length - numSameSpecies];
            {
                int i = 0;
                for (int j = 0; j < father.spiecie.Length; j++)
                {
                    if (!spiecie.Contains(father.spiecie[j]))
                    {
                        spiecie[i] = father.spiecie[j];
                        i++;
                    }
                }
                for (int j = 0; j < mother.spiecie.Length; j++)
                {
                    if (!spiecie.Contains(mother.spiecie[j]))
                    {
                        spiecie[i] = mother.spiecie[j];
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
            { CellAction.Evolving1, null },
            { CellAction.Evolving2, null },
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

            if (IsCreatingChildren == false)
            {
                AlreadyUseClone = 0;
            }
            else if (AlreadyUseClone == MaxClone)
            {
                IsCreatingChildren = false;
                AlreadyUseClone = 0;
            }

            PerformAction(brain.ChooseAction());

            if (brain.IsErrorMove) CellColor = Constants.errorCellColor;


            brain.LearnWithTeacher();


            Energy -= IsSlip ? Constants.slipEnergyCost : Constants.actionEnergyCost;

            if (world.IsAreaPoisoned(PositionX, PositionY))
            {
                CurrentAge += Constants.poisonedDecLive;
            }
            if (CurrentAge >= Constants.liveTime)
            {
                IsDead = true;
            }
            else
            {
                if (!IsSlip) CurrentAge += Constants.actionLiveCost;
                else CurrentAge += Constants.slipLiveCost;

                IsDead = IsNoEnergy();
            }

            if (IsDead)
            {
                CellColor = Constants.deadCellColor;
            }
        }

        public List<int> GetWorldAroundInfo()
        {
            return world.GetInfoFromAreaToCellBrainInput(PositionX, PositionY);
        }

        public GenAction[] GetGenomCycle()
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
            if (world.IsMoveAvailable(PositionX, PositionY - 1)) PositionY--;

        }
        private void MoveDown()
        {
            if (world.IsMoveAvailable(PositionX, PositionY + 1)) PositionY++;
        }
        private void MoveLeft()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY)) PositionX--;
        }
        private void MoveRight()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY)) PositionX++;
        }

        private void MoveLeftUp()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY - 1))
            {
                PositionX--;
                PositionY--;
            }
        }
        private void MoveRightUp()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY - 1))
            {
                PositionX++;
                PositionY--;
            }
        }
        private void MoveRightDown()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY + 1))
            {
                PositionX++;
                PositionY++;
            }
        }
        private void MoveLeftDown()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY + 1))
            {
                PositionX--;
                PositionY++;
            }
        }

        //Jump
        private void JumpUp()
        {
            if (world.IsMoveAvailable(PositionX, PositionY - Constants.jumpDistance))
            {
                PositionY -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpRight()
        {
            if (world.IsMoveAvailable(PositionX + Constants.jumpDistance, PositionY))
            {
                PositionX += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpDown()
        {
            if (world.IsMoveAvailable(PositionX, PositionY + Constants.jumpDistance))
            {
                PositionY += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;

            }
        }
        private void JumpLeft()
        {
            if (world.IsMoveAvailable(PositionX - Constants.jumpDistance, PositionY))
            {
                PositionX -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
        }

        //Bite
        private void BiteLeftUp()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY - 1))
            {
                world.CellHunt(this, world.GetCell(PositionX - 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteUp()
        {
            if (world.IsVictimExists(PositionX, PositionY - 1))
            {
                world.CellHunt(this, world.GetCell(PositionX, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightUp()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY - 1))
            {
                world.CellHunt(this, world.GetCell(PositionX + 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRight()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY))
            {
                world.CellHunt(this, world.GetCell(PositionX + 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightDown()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY + 1))
            {
                world.CellHunt(this, world.GetCell(PositionX + 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteDown()
        {
            if (world.IsVictimExists(PositionX, PositionY + 1))
            {
                world.CellHunt(this, world.GetCell(PositionX, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeftDown()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY + 1))
            {
                world.CellHunt(this, world.GetCell(PositionX - 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeft()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY))
            {
                world.CellHunt(this, world.GetCell(PositionX - 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
        }

        //Action
        private void Photosynthesis()
        {
            if (world.IsDay())
            {
                double proc = (double)world.Cells.Count * 100 / (double)((Constants.areaSizeX - 2) * (Constants.areaSizeY - 2));
                double addEnergy = Constants.minPhotosynthesis + Constants.maxPhotosynthesis / 100.0 * (100 - proc);

                int numOfCellsAround = world.GetNumOfLiveCellsAround(PositionX, PositionY);
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
            world.CellAbsorb(this);
        }
        private void Slip()
        {
            IsSlip = true;
            CellColor = Constants.slipCellColor;
        }
        private void Shout()
        {
            world.CellShout(this);
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
