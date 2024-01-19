using CellEvolution.Cell.GenAlg;

namespace CellEvolution.Cell.NN
{
    public partial class CellModel
    {
        private Random random = new Random();
        private readonly object lockObject = new object();

        const int turnBeforeFlag = 0;

        public readonly CellGen gen;
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

        public int CurrentGenIndex = 0;

        public int numOfMoves = 0;
        public int CurrentAge = 0;

        public ConsoleColor CellColor = Constants.newCellColor;

        public bool IsErrorMove = false;

        public bool IsCorpseEaten = false;
        public bool IsDead = false;
        public bool IsSlip = false;

        public bool IsReproducting = false;
        public bool IsCreatingClone = false;
        public bool IsCreatingChildren = false;

        public int[] LastMovesDecidedActionsNum = new int[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];
        public double[][] LastMovesInputs = new double[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime][];
        public bool[] ErrorMoves = new bool[Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime];

        private double[] RandomInputToClone;

        private double[] inputs;

        public CellModel(int positionX, int positionY, World map, int creationNum)
        {
            id = Guid.NewGuid();
            spiecie = new int[1];
            spiecie[0] = creationNum;

            PositionX = positionX;
            PositionY = positionY;

            Energy = Constants.startCellEnergy;

            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                LastMovesDecidedActionsNum[i] = -1;
            }

            world = map;

            brain = new NNCellBrain();
            brain.RandomFillWeightsParallel();
            gen = new CellGen();
        }
        public CellModel(int positionX, int positionY, World map, CellModel original)
        {
            id = Guid.NewGuid();

            spiecie = original.spiecie;

            generationNum = original.generationNum + 1;

            PositionX = positionX;
            PositionY = positionY;

            world = map;

            brain = new NNCellBrain();
            brain.Clone(original.brain, RandomInputToClone);

            bool flag = false;
            if (world.Logic.CurrentTurn > turnBeforeFlag)
            {
                flag = true;
            }
            gen = new CellGen(original.gen, flag);

            Energy += original.EnergyBank;

            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                LastMovesDecidedActionsNum[i] = -1;
            }
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
            brain.Clone(mainParent.brain, RandomInputToClone);

            bool flag = false;
            if (world.Logic.CurrentTurn > turnBeforeFlag)
            {
                flag = true;
            }

            gen = new CellGen(mother.gen, father.gen, flag);

            Energy = mother.EnergyBank + father.EnergyBank + Constants.startCellEnergy * 2;

            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)
            {
                LastMovesDecidedActionsNum[i] = -1;
            }
        }


        public void MakeAction()
        {
            IsSlip = false;
            IsCreatingClone = false;
            IsErrorMove = false;

            if (IsReproducting == false)
            {
                AlreadyUseClone = 0;
            }
            else if (AlreadyUseClone == MaxClone)
            {
                IsReproducting = false;
                AlreadyUseClone = 0;
            }

            int decidedAction = ChooseAction();

            PerformAction(decidedAction);

            RegisterDecidedAction(decidedAction);
            RegisterErrorMove(IsErrorMove);

            numOfMoves++;

            brain.UseExpToLearn(IsErrorMove,numOfMoves, LastMovesInputs, LastMovesDecidedActionsNum, ErrorMoves);

            gen.RandomMutationDuringLive();

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
            NextGenIndex();

            inputs = CreateBrainInput();
            RegisterInput(inputs);

            double rand = random.NextDouble();
            if (rand < 0.1)
            {
                RandomInputToClone = inputs;
            }

            double[] outputs = brain.FeedForwardWithNoise(inputs);
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
            for (int i = 0; i < area.Count; i++) //48+48+9+7 = 112
            {
                inputsBrain[j] = area[i];
                j++;
            }

            inputsBrain[j] = (Convert.ToInt16(world.IsDay())); //113
            j++;
            inputsBrain[j] = (Initiation); //114
            j++;
            inputsBrain[j] = (Energy); //115
            j++;
            inputsBrain[j] = (MaxClone); //116
            j++;
            inputsBrain[j] = (CurrentAge);  //117
            j++;
            inputsBrain[j] = (EnergyBank);  //118
            j++;
            for (int i = 0; i < Constants.numOfMemoryLastMoves; i++)  //128
            {
                inputsBrain[j] = LastMovesDecidedActionsNum[i];
                j++;
            }

            return inputsBrain.ToArray();
        }

        private bool IsNoEnergy()
        {
            if (Energy < 0) return true;
            else return false;
        }
        private void RegisterDecidedAction(int decidedAction)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                LastMovesDecidedActionsNum[i] = LastMovesDecidedActionsNum[i - 1];
            }

            LastMovesDecidedActionsNum[0] = decidedAction;
        }
        private void RegisterInput(double[] input)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                LastMovesInputs[i] = LastMovesInputs[i - 1];
            }

            LastMovesInputs[0] = input;
        }
        private void RegisterErrorMove(bool res)
        {
            for (int i = Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime - 1; i > 0; i--)
            {
                ErrorMoves[i] = ErrorMoves[i - 1];
            }
            if (res) CellColor = Constants.errorColor;
            ErrorMoves[0] = res;
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
            else IsErrorMove = true;
        }
        private void MoveDown()
        {
            if (world.IsMoveAvailable(PositionX, PositionY + 1)) PositionY++;
            else IsErrorMove = true;
        }
        private void MoveLeft()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY)) PositionX--;
            else IsErrorMove = true;
        }
        private void MoveRight()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY)) PositionX++;
            else IsErrorMove = true;
        }

        private void MoveLeftUp()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY - 1))
            {
                PositionX--;
                PositionY--;
            }
            else IsErrorMove = true;
        }
        private void MoveRightUp()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY - 1))
            {
                PositionX++;
                PositionY--;
            }
            else IsErrorMove = true;
        }
        private void MoveRightDown()
        {
            if (world.IsMoveAvailable(PositionX + 1, PositionY + 1))
            {
                PositionX++;
                PositionY++;
            }
            else IsErrorMove = true;
        }
        private void MoveLeftDown()
        {
            if (world.IsMoveAvailable(PositionX - 1, PositionY + 1))
            {
                PositionX--;
                PositionY++;
            }
            else IsErrorMove = true;
        }

        //Jump
        private void JumpUp()
        {
            if (world.IsMoveAvailable(PositionX, PositionY - Constants.jumpDistance))
            {
                PositionY -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
            else IsErrorMove = true;
        }
        private void JumpRight()
        {
            if (world.IsMoveAvailable(PositionX + Constants.jumpDistance, PositionY))
            {
                PositionX += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
            else IsErrorMove = true;
        }
        private void JumpDown()
        {
            if (world.IsMoveAvailable(PositionX, PositionY + Constants.jumpDistance))
            {
                PositionY += Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;

            }
            else IsErrorMove = true;
        }
        private void JumpLeft()
        {
            if (world.IsMoveAvailable(PositionX - Constants.jumpDistance, PositionY))
            {
                PositionX -= Constants.jumpDistance;
                Energy -= Constants.jumpEnergyCost;
            }
            else IsErrorMove = true;
        }

        //Bite
        private void BiteLeftUp()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY - 1))
            {
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteUp()
        {
            if (world.IsVictimExists(PositionX, PositionY - 1))
            {
                world.Hunt(this, world.GetCell(PositionX, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteRightUp()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY - 1))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY - 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteRight()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteRightDown()
        {
            if (world.IsVictimExists(PositionX + 1, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX + 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteDown()
        {
            if (world.IsVictimExists(PositionX, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteLeftDown()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY + 1))
            {
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY + 1));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
        }
        private void BiteLeft()
        {
            if (world.IsVictimExists(PositionX - 1, PositionY))
            {
                world.Hunt(this, world.GetCell(PositionX - 1, PositionY));
                CellColor = Constants.biteCellColor;
            }
            else IsErrorMove = true;
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

                IsErrorMove = true;
            }
        }
        private void Absorption()
        {
            int EnergyBeforeAbsorb = Energy;

            CellColor = Constants.absorbCellColor;
            world.Absorb(this);

            if (EnergyBeforeAbsorb == Energy)
            {
                IsErrorMove = true;
            }
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
