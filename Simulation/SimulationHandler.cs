
using System.Diagnostics;

using CellEvolution.WorldResources;
using СellEvolution;

namespace CellEvolution.Simulation
{
    public class SimulationHandler
    {
        private WorldModel world = new WorldModel();
        private StatDatabase statDatabase = new StatDatabase();

        public int PhotoCells = 0;
        public int BiteCells = 0;
        public int AbsorbCells = 0;
        public int MineCells = 0;
        public int ErrorCells = 0;
        public int SlipCells = 0;

        public double DayErrorValue = 0;
        public double CurrentErrorProc = 0;

        public void StartSimulation()
        {
            Stopwatch stopwatchCells = new Stopwatch();

            world.InitWorldRenderer(new Renderer(world));
            world.StartRenderIfRendererExists();
            world.CreateVisualIfRendererExists();

            do
            {
                UpdateCellsTypeInfo();
                UpdateDataBaseDayErrorValue();

                ShowWorldInfo();
                ShowTimeInfo(stopwatchCells);
                ShowCellsNumInfo();
                ShowCellsTypeInfo();
                
                stopwatchCells.Restart();
                world.MakeTurn();
                stopwatchCells.Stop();

            } while(world.Cells.Count > 0);
        }

        private void ShowWorldInfo()
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

            Console.Write($"Turn: {world.CurrentTurn}  Years: {world.CurrentYear} HoursInYear: {world.CurrentTurn % Constants.numOfTurnInYear}/{Constants.numOfTurnInYear} DayTime: {world.CurrentDayTime} Hours: {world.CurrentHours}/{world.NumOfTurnInDay}/{world.NumOfTurnInDay + world.NumOfTurnInNight}    PhotosintMax: {Energy} Photosint4Around: {Energy4Cell} Photosint8Around: {Energy8Cell}  Photosint1Around: {Energy1Cell} Photosint0Around: {Energy0Cell}  LoseEnergyAtNight:{EnergyNight}                        ");
            Console.SetCursorPosition(20, Constants.areaSizeY + 1);
        }
        private void ShowCellsNumInfo()
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Constants.areaSizeY + 1);

            int Energy = 0;
            foreach (var cell in world.Cells)
            {
                Energy += cell.Energy;
            }
            if (world.Cells.Count != 0)
            {
                Console.Write($"Cells:{world.Cells.Count} Mid Energy {Energy / world.Cells.Count}          ");
            }
        }
        private void ShowCellsTypeInfo()
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(94, Constants.areaSizeY + 1);
            Console.Write($"Error %: {CurrentErrorProc} Photo: {PhotoCells} Hunters: {BiteCells} Absorb: {AbsorbCells} Mine: {MineCells} Slip: {SlipCells}            ");
        }
        private void ShowTimeInfo(Stopwatch stopwatchCells)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Constants.areaSizeY + 2);
            Console.Write("Total turn time: " + stopwatchCells.ElapsedMilliseconds / 1000.0 + " Mid time for each cell: " + stopwatchCells.ElapsedMilliseconds / 1000.0 / world.Cells.Count);
        }

        private void UpdateCellsTypeInfo()
        {
            PhotoCells = 0;
            BiteCells = 0;
            AbsorbCells = 0;
            MineCells = 0;
            ErrorCells = 0;
            SlipCells = 0;

            foreach (var j in world.Cells)
            {
                switch (j.CellColor)
                {
                    case Constants.photoCellColor: PhotoCells++; break;
                    case Constants.biteCellColor: BiteCells++; break;
                    case Constants.absorbCellColor: AbsorbCells++; break;
                    case Constants.mineCellColor: MineCells++; break;
                    case Constants.slipCellColor: SlipCells++; break;
                    case Constants.errorCellColor: ErrorCells++; break;
                    default: break;
                }
            }
            if (world.Cells.Count != 0)
            {
                CurrentErrorProc = (double)ErrorCells * 100 / (double)world.Cells.Count;
            }
            
        }
        
        private void UpdateDataBaseDayErrorValue()
        {
            if(world.CurrentTurn % (16) == 0 && world.CurrentTurn != 0)
            {
                statDatabase.UpdateStat(world.TotalDays, DayErrorValue);
                DayErrorValue = 0;
            }
            DayErrorValue += CurrentErrorProc;
        }
    }
}
