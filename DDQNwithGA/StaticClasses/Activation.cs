using EvolutionNetwork.GenAlg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.StaticClasses
{
    static public class Activation
    {
        public static double DSwishActivation(double x, double beta)
        {
            double sigmoid = 1.0 / (1.0 + Math.Exp(-beta * x));
            return sigmoid + beta * x * sigmoid * (1 - sigmoid);
        }

        public static double SwishActivation(double x, double beta)
        {
            return x / (1.0 + Math.Exp(-beta * x));
        }
    }
}
