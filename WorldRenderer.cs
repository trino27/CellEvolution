using CellEvolution.Cell.NN;
using System;
using System.Collections.Generic;

namespace CellEvolution
{
    public class WorldRenderer
    {
        private object lockObject = new object();
        private World world;
        public WorldRenderer(World world)
        {
            this.world = world;
        }

        public void CreateWorldVisual()
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
                            if (Constants.borderChar == world.AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.borderColor;
                            }
                            else if (Constants.cellChar == world.AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = world.GetCell(x / 2, y).CellColor;
                            }
                            else if (Constants.emptyChar == world.AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.emptyColor;
                            }
                            else if (Constants.poisonChar == world.AreaChar[x / 2, y])
                            {
                                Console.ForegroundColor = Constants.poisonColor;
                            }
                            Console.Write(world.AreaChar[x / 2, y]);
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

        public void VisualChange(int x, int y, char ch, ConsoleColor fore)
        {
            lock (lockObject)
            {
                Console.CursorVisible = false;
                Console.SetCursorPosition(x * 2, y);
                Console.ForegroundColor = fore;
                Console.Write(ch);
                Console.ResetColor();
            }
        }
    }
}
