using System;

namespace CellEvolution
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            Logic logic;
            do
            {
                logic = new Logic();
                logic.StartSimulation();
            } while (logic.TotallDays <= 300);
            
        }
    }
}
