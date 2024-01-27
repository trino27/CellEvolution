using CellEvolution;
using CellEvolution.Cell.NN;
using СellEvolution.Meteor;

namespace СellEvolution.WorldResources
{
    public class WorldArea
    {
        private readonly World world;

        private object lockObject = new object();

        public char[,] AreaChar;
        public ConsoleColor[,] AreaColor;
        public int[,] AreaVoice;
        public int[,] AreaEnergy;

        public List<MeteorBlock> MeteorBlocks = new List<MeteorBlock>();

        public WorldArea(World world)
        {
            this.world = world;

            AreaChar = new char[Constants.areaSizeX, Constants.areaSizeY];
            AreaColor = new ConsoleColor[Constants.areaSizeX, Constants.areaSizeY];
            AreaEnergy = new int[Constants.areaSizeX, Constants.areaSizeY];
            AreaVoice = new int[Constants.areaSizeX, Constants.areaSizeY];

            Console.WriteLine("Creating World!");
            CreateAreasParallel();
            Console.WriteLine("World Created!");
        }

        private void CreateAreaColorParallel()
        {
            Parallel.For(0, Constants.areaSizeY, y =>
            {
                for (int x = 0; x < Constants.areaSizeX; x++)
                {
                    lock (lockObject)
                    {
                        if (Constants.borderChar == AreaChar[x, y])
                        {
                            AreaColor[x, y] = Constants.borderColor;
                        }
                        else if (Constants.cellChar == AreaChar[x, y])
                        {
                            AreaColor[x, y] = world.GetCell(x, y).CellColor;
                        }
                        else if (Constants.emptyChar == AreaChar[x, y])
                        {
                            AreaColor[x, y] = Constants.emptyColor;
                        }
                        else if (Constants.poisonChar == AreaChar[x, y])
                        {
                            AreaColor[x, y] = Constants.poisonColor;
                        }
                    }
                }
            });
        }

        private void CreateAreasParallel()
        {
            Console.WriteLine("Creating Borders!");
            Task taskX = new Task(() =>
            {
                for (int x = 0; x < Constants.areaSizeX; x++)
                {
                    AreaChar[x, 0] = Constants.borderChar;
                    AreaChar[x, Constants.areaSizeY - 1] = Constants.borderChar;
                }

                Console.WriteLine("Borders X Created!");
            });

            Task taskY = new Task(() =>
            {
                for (int y = 0; y < Constants.areaSizeY; y++)
                {
                    AreaChar[0, y] = Constants.borderChar;
                    AreaChar[Constants.areaSizeX - 1, y] = Constants.borderChar;
                }
                Console.WriteLine("Borders Y Created!");
            });
            taskX.Start();
            taskY.Start();


            Console.WriteLine("Creating live!");
            Task taskCell = new Task(() =>
            {
                int i = 1;
                for (int y = 1; y < Constants.areaSizeY - 1; y += Constants.startCellCreationDistance)
                {
                    for (int x = 1; x < Constants.areaSizeX - 1; x += Constants.startCellCreationDistance)
                    {
                        AreaChar[x, y] = Constants.cellChar;
                        world.Cells.Add(new CellModel(x, y, world, i));
                        Console.SetCursorPosition(0, 5);
                        Console.WriteLine($"Cells: {i}");
                        Console.WriteLine($"X: {x}");
                        Console.Write($"Y: {y}");
                        i++;
                    }
                }

                Console.WriteLine();
            });
            taskCell.Start();

            Task taskEmpty = new Task(() =>
            {
                FillAreaParallel();
            });
            taskEmpty.Start();


            taskX.Wait();
            taskY.Wait();
            taskCell.Wait();
            taskEmpty.Wait();

            CreateAreaColorParallel();
        }

        public void FillAreaParallel()
        {
            Parallel.For(1, Constants.areaSizeY - 1, y =>
            {
                Parallel.For(1, Constants.areaSizeX - 1, x =>
                {
                    lock (lockObject)
                    {
                        if (AreaEnergy[x, y] < Constants.areaEnergyStartVal)
                        {
                            AreaEnergy[x, y] = Constants.areaEnergyStartVal;
                        }
                        if (IsAreaPoisoned(x, y))
                        {
                            AreaChar[x, y] = Constants.poisonChar;
                            AreaColor[x, y] = Constants.poisonColor;
                        }
                        else if (AreaChar[x, y] == Constants.nullChar)
                        {
                            AreaChar[x, y] = Constants.emptyChar;
                            AreaColor[x, y] = Constants.emptyColor;
                        }
                    }
                });
            });
        }

        public int GetNumOfLiveCellsAround(int positionX, int positionY)
        {
            lock (lockObject)
            {
                return GetAreaCharAroundCellInt(positionX, positionY, 1).Count(cell => cell >= Constants.KnewCell && cell < Constants.KdeadCell);
            }
        }

        public int GetCurrentAreaEnergy(int positionX, int positionY) => AreaEnergy[positionX, positionY];

        public List<int> GetAreaCharAroundCellInt(int positionX, int positionY, int dist)
        {
            lock (lockObject)
            {
                List<int> area = new List<int>((dist * 2 + 1) * (dist * 2 + 1) - 1);

                for (int x = positionX - dist; x < positionX + dist + 1; x++)
                {
                    for (int y = positionY - dist; y <= positionY + dist; y++)
                    {
                        if (y >= 0 && y < Constants.areaSizeY && x >= 0 && x < Constants.areaSizeX)
                        {
                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.meteorChar: k = Constants.Kmeteor; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
                                                case Constants.mineCellColor: k = Constants.KmineCell; break; // mine
                                                case Constants.hideCellColor: k = Constants.KhideCell; break; // hide
                                                case Constants.evolvingCellColor: k = Constants.KevolvingCell; break; // evolving
                                                case Constants.errorCellColor: k = Constants.KerrorCell; break; // error
                                                case Constants.deadCellColor: k = Constants.KdeadCell; break; // dead
                                            }

                                        }
                                        break;
                                }
                                area.Add(k);
                            }
                        }
                        else
                        {
                            area.Add(0);
                        }
                    }
                }

                return area;
            }
        }
        public List<int> GetAreaCharAroundCellInt(int positionX, int positionY)
        {
            lock (lockObject)
            {
                List<int> area = new List<int>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);
                for (int x = positionX - Constants.visionDistance; x <= positionX + Constants.visionDistance; x++)
                {
                    for (int y = positionY - Constants.visionDistance; y <= positionY + Constants.visionDistance; y++)
                    {
                        if (y >= 0 && y < Constants.areaSizeY && x >= 0 && x < Constants.areaSizeX)
                        {
                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.meteorChar: k = Constants.Kmeteor; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
                                                case Constants.mineCellColor: k = Constants.KmineCell; break; // mine
                                                case Constants.hideCellColor: k = Constants.KhideCell; break; // hide
                                                case Constants.evolvingCellColor: k = Constants.KevolvingCell; break; // evolving
                                                case Constants.errorCellColor: k = Constants.KerrorCell; break; // error
                                                case Constants.deadCellColor: k = Constants.KdeadCell; break; // dead
                                            }

                                        }
                                        break;
                                }
                                area.Add(k);
                            }
                        }
                        else
                        {
                            area.Add(0);
                        }
                    }
                }

                return area;
            }
        }
        public List<int> GetInfoFromAreaToCellBrainInput(int positionX, int positionY)
        {
            lock (lockObject)
            {
                List<int> area = new List<int>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);
                List<int> cellGenArea = new List<int>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);
                List<int> energyAreaInfo = new List<int>((Constants.energyAreaVisionDistance * 2 + 1) * (Constants.energyAreaVisionDistance * 2 + 1));

                for (int x = positionX - Constants.visionDistance; x <= positionX + Constants.visionDistance; x++)
                {
                    for (int y = positionY - Constants.visionDistance; y <= positionY + Constants.visionDistance; y++)
                    {
                        if (y >= 0 && y < Constants.areaSizeY && x >= 0 && x < Constants.areaSizeX)
                        {
                            if (y >= positionY - Constants.energyAreaVisionDistance && y <= positionY + Constants.energyAreaVisionDistance &&
                            x >= positionX - Constants.energyAreaVisionDistance && x <= positionX + Constants.energyAreaVisionDistance)
                            {
                                energyAreaInfo.Add(AreaEnergy[x, y]);
                            }

                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                bool isGenWrited = false;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.meteorChar: k = Constants.Kmeteor; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
                                                case Constants.mineCellColor: k = Constants.KmineCell; break; // mine
                                                case Constants.hideCellColor: k = Constants.KhideCell; break; // hide
                                                case Constants.evolvingCellColor: k = Constants.KevolvingCell; break; // evolving
                                                case Constants.errorCellColor: k = Constants.KerrorCell; break; // error
                                                case Constants.deadCellColor: k = Constants.KdeadCell; break; // dead
                                            }

                                            cellGenArea.Add(world.cellActionHandler.CellGenomeSimilarity(world.GetCell(x, y), world.GetCell(positionX, positionY)) + 1);
                                            isGenWrited = true;
                                        }
                                        break;
                                }
                                if (!isGenWrited)
                                {
                                    cellGenArea.Add(0);
                                }
                                area.Add(k);
                            }
                        }
                        else
                        {
                            area.Add(Constants.Kborder);
                            cellGenArea.Add(0);
                        }
                    }
                }

                area.AddRange(cellGenArea);
                area.AddRange(energyAreaInfo);
                area.AddRange(GetAreaVoiceInfo(positionX, positionY));
                area.Add(Convert.ToInt16(world.CurrentDayTime) * Constants.brainInputDayNightPoweredK);

                return area;
            }
        }

        public List<int> GetAreaVoiceInfo(int positionX, int positionY)
        {
            lock (lockObject)
            {
                List<int> res = new List<int>(7);

                if (AreaVoice[positionX, positionY] != 0)
                {
                    res.Add(positionX - world.Cells[AreaVoice[positionX, positionY] - 1].PositionX);
                    res.Add(positionY - world.Cells[AreaVoice[positionX, positionY] - 1].PositionY);

                    res.Add(world.cellActionHandler.CellGenomeSimilarity(world.GetCell(positionX, positionY), world.Cells[AreaVoice[positionX, positionY] - 1]));
                    res.Add(world.Cells[AreaVoice[positionX, positionY] - 1].Energy);
                    res.Add(world.Cells[AreaVoice[positionX, positionY] - 1].EnergyBank);
                    res.Add(world.Cells[AreaVoice[positionX, positionY] - 1].MaxClone);
                    res.Add(world.Cells[AreaVoice[positionX, positionY] - 1].Initiation);
                }
                else
                {
                    for (int i = 0; i < 7; i++)
                    {
                        res.Add(0);
                    }
                }

                return res;
            }
        }

        public List<(int, int)> FindAllEmptyCharNearCellCoord(int positionX, int positionY)
        {

            List<(int, int)> area = new List<(int, int)>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);

            for (int x = positionX - Constants.visionDistance; x < positionX + Constants.visionDistance + 1; x++)
            {
                for (int y = positionY - Constants.visionDistance; y < positionY + Constants.visionDistance + 1; y++)
                {
                    if (y >= 0 && y < Constants.areaSizeY && x >= 0 && x < Constants.areaSizeX && !(x == positionX && y == positionY))
                    {
                        if (AreaChar[x, y] == Constants.emptyChar)
                        {
                            area.Add((x, y));
                        }
                    }
                }
            }


            return area;
        }
        public List<(int, int)> FindAllCellCharNearCellCoord(int positionX, int positionY)
        {

            List<(int, int)> area = new List<(int, int)>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);

            for (int x = positionX - Constants.visionDistance; x < positionX + Constants.visionDistance + 1; x++)
            {
                for (int y = positionY - Constants.visionDistance; y < positionY + Constants.visionDistance + 1; y++)
                {
                    if (y >= 0 && y < Constants.areaSizeY && x >= 0 && x < Constants.areaSizeX && !(x == positionX && y == positionY))
                    {
                        if (AreaChar[x, y] == Constants.cellChar)
                        {
                            area.Add((x, y));
                        }
                    }
                }
            }


            return area;
        }

        public void ClearAreaVoiceParallel()
        {
            Parallel.For(0, Constants.areaSizeX, x =>
            {
                for (int y = 0; y < Constants.areaSizeY; y++)
                {
                    AreaVoice[x, y] = 0;
                }
            });
        }

        public void CreatePoisonArea(int x, int y)
        {
            lock (lockObject)
            {
                AreaChar[x, y] = Constants.poisonChar;
                AreaColor[x, y] = Constants.poisonColor;

                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(x, y, Constants.poisonChar, Constants.poisonColor);
                }
            }
        }
        public void ClearAreaFromPoison(int x, int y)
        {
            if (AreaChar[x, y] == Constants.poisonChar)
            {
                AreaChar[x, y] = Constants.emptyChar;
                AreaColor[x, y] = Constants.emptyColor;

                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(x, y, Constants.emptyChar, Constants.emptyColor);
                }
            }

        }

        public void CreateMeteorBlock(MeteorBlock meteorBlock)
        {
            lock (lockObject)
            {
                if (MeteorBlocks.Exists(meteorBlock2 => meteorBlock.PositionX == meteorBlock2.PositionX && meteorBlock.PositionY == meteorBlock2.PositionY))
                {
                    MeteorBlocks.Remove(GetMeteorBlock(meteorBlock.PositionX, meteorBlock.PositionY));
                }

                MeteorBlocks.Add(meteorBlock);

                AreaChar[meteorBlock.PositionX, meteorBlock.PositionY] = Constants.meteorChar;
                AreaColor[meteorBlock.PositionX, meteorBlock.PositionY] = Constants.meteorColor;

                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(meteorBlock.PositionX, meteorBlock.PositionY, Constants.meteorChar, Constants.meteorColor);
                }
            }
        }
        public void ClearMeteorBlock(MeteorBlock meteorBlock)
        {
            lock (lockObject)
            {
                if (IsAreaPoisoned(meteorBlock.PositionX, meteorBlock.PositionY))
                {
                    world.WorldArea.CreatePoisonArea(meteorBlock.PositionX, meteorBlock.PositionY);
                }
                else
                {
                    if (world.IsRenderAllow)
                    {
                        world.worldRenderer.VisualChange(meteorBlock.PositionX, meteorBlock.PositionY, Constants.emptyChar, Constants.emptyColor);
                    }

                    world.WorldArea.AreaChar[meteorBlock.PositionX, meteorBlock.PositionY] = Constants.emptyChar;
                    world.WorldArea.AreaColor[meteorBlock.PositionX, meteorBlock.PositionY] = Constants.emptyColor;
                }
            }
        }

        public MeteorBlock GetMeteorBlock(int x, int y)
        {
            lock (lockObject)
            {
                return MeteorBlocks.FirstOrDefault(meteorBlock => meteorBlock.PositionX == x && meteorBlock.PositionY == y);
            }
        }

        public void ClearDeadCells()
        {
            lock (lockObject)
            {
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    if (world.Cells[i].IsDead && !world.Cells[i].IsCorpseEaten)
                    {
                        ClearAreaFromDeadCell(world.Cells[i].PositionX, world.Cells[i].PositionY);
                        DeadCellToAreaEnergy(i);
                    }
                }

                world.Cells.RemoveAll(cell => cell.IsDead);
            }
        }
        public void ClearAreaFromDeadCell(int positionX, int positionY)
        {
            if (IsAreaPoisoned(positionX, positionY))
            {
                CreatePoisonArea(positionX, positionY);
            }
            else
            {
                AreaChar[positionX, positionY] = Constants.emptyChar;
                AreaColor[positionX, positionY] = Constants.emptyColor;

                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(positionX, positionY, Constants.emptyChar, Constants.emptyColor);
                }
            }
        }
        public void DeadCellToAreaEnergy(int i)
        {
            int x = world.Cells[i].PositionX;
            int y = world.Cells[i].PositionY;
            if (world.Cells[i].Energy / 4 > Constants.minEnergyFromDeadCell)
            {
                AreaEnergy[x, y] += world.Cells[i].Energy / 4;
            }
            else
            {
                AreaEnergy[x, y] += Constants.minEnergyFromDeadCell;
            }

            if (IsAreaPoisoned(x, y) && AreaChar[x, y] == Constants.emptyChar)
            {
                CreatePoisonArea(x, y);
            }
        }
        public void DeadCellToAreaEnergy(CellModel cell)
        {
            int x = cell.PositionX;
            int y = cell.PositionY;
            if (cell.Energy / 4 > Constants.minEnergyFromDeadCell)
            {
                AreaEnergy[x, y] += cell.Energy / 4;
            }
            else
            {
                AreaEnergy[x, y] += Constants.minEnergyFromDeadCell;
            }

            if (IsAreaPoisoned(x, y) && AreaChar[x, y] == Constants.emptyChar)
            {
                CreatePoisonArea(x, y);
            }
        }
        public bool IsAreaPoisoned(int x, int y) => AreaEnergy[x, y] > Constants.energyAreaPoisonedCorner;
    }
}
