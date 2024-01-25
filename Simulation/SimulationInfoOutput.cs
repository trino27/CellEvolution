using CellEvolution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using СellEvolution.WorldResources;

namespace СellEvolution.Simulation
{
    public class SimulationInfoOutput
    {

        //private void ShowWorldInfo(int hours)
        //{
        //    Console.CursorVisible = false;
        //    Console.SetCursorPosition(0, Constants.areaSizeY);
        //    double proc = (double)worldArea.Cells.Count * 100 / (double)((Constants.areaSizeX - 2) * (Constants.areaSizeY - 2));
        //    int Energy = (int)(Constants.minPhotosynthesis + Constants.maxPhotosynthesis / 100.0 * (100 - proc));

        //    int Energy8Cell = (int)(Energy / ((8 - Constants.availableCellNumAroundMax) * (8 + 1 - Constants.availableCellNumAroundMax)));
        //    int Energy4Cell = (int)(Energy / ((4 - Constants.availableCellNumAroundMax) * (4 + 1 - Constants.availableCellNumAroundMax)));
        //    int Energy1Cell = (int)(Energy / ((1 - Constants.availableCellNumAroundMin) * (1 - 1 - Constants.availableCellNumAroundMin)));
        //    int Energy0Cell = (int)(Energy / ((0 - Constants.availableCellNumAroundMax) * (0 - 1 - Constants.availableCellNumAroundMin)));

        //    int EnergyNight = (int)(Constants.minNightPhotosynthesisFine + Constants.maxNightPhotosynthesisFine / 100.0 * proc);

        //    Console.Write($"Turn: {CurrentTurn}  Years: {CurrentYear} Days: {CurrentDay}/{Constants.numOfDaysInYear} DayTime: {CurrentDayTime} Hours: {hours + 1}/{Constants.numOfTurnsInDayTime}/{Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime}    PhotosintMax: {Energy} Photosint4Around: {Energy4Cell} Photosint8Around: {Energy8Cell}  Photosint1Around: {Energy1Cell} Photosint0Around: {Energy0Cell}  LoseEnergyAtNight:{EnergyNight}                        ");
        //    Console.SetCursorPosition(20, Constants.areaSizeY + 1);
        //}
        //private void ShowCellsNumInfo()
        //{
        //    Console.CursorVisible = false;
        //    Console.SetCursorPosition(0, Constants.areaSizeY + 1);

        //    int Energy = 0;
        //    foreach (var cell in worldArea.Cells)
        //    {
        //        Energy += cell.Energy;
        //    }
        //    Console.Write($"Cells:{worldArea.Cells.Count} Mid Energy {Energy / worldArea.Cells.Count}          ");
        //}
        //private void ShowCellsTypeInfo()
        //{
        //    int PhotoCells = 0;
        //    int BiteCells = 0;
        //    int AbsorbCells = 0;
        //    int EvolveCells = 0;
        //    int ErrorCells = 0;
        //    int SlipCells = 0;
        //    foreach (var j in worldArea.Cells)
        //    {
        //        switch (j.CellColor)
        //        {
        //            case Constants.photoCellColor: PhotoCells++; break;
        //            case Constants.biteCellColor: BiteCells++; break;
        //            case Constants.absorbCellColor: AbsorbCells++; break;
        //            case Constants.evolvingCellColor: EvolveCells++; break;
        //            case Constants.slipCellColor: SlipCells++; break;
        //            case Constants.errorCellColor: ErrorCells++; break;
        //            default: break;
        //        }
        //    }

        //    CurrentErrorProc = (double)ErrorCells * 100 / (double)worldArea.Cells.Count;

        //    Console.CursorVisible = false;
        //    Console.SetCursorPosition(94, Constants.areaSizeY + 1);
        //    Console.Write($"Error %: {CurrentErrorProc} Plants: {PhotoCells} Hunters: {BiteCells} Mushrooms: {AbsorbCells} Students: {EvolveCells} Slip: {SlipCells}            ");
        //}
        //private void ShowTimeInfo(Stopwatch stopwatchAll, Stopwatch stopwatchCells)
        //{
        //    Console.CursorVisible = false;
        //    Console.SetCursorPosition(0, Constants.areaSizeY + 2);
        //    Console.Write("Total turn time: " + stopwatchAll.ElapsedMilliseconds / 1000.0 + " Mid time for each cell: " + stopwatchCells.ElapsedMilliseconds / 1000.0 / worldArea.Cells.Count + " Time for load: " + ((stopwatchAll.ElapsedMilliseconds / 1000.0) - (stopwatchCells.ElapsedMilliseconds / 1000.0)));
        //}
    }
}
