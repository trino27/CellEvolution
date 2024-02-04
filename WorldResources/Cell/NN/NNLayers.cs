using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.WorldResources.Cell.NN
{
    public struct NNLayers
    {
        public int size { get; set; }
        public int nextSize { get; set; }
        public double[] neurons { get; set; }
        public double[] biases { get; set; }
        public double[,] weights { get; set; }
        public double[] errors { get; set; } 

        public NNLayers(int size, int nextSize)
        {
            this.size = size;
            this.nextSize = nextSize;
            neurons = new double[size];
            biases = new double[size]; 
            weights = new double[size, nextSize];
            errors = new double[size];
        }
    }
}
