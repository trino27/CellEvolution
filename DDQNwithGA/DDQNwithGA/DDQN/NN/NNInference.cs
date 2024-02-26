using EvolutionNetwork.GenAlg;
using EvolutionNetwork.StaticClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.DDQN.NN
{
    internal class NNInference
    {
        private readonly HyperparameterGen gen;
        private readonly int[] layersSizes;
        public NNInference(HyperparameterGen gen, int[] layersSizes)
        {
            this.gen = gen;
            this.layersSizes = layersSizes; 
        }

        public double[] FeedForward(double[] input, NNLayers[] layers)
        {
            layers[0].neurons = input; // Инициализация входного слоя

            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i].size; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < layers[i - 1].size; k++)
                    {
                        sum += layers[i - 1].neurons[k] * layers[i - 1].weights[k, j];
                    }
                    layers[i].neurons[j] = Activation.SwishActivation(sum + layers[i].biases[j], gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta]);
                }
            }

            return layers[layers.Length - 1].neurons;
        }
        public double[] FeedForwardWithNoise(double[] input, NNLayers[] layers)
        {
            layers[0].neurons = input; // Инициализация входного слоя

            for (int i = 1; i < layers.Length; i++)
            {
                for (int j = 0; j < layers[i].size; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < layers[i - 1].size; k++)
                    {
                        sum += layers[i - 1].neurons[k] * layers[i - 1].weights[k, j];
                    }
                    double activation = Activation.SwishActivation(sum + layers[i].biases[j], gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta]);
                    // Добавление шума к результату активации
                    layers[i].neurons[j] = activation + Regularization.GenerateRandomNoise() * gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.noiseIntensity];
                }

                layers[i].neurons = Regularization.ApplyDropout(layers[i].neurons, i, layersSizes, gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.dropoutRate]);
            }

            return layers[layers.Length - 1].neurons;
        }

        public int FindMaxIndexForFindAction(double[] array)
        {
            Random random = new Random();

            int maxIndex = random.Next(0, layersSizes[^1]);
            double maxWeight = array[maxIndex];

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > maxWeight)
                {
                    maxWeight = array[i];
                    maxIndex = i;
                }
            }

            return maxIndex;
        }
    }
}
