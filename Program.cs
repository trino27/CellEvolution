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
                Simulation simulation = new Simulation();

                simulation.StartSimulation();
            } while (true);
        }
    }
}
