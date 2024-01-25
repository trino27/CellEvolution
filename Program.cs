using System;
using СellEvolution.Simulation;

namespace CellEvolution
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            
            Simulation simulation = new Simulation();
            do
            {
                simulation.StartSimulation();

            } while (true);
            
        }
    }
}
