using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.WorldResources.NN
{
    internal class Normalizer
    {
        public static  double LogNormalize(double value)
        {
            return Math.Log(value + 1);
        }

        public static double LogDenormalize(double normalizedValue)
        {
            return Math.Exp(normalizedValue) - 1;
        }
    }

}
