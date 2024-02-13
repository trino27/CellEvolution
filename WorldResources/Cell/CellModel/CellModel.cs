using CellEvolution.WorldResources;
using EvolutionNetwork.DDQNwithGA;
using СellEvolution;

namespace CellEvolution.Cell.CellModel
{
    public partial class CellModel
    {
        private Random random = new Random();

        private readonly DDQNwithGAModel brain;
        private readonly WorldModel world;

        private readonly Guid id;
        private readonly int generationNum = 0;
        private int[] specie = new int[1];

        private readonly int[] layersSizes = { 125, 256, 256, 128, 30 };

        private Dictionary<CellAction, Action> actionDictionary;

        public int AlreadyUseClone = 0;

        public int PositionX;
        public int PositionY;

        public int Energy = Constants.startCellEnergy;

        public int CurrentAge = 0;

        public ConsoleColor CellColor = Constants.newCellColor;

        public bool IsCorpseEaten = false;
        public bool IsDead = false;
        public bool IsSlip = false;
        public bool IsHide = false;
        public bool IsMadeAction = false;

        public bool IsCreatingChildren = false;
        public bool IsCreatingClone = false;

        public CellModel(int positionX, int positionY, WorldModel world, int creationNum)
        {
            id = Guid.NewGuid();

            specie[0] = creationNum;

            this.world = world;

            brain = new DDQNwithGAModel(layersSizes, (uint)Constants.maxMemoryCapacity);

            ActionDictionaryInit();
            CellInit(positionX, positionY);
        }
        public CellModel(int positionX, int positionY, WorldModel world, CellModel original)
        {
            id = Guid.NewGuid();

            specie = original.specie;

            this.world = world;

            generationNum = original.generationNum + 1;

            brain = new DDQNwithGAModel(original.brain);

            ActionDictionaryInit();
            CellInit(positionX, positionY);
        }
        public CellModel(int positionX, int positionY, WorldModel world, CellModel mother, CellModel father)
        {
            id = Guid.NewGuid();

            if (random.Next(0, 2) == 0)
            {
                generationNum = mother.generationNum + 1;
            }
            else
            {
                generationNum = father.generationNum + 1;
            }

            this.world = world;

            brain = new DDQNwithGAModel(mother.brain, father.brain);

            SpecieFromParentsInit(mother, father);
            ActionDictionaryInit();
            CellInit(positionX, positionY);
        }

        private void CellInit(int positionX, int positionY)
        {
            PositionX = positionX;
            PositionY = positionY;
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
            { CellAction.Hide, Hide },
            { CellAction.MineTop, MineTop },
            { CellAction.MineRightSide, MineRightSide },
            { CellAction.MineBottom, MineBottom },
            { CellAction.MineLeftSide, MineLeftSide },
        };
        }

        public void MakeAction()
        {
            if (IsMadeAction)
            {
                brain.RegisterLastActionResult(CreateBrainInput(), AlreadyUseClone, Energy, Constants.actionEnergyCost);
                IsMadeAction = false;
            }

            IsSlip = false;

            IsCreatingClone = false;
            IsCreatingChildren = false;
            AlreadyUseClone = 0;

            IsHide = false;

            PerformAction((CellAction)brain.ChooseAction(CreateBrainInput(), Energy));
            IsMadeAction = true;

            if (brain.IsErrorMove)
            {
                CellColor = Constants.errorCellColor;
            }

            Energy -= IsSlip ? Constants.slipEnergyCost : Constants.actionEnergyCost;

            if (world.WorldArea.IsAreaPoisoned(PositionX, PositionY))
            {
                Energy -= Constants.poisonedDecEnergy;
            }

            if (IsNoEnergy()) IsDead = true;

            if (CurrentAge > Constants.maxLive) IsDead = true;
            else CurrentAge++;

            if (IsDead)
            {
                CellColor = Constants.deadCellColor;
            }
        }

        private double[] CreateBrainInput()
        {
            double[] inputsBrain = new double[layersSizes[0]];
            List<int> areaInfo = world.cellActionHandler.GetInfoFromAreaToCellBrainInput(PositionX, PositionY);
            double[] inputsMemory = brain.CreateMemoryInput();
            int j = 0;
            for (int i = 0; i < areaInfo.Count; i++) //0-47(-48)(areaChar) 48-95(-48)(cellsEnergy) 96-104(-9)(energyArea) 105(-1)(DayTime) 106(-1)(Photosynthesis) 107(-1)(IsPoison)     
            {
                if (i < 48)
                {
                    inputsBrain[j] = Normalizer.CharNormalize(areaInfo[i]);
                }
                else if (i >= 48 && i < 105)
                {
                    inputsBrain[j] = Normalizer.EnergyNormalize(areaInfo[i]);
                }
                else if (i == 106)
                {
                    inputsBrain[j] = Normalizer.PhotosyntesNormalize(areaInfo[i]);
                }
                else
                {
                    inputsBrain[j] = areaInfo[i];
                }
                j++;
            }

            inputsBrain[j] = Normalizer.EnergyNormalize(Energy); //108
            j++;

            for (int i = 0; i < Constants.maxMemoryCapacity; i++) //109-124
            {
                inputsBrain[j] = Normalizer.ActionNormalize(inputsMemory[i]);
                j++;
            }


            return inputsBrain.ToArray();
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
            if (world.CurrentDayTime == WorldModel.DayTime.Day)
            {
               
                Energy += (int)world.WorldArea.CulcPhotosyntesisEnergy(PositionX, PositionY);
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

        private void MineTop()
        {
            CellColor = Constants.mineCellColor;
            world.cellActionHandler.CellMineTop(this);
        }
        private void MineRightSide()
        {
            CellColor = Constants.mineCellColor;
            world.cellActionHandler.CellMineRightSide(this);
        }
        private void MineBottom()
        {
            CellColor = Constants.mineCellColor;
            world.cellActionHandler.CellMineBottom(this);
        }
        private void MineLeftSide()
        {
            CellColor = Constants.mineCellColor;
            world.cellActionHandler.CellMineLeftSide(this);
        }

        private void Slip()
        {
            IsSlip = true;
            CellColor = Constants.slipCellColor;
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
