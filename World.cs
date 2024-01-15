using CellEvolution.Cell.NN;
using System.Collections.Generic;

namespace CellEvolution
{
    public class World
    {
        private readonly object lockObject = new object();

        public char[,] AreaChar;
        public ConsoleColor[,] AreaColor;
        public int[,] AreaVoice;
        public int[,] AreaEnergy;

        public List<CellModel> Cells = new List<CellModel>();
        public Logic Logic;

        public World(Logic logic)
        {
            AreaChar = new char[Constants.areaSizeX, Constants.areaSizeY];
            AreaColor = new ConsoleColor[Constants.areaSizeX, Constants.areaSizeY];
            AreaEnergy = new int[Constants.areaSizeX, Constants.areaSizeY];
            AreaVoice = new int[Constants.areaSizeX, Constants.areaSizeY];

            Console.WriteLine("Creating World!");
            CreateArea();
            Console.WriteLine("World Created!");
            Logic = logic;
        }

        public void CreateVisual() ///!!!!!!!!
        {
            Console.Clear();
            lock (lockObject)
            {
                for (int y = 0; y < Constants.areaSizeY; y++)
                {
                    for (int x = 0; x < Constants.areaSizeX * 2; x++)
                    {
                        Console.CursorVisible = false;
                        Console.SetCursorPosition(x, y);

                        if (x % 2 == 0)
                        {
                            if (Constants.borderChar == AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.wallColor;
                                AreaColor[x / 2, y] = Console.ForegroundColor;
                            }
                            else if (Constants.cellChar == AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = GetCell(x / 2, y).CellColor;
                                AreaColor[x / 2, y] = Console.ForegroundColor;
                            }
                            else if (Constants.wallChar == AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.wallColor;
                                AreaColor[x / 2, y] = Console.ForegroundColor;
                            }
                            else if (Constants.emptyChar == AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.emptyColor;
                                AreaColor[x / 2, y] = Console.ForegroundColor;
                            }
                            else if (Constants.poisonChar == AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.poisonColor;
                                AreaColor[x / 2, y] = Console.ForegroundColor;
                            }
                            Console.Write(AreaChar[x / 2, y]);
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.Write(Constants.nullChar);
                        }
                    }
                }
            }
            Console.ResetColor();

        }
        private void CreateArea()
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
                        Cells.Add(new CellModel(x, y, this));
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
                FillArea();
            });
            taskEmpty.Start();

            taskX.Wait();
            taskY.Wait();
            taskCell.Wait();
            taskEmpty.Wait();
        }
        public void FillArea()
        {
            Parallel.For(1, Constants.areaSizeY - 1, y =>
            {
                for (int x = 1; x < Constants.areaSizeX - 1; x++)
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
                }
            });
        }

        public void CellMove(int i)
        {
            if (i < Cells.Count)
            {
                if (Cells[i].IsDead == false)
                {
                    int LastX = Cells[i].PositionX;
                    int LastY = Cells[i].PositionY;

                    Cells[i].MakeAction();
                    lock (lockObject)
                    {
                        if (!(Cells[i].PositionX == LastX && Cells[i].PositionY == LastY))
                        {
                            CellChangePos(Cells[i], LastX, LastY);
                        }
                        else
                        {
                            Console.CursorVisible = false;
                            Console.SetCursorPosition(Cells[i].PositionX * 2, Cells[i].PositionY);
                            if (IsAreaPoisoned(LastX, LastY))
                            {
                                CreatePoisonArea(LastX, LastY);
                            }
                            else
                            {
                                Console.ForegroundColor = Cells[i].CellColor;
                                AreaColor[Cells[i].PositionX, Cells[i].PositionY] = Cells[i].CellColor;
                                Console.Write(Constants.cellChar);
                            }
                            Console.ResetColor();
                        }
                    }
                }
            }
        }
        private void CellChangePos(CellModel cell, int lastX, int lastY)
        {
            lock (lockObject)
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(cell.PositionX * 2, cell.PositionY);
                Console.ForegroundColor = cell.CellColor;

                AreaColor[cell.PositionX, cell.PositionY] = cell.CellColor;
                AreaChar[cell.PositionX, cell.PositionY] = Constants.cellChar;

                Console.Write(Constants.cellChar);
                Console.ResetColor();

                if (IsAreaPoisoned(lastX, lastY))
                {
                    CreatePoisonArea(lastX, lastY);
                }
                else
                {
                    Console.SetCursorPosition(lastX * 2, lastY);
                    Console.Write(Constants.emptyChar);

                    AreaChar[lastX, lastY] = Constants.emptyChar;
                    AreaColor[lastX, lastY] = Constants.emptyColor;
                }
            }

        }

        public CellModel GetCell(int x, int y)
        {
            lock (lockObject)
            {
                return Cells.FirstOrDefault(c => c.PositionX == x && c.PositionY == y);
            }
        }
        public void CheckArea()
        {
            for (int x = 0; x < Constants.areaSizeX; x++)
            {
                for (int y = 0; y < Constants.areaSizeY; y++)
                {
                    if (AreaChar[x, y] != Constants.cellChar && AreaChar[x, y] != Constants.wallChar)
                    {
                        Console.CursorVisible = false;
                        Console.SetCursorPosition(x * 2, y);
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write(AreaChar[x, y]);
                        Console.ResetColor();
                    }

                }

            }

        }
        public int GetNumOfLiveCellsAround(int positionX, int positionY)
        {
            lock (lockObject)
            {
                List<int> area = GetAreaCharAroundCellInt(positionX, positionY, 1);
                int res = 0;

                for (int i = 0; i < area.Count; i++)
                {
                    if (area[i] >= 4 && area[i] != 11) res++;
                }


                return res;
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
                        if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX))
                        {
                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: break;
                                    case Constants.emptyChar: k = 1; break;
                                    case Constants.wallChar: k = 2; break;
                                    case Constants.poisonChar: k = 3; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = 4; break; // new
                                                case Constants.biteCellColor: k = 5; break; // hunter
                                                case Constants.photoCellColor: k = 6; break; // plant
                                                case Constants.absorbCellColor: k = 7; break; // mushroom
                                                case Constants.wallDestroyerCellColor: k = 8; break; // bird
                                                case Constants.slipCellColor: k = 9; break; // slip
                                                case Constants.evolvingCellColor: k = 10; break; // evolving
                                                case Constants.deadCellColor: k = 11; break; // dead
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
                        if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX))
                        {
                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: break;
                                    case Constants.emptyChar: k = 1; break;
                                    case Constants.wallChar: k = 2; break;
                                    case Constants.poisonChar: k = 3; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = 4; break; // new
                                                case Constants.biteCellColor: k = 5; break; // hunter
                                                case Constants.photoCellColor: k = 6; break; // plant
                                                case Constants.absorbCellColor: k = 7; break; // mushroom
                                                case Constants.wallDestroyerCellColor: k = 8; break; // bird
                                                case Constants.slipCellColor: k = 9; break; // slip
                                                case Constants.evolvingCellColor: k = 10; break; // evolving
                                                case Constants.deadCellColor: k = 11; break; // dead
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
                List<int> area = new List<int>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1));
                List<int> cellGenArea = new List<int>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1));
                List<int> energyAreaInfo = new List<int>((Constants.energyAreaVisionDistance * 2 + 1) * (Constants.energyAreaVisionDistance * 2 + 1));

                for (int x = positionX - Constants.visionDistance; x <= positionX + Constants.visionDistance; x++)
                {
                    for (int y = positionY - Constants.visionDistance; y <= positionY + Constants.visionDistance; y++)
                    {
                        if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX))
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
                                    case Constants.borderChar: break;
                                    case Constants.emptyChar: k = 1; break;
                                    case Constants.wallChar: k = 2; break;
                                    case Constants.poisonChar: k = 3; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = 4; break; // new
                                                case Constants.biteCellColor: k = 5; break; // hunter
                                                case Constants.photoCellColor: k = 6; break; // plant
                                                case Constants.absorbCellColor: k = 7; break; // mushroom
                                                case Constants.wallDestroyerCellColor: k = 8; break; // bird
                                                case Constants.slipCellColor: k = 9; break; // slip
                                                case Constants.evolvingCellColor: k = 10; break; // evolving
                                                case Constants.deadCellColor: k = 11; break; // dead
                                            }

                                            cellGenArea.Add(GenomeSimilarity(GetCell(x, y), GetCell(positionX, positionY)) + 1);
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
                            area.Add(0);
                            cellGenArea.Add(0);
                        }
                    }
                }

                area.AddRange(cellGenArea);
                area.AddRange(energyAreaInfo);
                area.AddRange(GetAreaVoiceInfo(positionX, positionY));

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
                    res.Add(positionX - Cells[AreaVoice[positionX, positionY] - 1].PositionX);
                    res.Add(positionY - Cells[AreaVoice[positionX, positionY] - 1].PositionY);

                    res.Add(GenomeSimilarity(GetCell(positionX, positionY), Cells[AreaVoice[positionX, positionY] - 1]));
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].Energy);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].EnergyBank);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].MaxClone);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].Initiation);
                }
                else
                {
                    for(int i = 0; i < 7; i++)
                    {
                        res.Add(0);
                    }
                }

                return res;
            }
        }
        private int GenomeSimilarity(CellModel cellA, CellModel cellB)
        {
            double simK = 0;

            if (cellA == null || cellB == null)
            {
                return 0;
            }

            lock (lockObject)
            {
                for (int i = 0; i < cellA.gen.GenActionsCycle.Length; i++)
                {
                    if (cellA.gen.GenActionsCycle[i] == cellB.gen.GenActionsCycle[i])
                    {
                        simK++;
                    }
                }
            }

            double temp = simK * 100.0 / cellA.gen.GenActionsCycle.Length;

            return (int)temp;
        }

        private List<(int, int)> FindAllEmptyCharNearCellCoord(int positionX, int positionY)
        {

            List<(int, int)> area = new List<(int, int)>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);

            for (int x = positionX - Constants.visionDistance; x < positionX + Constants.visionDistance + 1; x++)
            {
                for (int y = positionY - Constants.visionDistance; y < positionY + Constants.visionDistance + 1; y++)
                {
                    if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX) && !(x == positionX && y == positionY))
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
        private List<(int, int)> FindAllCellCharNearCellCoord(int positionX, int positionY)
        {

            List<(int, int)> area = new List<(int, int)>((Constants.visionDistance * 2 + 1) * (Constants.visionDistance * 2 + 1) - 1);

            for (int x = positionX - Constants.visionDistance; x < positionX + Constants.visionDistance + 1; x++)
            {
                for (int y = positionY - Constants.visionDistance; y < positionY + Constants.visionDistance + 1; y++)
                {
                    if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX) && !(x == positionX && y == positionY))
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

        public void StartCreatingClones()
        {
            lock (lockObject)
            {
                for (int i = 0; i < Cells.Count; i++)
                {
                    if (Cells[i].IsCreatingClone)
                    {
                        List<(int, int)> newCellCoord = FindAllEmptyCharNearCellCoord(Cells[i].PositionX, Cells[i].PositionY);
                        for (int j = 0; j < Cells[i].MaxClone; j++)
                        {
                            if (newCellCoord.Count > 0)
                            {
                                Random random = new Random();
                                int k = random.Next(0, newCellCoord.Count);
                                if (AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] == Constants.emptyChar && Cells[i].Energy > (Constants.cloneEnergyCost + Cells[i].EnergyBank) * 2)
                                {
                                    Cells.Add(new CellModel(newCellCoord[k].Item1, newCellCoord[k].Item2, this, Cells[i]));

                                    AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.cellChar;
                                    AreaColor[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.newCellColor;

                                    int temp = (Constants.cloneEnergyCost + Cells[i].EnergyBank);
                                    if (temp > 0)
                                    {
                                        Cells[i].Energy -= temp;
                                    }

                                }
                                newCellCoord.Remove(newCellCoord[k]);
                            }
                        }

                    }
                }
            }
        }
        public void StartReproduction()
        {
            lock (lockObject)
            {
                for (int i = 0; i < Cells.Count; i++)
                {
                    if (Cells[i].IsReproducting)
                    {
                        List<(int, int)> otherCellCoord = FindAllCellCharNearCellCoord(Cells[i].PositionX, Cells[i].PositionY);
                        bool IsFindPartner = false;
                        int otherPartnerX = 0;
                        int otherPartnerY = 0;

                        // код подбора партнера
                        while (otherCellCoord.Count > 0 && IsFindPartner == false)
                        {
                            Random random = new Random();
                            int k = random.Next(0, otherCellCoord.Count);
                            CellModel temp = GetCell(otherCellCoord[k].Item1, otherCellCoord[k].Item2);
                            if (temp != null && temp.IsReproducting && temp.MaxClone != temp.AlreadyUseClone)
                            {
                                IsFindPartner = true;
                                otherPartnerX = otherCellCoord[k].Item1;
                                otherPartnerY = otherCellCoord[k].Item2;
                            }
                            otherCellCoord.Remove(otherCellCoord[k]);
                        }

                        if (IsFindPartner)
                        {
                            List<(int, int)> newCellCoord = FindAllEmptyCharNearCellCoord(Cells[i].PositionX, Cells[i].PositionY);
                            newCellCoord.AddRange(FindAllEmptyCharNearCellCoord(otherPartnerX, otherPartnerY));

                            CellModel mother;
                            CellModel father;

                            if (Cells[i].Initiation >= GetCell(otherPartnerX, otherPartnerY).Initiation)
                            {
                                father = Cells[i];
                                mother = GetCell(otherPartnerX, otherPartnerY);
                            }
                            else
                            {
                                mother = Cells[i];
                                father = GetCell(otherPartnerX, otherPartnerY);
                            }

                            for (int j = mother.AlreadyUseClone; j < mother.MaxClone; j++)
                            {
                                if (newCellCoord.Count > 0)
                                {
                                    Random random = new Random();
                                    int k = random.Next(0, newCellCoord.Count);
                                    if (AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] == Constants.emptyChar &&
                                        mother.Energy > (Constants.cloneEnergyCost + mother.EnergyBank) * 2 &&
                                        father.Energy > (Constants.cloneEnergyCost + mother.EnergyBank) * 2)
                                    {
                                        Cells.Add(new CellModel(newCellCoord[k].Item1, newCellCoord[k].Item2, this, mother, father));

                                        mother.AlreadyUseClone++;

                                        AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.cellChar;
                                        AreaColor[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.newCellColor;

                                        int tempMother = (Constants.cloneEnergyCost + mother.EnergyBank);
                                        if (tempMother > 0)
                                        {
                                            mother.Energy -= tempMother;
                                        }
                                        int tempFather = (Constants.cloneEnergyCost + father.EnergyBank);
                                        if (tempFather > 0)
                                        {
                                            father.Energy -= tempFather;
                                        }
                                    }
                                    newCellCoord.Remove(newCellCoord[k]);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void DeadCellToAreaEnergy(int i)
        {
            int x = Cells[i].PositionX;
            int y = Cells[i].PositionY;
            if (Cells[i].Energy / 4 > Constants.minEnergyFromDeadCell)
            {
                AreaEnergy[x, y] += Cells[i].Energy / 4;
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
        public void CreatePoisonArea(int x, int y)
        {
            lock (Console.Out)
            {
                AreaChar[x, y] = Constants.poisonChar;
                AreaColor[x, y] = Constants.poisonColor;

                Console.CursorVisible = false;
                Console.SetCursorPosition(x * 2, y);
                Console.ForegroundColor = Constants.poisonColor;
                Console.Write(Constants.poisonChar);
                Console.ResetColor();
            }
        }

        public void ClearDeadCells()
        {
            lock (lockObject)
            {
                for (int i = 0; i < Cells.Count; i++)
                {
                    if (Cells[i].IsDead && !Cells[i].IsCorpseEaten)
                    {
                        ClearVisualFromDeadCell(Cells[i].PositionX, Cells[i].PositionY);
                        DeadCellToAreaEnergy(i);
                    }
                }

                Cells.RemoveAll(cell => cell.IsDead);
            }
        }

        private void ClearVisualFromDeadCell(int positionX, int positionY)
        {
            if (IsAreaPoisoned(positionX, positionY))
            {
                CreatePoisonArea(positionX, positionY);
            }
            else
            {
                AreaChar[positionX, positionY] = Constants.emptyChar;
                AreaColor[positionX, positionY] = Constants.emptyColor;

                Console.CursorVisible = false;
                Console.SetCursorPosition(positionX * 2, positionY);
                Console.Write(Constants.emptyChar);
            }
        }

        public void Hunt(CellModel hunter, CellModel victim)
        {
            lock (lockObject)
            {
                if (victim != null)
                {
                    if (victim.IsDead)
                    {
                        ClearVisualFromDeadCell(victim.PositionX, victim.PositionY);
                        victim.IsCorpseEaten = true;

                        hunter.Energy += victim.Energy / 4;
                        if (victim.Energy / 4 > Constants.minEnergyFromDeadCell)
                        {
                            hunter.Energy += victim.Energy / 4;
                        }
                        else
                        {
                            hunter.Energy += Constants.minEnergyFromDeadCell;
                        }
                    }
                    else
                    {
                        victim.IsDead = true;
                        hunter.Energy += (int)(victim.Energy * 2.0 / 3.0);

                        victim.CellColor = Constants.deadCellColor;

                        Console.SetCursorPosition(victim.PositionX * 2, victim.PositionY);
                        Console.ForegroundColor = victim.CellColor;
                        Console.Write(Constants.cellChar);
                        Console.ResetColor();

                    }
                }
            }

        }

        public int GetCurrentYear() => Logic.CurrentYear;
        public bool IsVictimExists(int positionX, int positionY)
        {
            lock (lockObject)
            {
                return Cells.Any(cell => cell.PositionX == positionX && cell.PositionY == positionY);
            }
        }
        public bool IsAreaPoisoned(int x, int y) => AreaEnergy[x, y] > Constants.energyAreaPoisonedCorner;
        public bool IsDay() => Logic.CurrentDayTime == Logic.DayTime.Day;

        public bool IsMoveAvailable(int positionX, int positionY) => positionX > 0 && positionY > 0 &&
                                                                     positionX < Constants.areaSizeX && positionY < Constants.areaSizeY &&
                                                                     (AreaChar[positionX, positionY] == Constants.emptyChar ||
                                                                      AreaChar[positionX, positionY] == Constants.poisonChar);

        public void CellShout(CellModel cell)
        {
            int index = -1;
            for(int i = 0;  i < Cells.Count; i++)
            {
                if (Cells[i] == cell)
                {
                    index = i; break;
                }
            }

            if (index != -1)
            {
                for (int x = cell.PositionX - Constants.voiceDistance; x < cell.PositionX + Constants.voiceDistance + 1; x++)
                {
                    for (int y = cell.PositionY - Constants.voiceDistance; y < cell.PositionY + Constants.voiceDistance + 1; y++)
                    {
                        if ((y >= 0 && y < Constants.areaSizeY) &&
                            (x >= 0 && x < Constants.areaSizeX) &&
                             AreaVoice[x, y] != 0 && 
                             Cells[index].Initiation >= Cells[AreaVoice[x, y] - 1].Initiation)
                        {
                                    AreaVoice[x, y] = index + 1;
                        }
                    }
                }
            }
        }
        public void ClearAreaVoiceParallel()
        {
            Parallel.For(0,Constants.areaSizeX, x =>
            {
                for (int y = 0; y < Constants.areaSizeY; y++)
                {
                    AreaVoice[x, y] = 0;
                }
            });
        }
    }
}
