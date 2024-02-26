using EvolutionNetwork.GenAlg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.StaticClasses
{
    static public class Regularization
    {
        public static double ApplyL2Regularization(double pureGradient, double lambdaL2, double weight)
        {
            return pureGradient - lambdaL2 * weight;
        }

        public static double[] ApplyDropout(double[] activations, int layerIndex, int[] layersSizes, double dropoutRate)
        {
            Random random = new Random();
            // Применяем дропаут только к скрытым слоям
            if (layerIndex > 0 && layerIndex < layersSizes.Length - 1)
            {
                for (int i = 0; i < activations.Length; i++)
                {
                    if (random.NextDouble() < dropoutRate)
                    {
                        activations[i] = 0;
                    }
                }
            }
            return activations;
        }

        public static double GenerateRandomNoise()
        {
            Random random = new Random();
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
