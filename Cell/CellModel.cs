using CellEvolution.Cell.GenAlg;

namespace CellEvolution.Cell.NN
{
    public partial class CellModel
    {
        private Random random = new Random();
        private readonly object lockObject = new object();

        private readonly Guid id;
        private readonly int generationNum = 0;

        public readonly CellGen gen;
        private readonly NNCellBrain brain;
        private readonly World world;

        public int MaxClone = 2;
        public int AlreadyClone = 0;

        public int PositionX;
        public int PositionY;

        public int Energy = Constants.startCellEnergy;
        public int EnergyBank = 0;

        public int Initiation = 1;

        public int CurrentGenIndex = 0;

        public int addLiveCount = 0;
        public int LiveTime = Constants.startLiveVal;
        public int CurrentAge = 0;

        public ConsoleColor CellColor = Constants.newCellColor;

        public bool IsCorpseEaten = false;
        public bool IsDead = false;
        public bool IsSlip = false;

        public bool IsReproducting = false;
        public bool IsCreatingClone = false;
        public bool IsCreatingChildren = false;

        double[] inputs;

        public List<int> LastMoves = new List<int>(Constants.numOfMemoryLastMoves);

        public CellModel(int positionX, int positionY, World map)
        {
            id = Guid.NewGuid();

            PositionX = positionX;
            PositionY = positionY;

            Energy = Constants.startCellEnergy;

            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                LastMoves.Add(-1);
            }

            world = map;

            brain = new NNCellBrain();
            brain.RandomFillWeights();
            gen = new CellGen();
        }
        public CellModel(int positionX, int positionY, World map, CellModel original)
        {
            id = Guid.NewGuid();

            generationNum = original.generationNum + 1;

            PositionX = positionX;
            PositionY = positionY;

            world = map;

            brain = new NNCellBrain();
            brain.Clone(original.brain);

            gen = new CellGen(original.gen);

            Energy += original.EnergyBank;


            if (original.LastMoves.Count < Constants.numOfMemoryLastMoves)
            {
                for (int i = 0; i < original.LastMoves.Count; i++)
                {
                    int temp = original.LastMoves[i];
                    LastMoves.Add(temp);
                }
            }
            else
            {
                for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
                {
                    LastMoves.Add(-1);
                }
                for (int i = 0; i < original.LastMoves.Count; i++)
                {
                    int temp = original.LastMoves[i];
                    LastMoves.Add(temp);
                }
            }
        }
        public CellModel(int positionX, int positionY, World map, CellModel mother, CellModel father)
        {
            id = Guid.NewGuid();

            CellModel mainParent;

            if (random.Next(0, 2) == 0)
            {
                mainParent = mother;
            }
            else
            {
                mainParent = father;
            }

            generationNum = mainParent.generationNum + 1;

            PositionX = positionX;
            PositionY = positionY;

            world = map;

            brain = new NNCellBrain();
            brain.Clone(mainParent.brain);

            gen = new CellGen(mother.gen, father.gen);

            Energy = mother.EnergyBank + father.EnergyBank + Constants.startCellEnergy*2;

            if (mainParent.LastMoves.Count < Constants.numOfMemoryLastMoves)
            {
                for (int i = 0; i < mainParent.LastMoves.Count; i++)
                {
                    int temp = mainParent.LastMoves[i];
                    LastMoves.Add(temp);
                }
            }
            else
            {
                for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
                {
                    LastMoves.Add(-1);
                }
                for (int i = 0; i < mainParent.LastMoves.Count; i++)
                {
                    int temp = mainParent.LastMoves[i];
                    LastMoves.Add(temp);
                }
            }
        }


        public void MakeAction()
        {
            IsSlip = false;
            IsCreatingClone = false;
            IsReproducting = false;
            AlreadyClone = 0;

            int decidedAction = ChooseAction();

            PerformAction(decidedAction);
            RegMove(decidedAction);

            Energy -= IsSlip ? Constants.slipEnergyCost : Constants.actionEnergyCost /*+ world.GetCurrentYear() * Constants.eachYearEnergyCostGain*/;

            //if (world.IsAreaPoisoned(PositionX, PositionY))
            //{
            //    LiveTime -= Constants.poisonedDecLive;
            //}
            if (CurrentAge >= LiveTime)
            {
                IsDead = true;
            }
            else
            {
                if (!IsSlip) CurrentAge += Constants.actionLiveCost;
                else CurrentAge += Constants.slipLiveCost;

                IsDead = IsNoEnergy();
            }
            lock (lockObject)
            {
                if (IsDead)
                {
                    CellColor = Constants.deadCellColor;
                }
            }
        }
        private void NextGenIndex()
        {
            CurrentGenIndex++;
            if (CurrentGenIndex >= Constants.genCycleSize)
            {
                CurrentGenIndex = 0;
            }
        }
        private int ChooseAction()
        {
            List<int> availableActions = new List<int>();

            switch (gen.GetCurrentGenAction(CurrentGenIndex))
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
                case CellGen.GenActions.Build:
                    {
                        availableActions.Add(25);
                        availableActions.Add(26);
                    }
                    break;
                case CellGen.GenActions.Evolving:
                    {
                        for (int i = 27; i < 32; i++)
                        {
                            availableActions.Add(i);
                        }
                    }
                    break;
                case CellGen.GenActions.Actions:
                    {
                        availableActions.Add(24);
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
            NextGenIndex();

            inputs = CreateBrainInput();
            double[] outputs = brain.FeedForward(inputs);
            int decidedAction = FindMaxIndexForFindAction(outputs, availableActions);

            return decidedAction;
        }

        private void PerformAction(int decidedAction)
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

                //Build
                case 25: BuildWalls(); break;
                case 26: DestroyWalls(); break;

                //Evolving
                case 27: GainInitiation(); break;
                case 28: GainLiveTime(); break;
                case 29: GainMaxClone(); break;
                case 30: GainEnergyBank(); break;
                case 31: DecEnergyBank(); break;
            }
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
            double[] inputsBrain = new double[brain.layers[0].size];
            List<int> area = world.GetInfoFromAreaToCellBrainInput(PositionX, PositionY);

            int j = 0;
            for (int i = 0; i < area.Count; i++) //48+48+9 = 105
            {
                inputsBrain[j] = area[i];
                j++;
            }

            inputsBrain[j] = (Convert.ToInt16(world.IsDay())); //106
            j++;
            inputsBrain[j] = (Initiation); //107
            j++;
            inputsBrain[j] = (Energy); //108
            j++;
            inputsBrain[j] = (MaxClone); //109
            j++;
            inputsBrain[j] = (LiveTime);  //110
            j++;
            inputsBrain[j] = (CurrentAge);  //111
            j++;
            inputsBrain[j] = (EnergyBank);  //112
            j++;
            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)  //128
            {
                inputsBrain[j] = (LastMoves[i]);
                j++;
            }

            return inputsBrain.ToArray();
        }

        private bool IsNoEnergy()
        {
            if (Energy < 0) return true;
            else return false;
        }
        private void RegMove(int decidedAction)
        {
            for (int i = LastMoves.Count - 1; i > 0; i--)
            {
                LastMoves[i] = LastMoves[i - 1];
            }

            LastMoves[0] = decidedAction;

        }
        private int Fact(int i)
        {
            if (i == 0) return 1;
            else return i * Fact(i - 1);
        }

        //Evolving
        private void GainInitiation()
        {
            Initiation++;
            CellColor = Constants.evolvingCellColor;

        }
        private void GainLiveTime()
        {
            int temp = Fact(addLiveCount + 1);
            if (Energy > temp + Constants.slipEnergyCost)
            {
                addLiveCount++;
                LiveTime += addLiveCount;
                Energy -= temp;
            }
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
            if (world.IsMoveAvailable(PositionX, PositionY - Constants.jumpRange))
            {
                PositionY -= Constants.jumpRange;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpRight()
        {
            if (world.IsMoveAvailable(PositionX + Constants.jumpRange, PositionY))
            {
                PositionX += Constants.jumpRange;
                Energy -= Constants.jumpEnergyCost;
            }
        }
        private void JumpDown()
        {
            if (world.IsMoveAvailable(PositionX, PositionY + Constants.jumpRange))
            {
                PositionY += Constants.jumpRange;
                Energy -= Constants.jumpEnergyCost;

            }
        }
        private void JumpLeft()
        {
            if (world.IsMoveAvailable(PositionX - Constants.jumpRange, PositionY))
            {
                PositionX -= Constants.jumpRange;
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
                if (numOfCellsAround > Constants.availableCellNumAround)
                {
                    addEnergy = addEnergy / ((numOfCellsAround - Constants.availableCellNumAround) * (numOfCellsAround + 1 - Constants.availableCellNumAround));
                }
                else
                {
                    addEnergy += Constants.availableCellNumAround - numOfCellsAround;
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
            int addEnergy = world.GetCurrentAreaEnergy(PositionX, PositionY);

            if (addEnergy > 0)
            {
                CellColor = Constants.absorbCellColor;
                Energy += addEnergy;
                world.EnergyArea[PositionX, PositionY] = 0;
            }
        }
        private void Slip()
        {
            IsSlip = true;
            CellColor = Constants.slipCellColor;
        }

        //Build
        private void BuildWalls()
        {
            if (world.GetAreaAroundCellInt(PositionX, PositionY, 1).Contains(1) && Energy > 24 + Constants.slipEnergyCost)
            {
                world.CreateWallsAroundParallel(this);
                Energy -= 24;
            }

        }
        private void DestroyWalls()
        {
            world.DestroyWallsAroundParallel(this);
            CellColor = Constants.wallDestroyerCellColor;
        }

        //Reproduction
        private void Clone()
        {
            IsCreatingClone = true;
        }
        private void Reproduction()
        {
            IsReproducting = true;
        }
    }
}
