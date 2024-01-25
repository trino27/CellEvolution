using CellEvolution;
using CellEvolution.Cell.NN;
using СellEvolution.WorldResources;

namespace СellEvolution.Cell
{
    public class CellActionHandler
    {
        private readonly World world;
        private object lockObject = new object();

        public CellActionHandler(World world)
        {
            this.world = world;
        }

        public void CellMove(int i)
        {
            if (i < world.Cells.Count && !world.Cells[i].IsDead)
            {
                int LastX = world.Cells[i].PositionX;
                int LastY = world.Cells[i].PositionY;

                world.Cells[i].MakeAction();
                lock (lockObject)
                {
                    if (!(world.Cells[i].PositionX == LastX && world.Cells[i].PositionY == LastY))
                    {
                        CellChangePos(world.Cells[i], LastX, LastY);
                    }
                    else
                    {
                        if (world.WorldArea.IsAreaPoisoned(LastX, LastY))
                        {
                            world.WorldArea.CreatePoisonArea(LastX, LastY);
                        }
                        else
                        {
                            world.WorldArea.AreaColor[world.Cells[i].PositionX, world.Cells[i].PositionY] = world.Cells[i].CellColor;

                            if (world.IsRenderAllow)
                            {
                                world.worldRenderer.VisualChange(world.Cells[i].PositionX, world.Cells[i].PositionY, Constants.cellChar, world.Cells[i].CellColor);
                            }
                        }
                    }
                }
            }
        }
        public void CellChangePos(CellModel cell, int lastX, int lastY)
        {
            lock (lockObject)
            {
                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(cell.PositionX, cell.PositionY, Constants.cellChar, cell.CellColor);
                }

                world.WorldArea.AreaColor[cell.PositionX, cell.PositionY] = cell.CellColor;
                world.WorldArea.AreaChar[cell.PositionX, cell.PositionY] = Constants.cellChar;

                if (world.WorldArea.IsAreaPoisoned(lastX, lastY))
                {
                    world.WorldArea.CreatePoisonArea(lastX, lastY);
                }
                else
                {
                    if (world.IsRenderAllow)
                    {
                        world.worldRenderer.VisualChange(lastX, lastY, Constants.emptyChar, Constants.emptyColor);
                    }

                    world.WorldArea.AreaChar[lastX, lastY] = Constants.emptyChar;
                    world.WorldArea.AreaColor[lastX, lastY] = Constants.emptyColor;
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
                        world.WorldArea.ClearAreaFromDeadCell(victim.PositionX, victim.PositionY);
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

                        if (world.IsRenderAllow)
                        {
                            world.worldRenderer.VisualChange(victim.PositionX, victim.PositionY, Constants.cellChar, victim.CellColor);
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
                            absorber.Energy += world.WorldArea.AreaEnergy[x, y];
                            world.WorldArea.AreaEnergy[x, y] = 0;
                            world.WorldArea.ClearAreaFromPoison(x, y);
                        }
                        else
                        {
                            absorber.Energy += world.WorldArea.AreaEnergy[x, y] / 4;
                            world.WorldArea.AreaEnergy[x, y] = world.WorldArea.AreaEnergy[x, y] / 4;

                            if (world.WorldArea.AreaEnergy[x, y] < Constants.energyAreaPoisonedCorner)
                            {
                                world.WorldArea.ClearAreaFromPoison(x, y);
                            }
                        }
                    }
                }
            }
        }
        public void CellShout(CellModel cell)
        {
            int index = -1;
            for (int i = 0; i < world.Cells.Count; i++)
            {
                if (world.Cells[i] == cell)
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
                        if (y >= 0 && y < Constants.areaSizeY &&
                            x >= 0 && x < Constants.areaSizeX &&
                             world.WorldArea.AreaVoice[x, y] != 0 &&
                             world.Cells[index].Initiation >= world.Cells[world.WorldArea.AreaVoice[x, y] - 1].Initiation)
                        {
                            world.WorldArea.AreaVoice[x, y] = index + 1;
                        }
                    }
                }
            }
        }
        public void CellStartCreatingClones()
        {
            lock (lockObject)
            {
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    if (world.Cells[i].IsCreatingClone)
                    {
                        List<(int, int)> newCellCoord = world.WorldArea.FindAllEmptyCharNearCellCoord(world.Cells[i].PositionX, world.Cells[i].PositionY);
                        for (int j = 0; j < world.Cells[i].MaxClone; j++)
                        {
                            if (newCellCoord.Count > 0)
                            {
                                Random random = new Random();
                                int k = random.Next(0, newCellCoord.Count);
                                if (world.WorldArea.AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] == Constants.emptyChar && world.Cells[i].Energy > (Constants.cloneEnergyCost + world.Cells[i].EnergyBank) * 2)
                                {
                                    world.Cells.Add(new CellModel(newCellCoord[k].Item1, newCellCoord[k].Item2, world, world.Cells[i]));

                                    world.WorldArea.AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.cellChar;
                                    world.WorldArea.AreaColor[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.newCellColor;

                                    int temp = Constants.cloneEnergyCost + world.Cells[i].EnergyBank;
                                    if (temp > 0)
                                    {
                                        world.Cells[i].Energy -= temp;
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
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    if (world.Cells[i].IsCreatingChildren)
                    {
                        List<(int, int)> otherCellCoord = world.WorldArea.FindAllCellCharNearCellCoord(world.Cells[i].PositionX, world.Cells[i].PositionY);
                        bool IsFindPartner = false;
                        int otherPartnerX = 0;
                        int otherPartnerY = 0;

                        while (otherCellCoord.Count > 0 && IsFindPartner == false)
                        {
                            Random random = new Random();
                            int k = random.Next(0, otherCellCoord.Count);
                            CellModel temp = world.GetCell(otherCellCoord[k].Item1, otherCellCoord[k].Item2);
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
                            List<(int, int)> newCellCoord = world.WorldArea.FindAllEmptyCharNearCellCoord(world.Cells[i].PositionX, world.Cells[i].PositionY);
                            newCellCoord.AddRange(world.WorldArea.FindAllEmptyCharNearCellCoord(otherPartnerX, otherPartnerY));

                            CellModel mother;
                            CellModel father;

                            if (world.Cells[i].Initiation >= world.GetCell(otherPartnerX, otherPartnerY).Initiation)
                            {
                                father = world.Cells[i];
                                mother = world.GetCell(otherPartnerX, otherPartnerY);
                            }
                            else
                            {
                                mother = world.Cells[i];
                                father = world.GetCell(otherPartnerX, otherPartnerY);
                            }

                            for (int j = mother.AlreadyUseClone; j < mother.MaxClone; j++)
                            {
                                if (newCellCoord.Count > 0)
                                {
                                    Random random = new Random();
                                    int k = random.Next(0, newCellCoord.Count);
                                    if (world.WorldArea.AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] == Constants.emptyChar &&
                                        mother.Energy > (Constants.cloneEnergyCost + mother.EnergyBank) * 2 &&
                                        father.Energy > (Constants.cloneEnergyCost + mother.EnergyBank) * 2)
                                    {
                                        world.Cells.Add(new CellModel(newCellCoord[k].Item1, newCellCoord[k].Item2, world, mother, father));

                                        mother.AlreadyUseClone++;

                                        world.WorldArea.AreaChar[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.cellChar;
                                        world.WorldArea.AreaColor[newCellCoord[k].Item1, newCellCoord[k].Item2] = Constants.newCellColor;

                                        int tempMother = Constants.cloneEnergyCost + mother.EnergyBank;
                                        if (tempMother > 0)
                                        {
                                            mother.Energy -= tempMother;
                                        }
                                        int tempFather = Constants.cloneEnergyCost + father.EnergyBank;
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
        public int CellGenomeSimilarity(CellModel cellA, CellModel cellB)
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
                return world.Cells.Any(cell => cell.PositionX == positionX && cell.PositionY == positionY);
            }
        }
        public bool IsMoveAvailable(int positionX, int positionY) => positionX > 0 && positionY > 0 &&
                                                                     positionX < Constants.areaSizeX && positionY < Constants.areaSizeY &&
                                                                     (world.WorldArea.AreaChar[positionX, positionY] == Constants.emptyChar ||
                                                                      world.WorldArea.AreaChar[positionX, positionY] == Constants.poisonChar);
    }
}
