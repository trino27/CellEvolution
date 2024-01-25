using CellEvolution.Cell.NN;
using System.Collections.Generic;

namespace CellEvolution
{
    public class World
    {
        private object lockObject = new object();

        public char[,] AreaChar;
        public ConsoleColor[,] AreaColor;
        public int[,] AreaVoice;
        public int[,] AreaEnergy;

        public List<CellModel> Cells = new List<CellModel>();
        public Logic Logic;
        public WorldRenderer Renderer;

        private bool IsRenderAllow = false;
        private bool IsRenderExists = false;

        public World(Logic logic)
        {
            AreaChar = new char[Constants.areaSizeX, Constants.areaSizeY];
            AreaColor = new ConsoleColor[Constants.areaSizeX, Constants.areaSizeY];
            AreaEnergy = new int[Constants.areaSizeX, Constants.areaSizeY];
            AreaVoice = new int[Constants.areaSizeX, Constants.areaSizeY];

            Console.WriteLine("Creating World!");
            CreateAreas();
            Console.WriteLine("World Created!");
            Logic = logic;
        }
        private void CreateAreaColor()
        {
            for (int y = 0; y < Constants.areaSizeY; y++)
            {
                for (int x = 0; x < Constants.areaSizeX; x++)
                {
                    if (Constants.borderChar == AreaChar[x, y])
                    {
                        Console.ForegroundColor = Constants.borderColor;
                        AreaColor[x, y] = Console.ForegroundColor;
                    }
                    else if (Constants.cellChar == AreaChar[x, y])
                    {
                        Console.ForegroundColor = GetCell(x, y).CellColor;
                        AreaColor[x, y] = Console.ForegroundColor;
                    }
                    else if (Constants.emptyChar == AreaChar[x, y])
                    {
                        Console.ForegroundColor = Constants.emptyColor;
                        AreaColor[x, y] = Console.ForegroundColor;
                    }
                    else if (Constants.poisonChar == AreaChar[x, y])
                    {
                        Console.ForegroundColor = Constants.poisonColor;
                        AreaColor[x, y] = Console.ForegroundColor;
                    }
                }
            }
        }
        private void CreateAreas()
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
                        Cells.Add(new CellModel(x, y, this, i));
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

            CreateAreaColor();
        }

        public void InitWorldRenderer(WorldRenderer worldRenderer)
        {
            Renderer = worldRenderer;
            IsRenderExists = true;
        }
        public bool StartRenderIfRendererExist()
        {
            if (IsRenderExists)
            {
                IsRenderAllow = true;
                return false;
            }
            return true;
        }
        public void StopRenderer() => IsRenderAllow = false;

        public void FillAreaParallel()
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

        //Cell Manager
        public void CellMove(int i)
        {
            if (i < Cells.Count && !Cells[i].IsDead)
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
                        if (IsAreaPoisoned(LastX, LastY))
                        {
                            CreatePoisonArea(LastX, LastY);
                        }
                        else
                        {
                            AreaColor[Cells[i].PositionX, Cells[i].PositionY] = Cells[i].CellColor;

                            if (IsRenderAllow)
                            {
                                Renderer.VisualChange(Cells[i].PositionX, Cells[i].PositionY, Constants.cellChar, Cells[i].CellColor);
                            }
                        }
                    }
                }
            }
        }
        private void CellChangePos(CellModel cell, int lastX, int lastY)
        {
            lock (lockObject)
            {
                if (IsRenderAllow)
                {
                    Renderer.VisualChange(cell.PositionX, cell.PositionY, Constants.cellChar, cell.CellColor);
                }

                AreaColor[cell.PositionX, cell.PositionY] = cell.CellColor;
                AreaChar[cell.PositionX, cell.PositionY] = Constants.cellChar;

                if (IsAreaPoisoned(lastX, lastY))
                {
                    CreatePoisonArea(lastX, lastY);
                }
                else
                {
                    if (IsRenderAllow)
                    {
                        Renderer.VisualChange(lastX, lastY, Constants.emptyChar, Constants.emptyColor);
                    }

                    AreaChar[lastX, lastY] = Constants.emptyChar;
                    AreaColor[lastX, lastY] = Constants.emptyColor;
                }
            }

        }
        public void CellHunt(CellModel hunter, CellModel victim)
        {
            lock (lockObject)
            {
                if (victim != null)
                {
                    if (victim.IsDead)
                    {
                        ClearAreaFromDeadCell(victim.PositionX, victim.PositionY);
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

                        if (IsRenderAllow)
                        {
                            Renderer.VisualChange(victim.PositionX, victim.PositionY, Constants.cellChar, victim.CellColor);
                        }
                    }
                }
            }

        }
        public void CellAbsorb(CellModel absorber)
        {
            lock (lockObject)
            {
                for (int x = absorber.PositionX - Constants.energyAreaAbsorbDistance; x <= absorber.PositionX + Constants.energyAreaAbsorbDistance; x++)
                {
                    for (int y = absorber.PositionY - Constants.energyAreaAbsorbDistance; y <= absorber.PositionY + Constants.energyAreaAbsorbDistance; y++)
                    {
                        if (absorber.PositionX == x && absorber.PositionY == y)
                        {
                            absorber.Energy += AreaEnergy[x, y];
                            AreaEnergy[x, y] = 0;
                            ClearAreaFromPoison(x, y);
                        }
                        else
                        {
                            absorber.Energy += AreaEnergy[x, y] / 4;
                            AreaEnergy[x, y] = AreaEnergy[x, y] / 4;

                            if (AreaEnergy[x, y] < Constants.energyAreaPoisonedCorner)
                            {
                                ClearAreaFromPoison(x, y);
                            }
                        }
                    }
                }
            }
        }
        public void CellShout(CellModel cell)
        {
            int index = -1;
            for (int i = 0; i < Cells.Count; i++)
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
        public void CellStartCreatingClones()
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
        public void CellStartReproduction()
        {
            lock (lockObject)
            {
                for (int i = 0; i < Cells.Count; i++)
                {
                    if (Cells[i].IsCreatingChildren)
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
                            if (temp != null && temp.IsCreatingChildren && temp.MaxClone != temp.AlreadyUseClone)
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
        } //Separate
        private int CellGenomeSimilarity(CellModel cellA, CellModel cellB)
        {
            double simK = 0;

            if (cellA == null || cellB == null)
            {
                return 0;
            }

            lock (lockObject)
            {
                for (int i = 0; i < cellA.GetGenomeCycle().Length; i++)
                {
                    if (cellA.GetGenomeCycle()[i] == cellB.GetGenomeCycle()[i])
                    {
                        simK++;
                    }
                }
            }

            double temp = simK * 100.0 / cellA.GetGenomeCycle().Length;

            return (int)temp;
        }

        public bool IsVictimExists(int positionX, int positionY)
        {
            lock (lockObject)
            {
                return Cells.Any(cell => cell.PositionX == positionX && cell.PositionY == positionY);
            }
        }
        public bool IsMoveAvailable(int positionX, int positionY) => positionX > 0 && positionY > 0 &&
                                                                     positionX < Constants.areaSizeX && positionY < Constants.areaSizeY &&
                                                                     (AreaChar[positionX, positionY] == Constants.emptyChar ||
                                                                      AreaChar[positionX, positionY] == Constants.poisonChar);
        //!Cell Manager

        public CellModel GetCell(int x, int y)
        {
            lock (lockObject)
            {
                return Cells.FirstOrDefault(c => c.PositionX == x && c.PositionY == y);
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
                    if (area[i] >= Constants.KnewCell && area[i] < Constants.KdeadCell) res++;
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
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
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
                        if ((y >= 0 && y < Constants.areaSizeY) && (x >= 0 && x < Constants.areaSizeX))
                        {
                            if (!(x == positionX && y == positionY))
                            {
                                int k = 0;
                                switch (AreaChar[x, y])
                                {
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
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
                                    case Constants.borderChar: k = Constants.Kborder; break;
                                    case Constants.emptyChar: k = Constants.Kempty; break;
                                    case Constants.poisonChar: k = Constants.Kpoison; break;
                                    case Constants.cellChar:
                                        {
                                            switch (AreaColor[x, y])
                                            {
                                                case Constants.newCellColor: k = Constants.KnewCell; break; // new
                                                case Constants.biteCellColor: k = Constants.KbiteCell; break; // hunter
                                                case Constants.photoCellColor: k = Constants.KphotoCell; break; // plant
                                                case Constants.absorbCellColor: k = Constants.KabsorbCell; break; // mushroom
                                                case Constants.slipCellColor: k = Constants.KslipCell; break; // slip
                                                case Constants.evolvingCellColor: k = Constants.KevolvingCell; break; // evolving
                                                case Constants.errorCellColor: k = Constants.KerrorCell; break; // error
                                                case Constants.deadCellColor: k = Constants.KdeadCell; break; // dead
                                            }

                                            cellGenArea.Add(CellGenomeSimilarity(GetCell(x, y), GetCell(positionX, positionY)) + 1);
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
                area.Add(Convert.ToInt16(IsDay()) * Constants.brainInputDayNightPoweredK);

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

                    res.Add(CellGenomeSimilarity(GetCell(positionX, positionY), Cells[AreaVoice[positionX, positionY] - 1]));
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].Energy);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].EnergyBank);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].MaxClone);
                    res.Add(Cells[AreaVoice[positionX, positionY] - 1].Initiation);
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

                if (IsRenderAllow)
                {
                    Renderer.VisualChange(x, y, Constants.poisonChar, Constants.poisonColor);
                }
            }
        }
        public void ClearAreaFromPoison(int x, int y) 
        {
            if (AreaChar[x, y] == Constants.poisonChar)
            {
                AreaChar[x, y] = Constants.emptyChar;
                AreaColor[x, y] = Constants.emptyColor;

                if (IsRenderAllow)
                {
                    Renderer.VisualChange(x, y, Constants.emptyChar, Constants.emptyColor);
                }
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
                        ClearAreaFromDeadCell(Cells[i].PositionX, Cells[i].PositionY);
                        DeadCellToAreaEnergy(i);
                    }
                }

                Cells.RemoveAll(cell => cell.IsDead);
            }
        }

        private void ClearAreaFromDeadCell(int positionX, int positionY)
        {
            if (IsAreaPoisoned(positionX, positionY))
            {
                CreatePoisonArea(positionX, positionY);
            }
            else
            {
                AreaChar[positionX, positionY] = Constants.emptyChar;
                AreaColor[positionX, positionY] = Constants.emptyColor;


                if (IsRenderAllow)
                {
                    Renderer.VisualChange(positionX, positionY, Constants.emptyChar, Constants.emptyColor);
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

        public bool IsAreaPoisoned(int x, int y) => AreaEnergy[x, y] > Constants.energyAreaPoisonedCorner;
        public bool IsDay() => Logic.CurrentDayTime == Logic.DayTime.Day;
    }
}
