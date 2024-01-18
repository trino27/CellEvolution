﻿using System.Collections.Generic;
using System.Diagnostics;
using СellEvolution;

namespace CellEvolution
{
    public partial class Logic
    {
        Random random = new Random();

        private World world;
        private Stat statSQl;

        public int CurrentYear = 0;
        public int CurrentDay = 0;
        public int CurrentHours = 0;
        public double CurrentErrorProc = 0;

        public int TotallDays = 0;
        public double TotallDayError = 0;
        

        public DayTime CurrentDayTime = DayTime.Day;

        public long CurrentTurn = 0;

        public Logic()
        {
            world = new World(this);
            statSQl = new Stat();
        }

        public void StartSimulation()
        {
            world.CreateVisual();
            
            Stopwatch stopwatchAll = new Stopwatch();
            Stopwatch stopwatchCells = new Stopwatch();
            do
            {
                stopwatchAll.Restart();

                ShowWorldInfo(CurrentHours);
                ShowCellsTypeInfo();
                ShowCellsNumInfo();
                UpdateStat();

                stopwatchCells.Restart();

                SortByInitiation();

                List<Task> tasks = new List<Task>();

                for (int i = 0; i < world.Cells.Count; i++)
                {
                    int index = i;
                    Task task = Task.Run(() =>
                    {
                        world.CellMove(index);

                    });

                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());

                stopwatchCells.Stop();

                CurrentTurn++;
                CurrentHours++;

                world.ClearDeadCells();

                world.StartReproduction();
                world.StartCreatingClones();

                world.ClearAreaVoiceParallel();

                UpdateTimeAndSeason();

                stopwatchAll.Stop();
                ShowTimeInfo(stopwatchAll, stopwatchCells);

            } while (world.Cells.Count > 0);
        }
        private void UpdateStat()
        {
            TotallDayError += CurrentErrorProc;
            if (CurrentHours == 0)
            {
                statSQl.InsertData(TotallDays, TotallDayError);
                TotallDayError = 0;
            }
        }
        private void UpdateTimeAndSeason()
        {
            Console.CursorVisible = false;
            if (CurrentHours >= Constants.numOfTurnsInDayTime)
            {
                CurrentDayTime = DayTime.Night;
                if (CurrentHours >= Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime)
                {
                    CurrentDayTime = DayTime.Day;
                    CurrentHours = 0;
                    CurrentDay++;
                    TotallDays++;
                }
                if(CurrentDay >= Constants.numOfDaysInYear)
                {
                    CurrentYear++;
                    CurrentDay=0;
                }
            }
        }
        

        private void ShowWorldInfo(int hours)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Constants.areaSizeY);
            double proc = (double)world.Cells.Count * 100 / (double)((Constants.areaSizeX - 2) * (Constants.areaSizeY - 2));
            int Energy = (int)(Constants.minPhotosynthesis + Constants.maxPhotosynthesis / 100.0 * (100 - proc));

            int Energy8Cell = (int)(Energy / ((8 - Constants.availableCellNumAroundMax) * (8 + 1 - Constants.availableCellNumAroundMax)));
            int Energy4Cell = (int)(Energy / ((4 - Constants.availableCellNumAroundMax) * (4 + 1 - Constants.availableCellNumAroundMax)));
            int Energy1Cell = (int)(Energy / ((1 - Constants.availableCellNumAroundMin) * (1 - 1 - Constants.availableCellNumAroundMin)));
            int Energy0Cell = (int)(Energy / ((0 - Constants.availableCellNumAroundMax) * (0 - 1 - Constants.availableCellNumAroundMin)));

            int EnergyNight = (int)(Constants.minNightPhotosynthesisFine + Constants.maxNightPhotosynthesisFine / 100.0 * proc);

            Console.Write($"Turn: {CurrentTurn}  Years: {CurrentYear} Days: {CurrentDay}/{Constants.numOfDaysInYear} DayTime: {CurrentDayTime} Hours: {hours+1}/{Constants.numOfTurnsInDayTime}/{Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime}    PhotosintMax: {Energy} Photosint4Around: {Energy4Cell} Photosint8Around: {Energy8Cell}  Photosint1Around: {Energy1Cell} Photosint0Around: {Energy0Cell}  LoseEnergyAtNight:{EnergyNight}                        ");
            Console.SetCursorPosition(20, Constants.areaSizeY + 1);
        }
        private void ShowCellsNumInfo()
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Constants.areaSizeY + 1);

            int Energy = 0;
            foreach(var cell in world.Cells)
            {
                Energy += cell.Energy;
            }
            Console.Write($"Cells:{world.Cells.Count} Mid Energy {Energy/world.Cells.Count}          ");
        }
        private void ShowCellsTypeInfo()
        {
            int PhotoCells = 0;
            int BiteCells = 0;
            int AbsorbCells = 0;
            int EvolveCells = 0;
            int ErrorCells = 0;
            int SlipCells = 0;
            foreach (var j in world.Cells)
            {
                switch (j.CellColor)
                {
                    case Constants.photoCellColor: PhotoCells++; break;
                    case Constants.biteCellColor: BiteCells++; break;
                    case Constants.absorbCellColor: AbsorbCells++; break;
                    case Constants.evolvingCellColor: EvolveCells++; break;
                    case Constants.slipCellColor: SlipCells++; break;
                    case Constants.errorColor: ErrorCells++; break;
                    default: break;
                }
            }

            CurrentErrorProc = (double)ErrorCells * 100 / (double)world.Cells.Count;

            Console.CursorVisible = false;
            Console.SetCursorPosition(94, Constants.areaSizeY + 1);
            Console.Write($"Error %: {CurrentErrorProc} Plants: {PhotoCells} Hunters: {BiteCells} Mushrooms: {AbsorbCells} Students: {EvolveCells} Slip: {SlipCells}            ");
        }
        private void ShowTimeInfo(Stopwatch stopwatchAll, Stopwatch stopwatchCells)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Constants.areaSizeY + 2);
            Console.Write("Total turn time: " + stopwatchAll.ElapsedMilliseconds / 1000.0 + " Mid time for each cell: " + stopwatchCells.ElapsedMilliseconds / 1000.0 / world.Cells.Count + " Time for load: " + ((stopwatchAll.ElapsedMilliseconds / 1000.0) - (stopwatchCells.ElapsedMilliseconds / 1000.0)));
        }

        private void SortByInitiation()
        {
            Shuffle(world.Cells);
            world.Cells = world.Cells.OrderBy(c => c.Initiation).ToList();
        }
        private void Shuffle<T>(List<T> array)
        {
            Random random = new Random();
            int size = array.Count;

            while (size > 1)
            {
                size--;
                int k = random.Next(size + 1);
                T temp = array[k];
                array[k] = array[size];
                array[size] = temp;
            }
        }

        
    }
}