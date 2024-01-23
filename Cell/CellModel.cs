using static CellEvolution.Cell.GenAlg.CellGen;

namespace CellEvolution.Cell.NN
{
    public partial class CellModel
    {
        private Random random = new Random();
        private readonly object lockObject = new object();

        private readonly NNCellBrain brain;
        private readonly World world;

        private readonly Guid id;
        private readonly int generationNum = 0;
        private readonly int[] spiecie;

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

        public bool IsReproducting = false;
        public bool IsCreatingClone = false;
        public bool IsCreatingChildren = false;

        public CellModel(int positionX, int positionY, World map, int creationNum)
        {
            id = Guid.NewGuid();
            spiecie = new int[1];
            spiecie[0] = creationNum;

            PositionX = positionX;
            PositionY = positionY;

            Energy = Constants.startCellEnergy;

            world = map;

            brain = new NNCellBrain(this);
            brain.RandomFillWeightsParallel();


        }
        public CellModel(int positionX, int positionY, World map, CellModel original)
        {
            id = Guid.NewGuid();

            spiecie = original.spiecie;

            generationNum = original.generationNum + 1;

            Energy += original.EnergyBank;

            PositionX = positionX;
            PositionY = positionY;

            world = map;

            brain = new NNCellBrain(this);
            brain.Clone(original.brain);

        }
        public CellModel(int positionX, int positionY, World map, CellModel mother, CellModel father)
        {
            id = Guid.NewGuid();

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

            PositionX = positionX;
            PositionY = positionY;

            world = map;

            brain = new NNCellBrain(this);
            brain.Clone(mainParent.brain, secondParent.brain);

            Energy = mother.EnergyBank + father.EnergyBank + Constants.startCellEnergy * 2;

        }


        public void MakeAction()
        {
            IsSlip = false;
            IsCreatingClone = false;

            if (IsReproducting == false)
            {
                AlreadyUseClone = 0;
            }
            else if (AlreadyUseClone == MaxClone)
            {
                IsReproducting = false;
                AlreadyUseClone = 0;
            }

            PerformAction(brain.ChooseAction());

            if (brain.IsErrorMove) CellColor = Constants.errorCellColor;


            brain.LearnWithTeacher();


            Energy -= IsSlip ? Constants.slipEnergyCost : Constants.actionEnergyCost /*+ world.GetCurrentYear() * Constants.eachYearEnergyCostGain*/;

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

        public GenActions[] GetGenomCycle()
        {
            return brain.GetGen().GenActionsCycle;
        }

        private void PerformAction(int decidedAction) //MovesCode
        {
            switch (decidedAction)
            {
                //Move
                case 0: MoveLeftUp(); break;
                case 1: MoveUp(); break;
                case 2: MoveRightUp(); break;
                case 3: MoveRight(); break;
                case 4: MoveRightDown(); break;
                case 5: MoveDown(); break;
                case 6: MoveLeftDown(); break;
                case 7: MoveLeft(); break;

                case 8: JumpUp(); break;
                case 9: JumpRight(); break;
                case 10: JumpDown(); break;
                case 11: JumpLeft(); break;

                //Hunt
                case 12: BiteLeftUp(); break;
                case 13: BiteUp(); break;
                case 14: BiteRightUp(); break;
                case 15: BiteRight(); break;
                case 16: BiteRightDown(); break;
                case 17: BiteDown(); break;
                case 18: BiteLeftDown(); break;
                case 19: BiteLeft(); break;

                //Photosynthesis
                case 20: Photosynthesis(); break;

                //Absorption
                case 21: Absorption(); break;

                //Preproduction
                case 22: Clone(); break;
                case 23: Reproduction(); break;

                // Slip
                case 24: Slip(); break;
                case 25: Shout(); break;

                case 26: break;
                case 27: break;

                //Evolving
                case 28: GainInitiation(); break;
                case 29: GainMaxClone(); break;
                case 30: GainEnergyBank(); break;
                case 31: DecEnergyBank(); break;
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
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteUp()
        {
            if (world.IsVictimExists(PositionX, PositionY - 1))
            {
                world.Hunt(this, world.GetCell(PositionX, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightUp()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY - 1))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRight()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteRightDown()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteDown()
        {
            if (world.IsVictimExists(PositionX, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeftDown()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
        }
        private void BiteLeft()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY))
            {
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY));
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
            world.Absorb(this);
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
            IsReproducting = !IsReproducting;
        }
    }
}
