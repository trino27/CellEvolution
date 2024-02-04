using CellEvolution;
using CellEvolution.Cell.NN;
using СellEvolution.WorldResources;

namespace СellEvolution.WorldResources.Cell
{
    public class CellActionHandler
    {
        Random random = new Random();

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
                int lastX = world.Cells[i].PositionX;
                int lastY = world.Cells[i].PositionY;

                world.Cells[i].MakeAction();

                if (world.Cells[i].PositionX != lastX || world.Cells[i].PositionY != lastY)
                {
                    CellChangePos(world.Cells[i], lastX, lastY);
                }
                else
                {
                    if (world.WorldArea.IsAreaPoisoned(lastX, lastY))
                    {
                        world.WorldArea.CreatePoisonArea(lastX, lastY);
                    }
                    else
                    {
                        UpdateAreaAfterMove(world.Cells[i]);
                    }
                }
            }
        }

        private void UpdateAreaAfterMove(CellModel cell)
        {
            lock (lockObject)
            {
                int posX = cell.PositionX;
                int posY = cell.PositionY;

                world.WorldArea.AreaColor[posX, posY] = cell.CellColor;

                if (world.IsRenderAllow)
                {
                    world.worldRenderer.VisualChange(posX, posY, Constants.cellChar, cell.CellColor);
                }
            }
        }

        public void CellChangePos(CellModel cell, int lastX, int lastY)
        {
            lock (lockObject)
            {
                UpdateAreaAfterMove(cell);

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

        public void CellMineTop(CellModel miner)
        {
            int addEnergy = 0;
            int y = miner.PositionY - 1;
            for (int x = miner.PositionX - 1; x <= miner.PositionX + 1; x++)
            {
                if (world.WorldArea.GetMeteorBlock(x, y) != null)
                {
                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum >= Constants.mineAmount)
                    {
                        world.WorldArea.GetMeteorBlock(x, y).OrbNum -= Constants.mineAmount;

                        addEnergy += Constants.mineAmount;
                    }
                    else
                    {
                        addEnergy += world.WorldArea.GetMeteorBlock(x, y).OrbNum;

                        world.WorldArea.GetMeteorBlock(x, y).OrbNum = 0;
                    }

                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum <= 0)
                    {
                        world.WorldArea.ClearMeteorBlock(world.WorldArea.GetMeteorBlock(x, y));
                    }
                }
            }


            miner.Energy += addEnergy;
        }
        public void CellMineRightSide(CellModel miner)
        {
            int addEnergy = 0;
            int x = miner.PositionX + 1;
            for (int y = miner.PositionY - 1; y <= miner.PositionY + 1; y++)
            {
                if (world.WorldArea.GetMeteorBlock(x, y) != null)
                {
                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum >= Constants.mineAmount)
                    {
                        world.WorldArea.GetMeteorBlock(x, y).OrbNum -= Constants.mineAmount;

                        addEnergy += Constants.mineAmount;
                    }
                    else
                    {
                        addEnergy += world.WorldArea.GetMeteorBlock(x, y).OrbNum;

                        world.WorldArea.GetMeteorBlock(x, y).OrbNum = 0;
                    }

                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum <= 0)
                    {
                        world.WorldArea.ClearMeteorBlock(world.WorldArea.GetMeteorBlock(x, y));
                    }
                }
            }


            miner.Energy += addEnergy;
        }
        public void CellMineBottom(CellModel miner)
        {
            int addEnergy = 0;
            int y = miner.PositionY + 1;
            for (int x = miner.PositionX - 1; x <= miner.PositionX + 1; x++)
            {
                if (world.WorldArea.GetMeteorBlock(x, y) != null)
                {
                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum >= Constants.mineAmount)
                    {
                        world.WorldArea.GetMeteorBlock(x, y).OrbNum -= Constants.mineAmount;

                        addEnergy += Constants.mineAmount;
                    }
                    else
                    {
                        addEnergy += world.WorldArea.GetMeteorBlock(x, y).OrbNum;

                        world.WorldArea.GetMeteorBlock(x, y).OrbNum = 0;
                    }

                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum <= 0)
                    {
                        world.WorldArea.ClearMeteorBlock(world.WorldArea.GetMeteorBlock(x, y));
                    }
                }
            }


            miner.Energy += addEnergy;
        }
        public void CellMineLeftSide(CellModel miner)
        {
            int addEnergy = 0;
            int x = miner.PositionX - 1;
            for (int y = miner.PositionY - 1; y <= miner.PositionY + 1; y++)
            {
                if (world.WorldArea.GetMeteorBlock(x, y) != null)
                {
                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum >= Constants.mineAmount)
                    {
                        world.WorldArea.GetMeteorBlock(x, y).OrbNum -= Constants.mineAmount;

                        addEnergy += Constants.mineAmount;
                    }
                    else
                    {
                        addEnergy += world.WorldArea.GetMeteorBlock(x, y).OrbNum;

                        world.WorldArea.GetMeteorBlock(x, y).OrbNum = 0;
                    }

                    if (world.WorldArea.GetMeteorBlock(x, y).OrbNum <= 0)
                    {
                        world.WorldArea.ClearMeteorBlock(world.WorldArea.GetMeteorBlock(x, y));
                    }
                }
            }


            miner.Energy += addEnergy;
        }

        public void CellStartCreatingClones()
        {
            lock (lockObject)
            {
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    var cell = world.Cells[i];

                    if (cell.IsCreatingClone)
                    {
                        CreateClonesForCell(cell);
                        cell.Energy = Constants.startCellEnergy;
                    }
                }

            }
        }
        private void CreateClonesForCell(CellModel cell)
        {
            List<(int, int)> newCellCoord = world.WorldArea.FindAllEmptyCharNearCellCoord(cell.PositionX, cell.PositionY);

            for (int j = 0; j < Constants.maxClone && newCellCoord.Count > 0; j++)
            {
                int randomIndex = GetRandomIndex(newCellCoord.Count);
                (int x, int y) = newCellCoord[randomIndex];

                if (world.WorldArea.AreaChar[x, y] == Constants.emptyChar &&
                    cell.Energy >= Constants.cloneEnergyCost + Constants.startCellEnergy)
                {
                    world.Cells.Add(new CellModel(x, y, world, cell));

                    world.WorldArea.AreaChar[x, y] = Constants.cellChar;
                    world.WorldArea.AreaColor[x, y] = Constants.newCellColor;

                    int temp = Constants.cloneEnergyCost;
                    if (temp > 0)
                    {
                        cell.Energy -= temp;
                    }
                }

                newCellCoord.RemoveAt(randomIndex);
            }
        }

        public void CellStartReproduction()
        {
            lock (lockObject)
            {
                for (int i = 0; i < world.Cells.Count; i++)
                {
                    var cell = world.Cells[i];

                    if (cell.IsCreatingChildren)
                    {
                        ReproduceCell(cell);
                        cell.Energy = Constants.startCellEnergy;
                    }
                }

            }
        }
        private void ReproduceCell(CellModel cell)
        {
            List<(int, int)> otherCellCoord = world.WorldArea.FindAllCellCharNearCellCoord(cell.PositionX, cell.PositionY);

            if (TryFindPartner(out int otherPartnerX, out int otherPartnerY, otherCellCoord))
            {
                List<(int, int)> newCellCoord = GetCombinedEmptyCoords(cell.PositionX, cell.PositionY, otherPartnerX, otherPartnerY);

                CellModel mother;
                CellModel father;

                if (random.Next(0, 2) == 0)
                {
                    father = cell;
                    mother = world.GetCell(otherPartnerX, otherPartnerY);
                }
                else
                {
                    mother = cell;
                    father = world.GetCell(otherPartnerX, otherPartnerY);
                }

                for (int j = mother.AlreadyUseClone; j < Constants.maxClone && newCellCoord.Count > 0; j++)
                {
                    int randomIndex = GetRandomIndex(newCellCoord.Count);
                    (int x, int y) = newCellCoord[randomIndex];

                    if (world.WorldArea.AreaChar[x, y] == Constants.emptyChar &&
                        mother.Energy >= Constants.cloneEnergyCost + Constants.startCellEnergy &&
                        father.Energy >= Constants.cloneEnergyCost + Constants.startCellEnergy)
                    {
                        world.Cells.Add(new CellModel(x, y, world, mother, father));

                        mother.AlreadyUseClone++;

                        world.WorldArea.AreaChar[x, y] = Constants.cellChar;
                        world.WorldArea.AreaColor[x, y] = Constants.newCellColor;

                        mother.Energy -= Constants.cloneEnergyCost;
                        father.Energy -= Constants.cloneEnergyCost;
                    }

                    newCellCoord.RemoveAt(randomIndex);
                }
            }
        }
        private bool TryFindPartner(out int partnerX, out int partnerY, List<(int, int)> otherCellCoord)
        {
            partnerX = 0;
            partnerY = 0;

            while (otherCellCoord.Count > 0)
            {
                int randomIndex = GetRandomIndex(otherCellCoord.Count);
                CellModel temp = world.GetCell(otherCellCoord[randomIndex].Item1, otherCellCoord[randomIndex].Item2);

                if (temp != null && temp.IsCreatingChildren && Constants.maxClone != temp.AlreadyUseClone)
                {
                    partnerX = otherCellCoord[randomIndex].Item1;
                    partnerY = otherCellCoord[randomIndex].Item2;
                    return true;
                }

                otherCellCoord.RemoveAt(randomIndex);
            }

            return false;
        }
        private List<(int, int)> GetCombinedEmptyCoords(int x1, int y1, int x2, int y2)
        {
            List<(int, int)> newCellCoord = world.WorldArea.FindAllEmptyCharNearCellCoord(x1, y1);
            newCellCoord.AddRange(world.WorldArea.FindAllEmptyCharNearCellCoord(x2, y2));
            return newCellCoord;
        }
        private int GetRandomIndex(int count)
        {
            return random.Next(0, count);
        }

        public int CellGenomeSimilarity(CellModel cellA, CellModel cellB)
        {
            double simK = 0;

            if (cellA == null || cellB == null)
            {
                return 0;
            }

            for (int i = 0; i < cellA.GetGenomeCycle().Length; i++)
            {
                if (cellA.GetGenomeCycle()[i] == cellB.GetGenomeCycle()[i])
                {
                    simK++;
                }
            }

            double temp = simK * 100.0 / cellA.GetGenomeCycle().Length;

            return (int)temp;
        }

        public bool IsVictimExists(int positionX, int positionY)
        {
            lock (lockObject)
            {
                return world.Cells.Any(cell => cell.PositionX == positionX && cell.PositionY == positionY && !cell.IsHide);
            }
        }
        public bool IsMoveAvailable(int positionX, int positionY)
        {
            lock (lockObject)
            {
                if (positionX > 0 && positionY > 0 && positionX < Constants.areaSizeX && positionY < Constants.areaSizeY &&
               (world.WorldArea.AreaChar[positionX, positionY] == Constants.emptyChar ||
                world.WorldArea.AreaChar[positionX, positionY] == Constants.poisonChar))
                {
                    return true;
                }
                return false;
            }
        }
    }
}
