using CellEvolution.Simulation;

namespace CellEvolution
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);

            do
            {
                SimulationHandler simulation = new SimulationHandler();

                simulation.StartSimulation();
            } while (true);
        }
    }
}
