using EvolutionNetwork.GenAlg;
using EvolutionNetwork.StaticClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.DDQN.NN
{
    internal class NNTrainWithSGDM
    {
        internal double[][][] VelocitiesWeights;
        internal double[][] VelocitiesBiases;

        private readonly HyperparameterGen gen;

        public NNTrainWithSGDM(HyperparameterGen gen)
        {
            this.gen = gen;
        }

        public void InitVelocities(NNLayers[] layers)
        {
            VelocitiesWeights = new double[layers.Length - 1][][];
            VelocitiesBiases = new double[layers.Length][];

            for (int i = 0; i < layers.Length; i++)
            {
                if (i < layers.Length - 1)
                {
                    VelocitiesWeights[i] = new double[layers[i].size][];
                    for (int j = 0; j < layers[i].size; j++)
                    {
                        VelocitiesWeights[i][j] = new double[layers[i + 1].size];
                        for (int k = 0; k < layers[i + 1].size; k++)
                        {
                            VelocitiesWeights[i][j][k] = 0;
                        }
                    }
                }

                VelocitiesBiases[i] = new double[layers[i].size];
                for (int j = 0; j < layers[i].size; j++)
                {
                    VelocitiesBiases[i][j] = 0;
                }
            }
        }
        public void BackPropagationSGDM(double[] predicted, double[] targets, NNLayers[] layers)
        {
            double learningRate = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.learningRate];
            double lambdaL2 = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.lambdaL2]; // Коэффициент L2 регуляризации
            double momentum = gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.momentumCoefficient]; // Коэффициент момента

            double[] errors = new double[predicted.Length];
            for (int i = 0; i < predicted.Length; i++)
            {
                errors[i] = targets[i] - predicted[i];
            }

            for (int layerIndex = layers.Length - 2; layerIndex >= 0; layerIndex--)
            {
                NNLayers currentLayer = layers[layerIndex];
                NNLayers nextLayer = layers[layerIndex + 1];
                double[] errorsNext = new double[currentLayer.size];

                for (int i = 0; i < nextLayer.size; i++)
                {
                    double gradient = errors[i] * Activation.DSwishActivation(nextLayer.neurons[i], gen.HyperparameterChromosome[HyperparameterGen.GenHyperparameter.beta]);

                    for (int j = 0; j < currentLayer.size; j++)
                    {
                        // Вычисление "чистого" градиента (без учета L2 регуляризации)
                        double pureGradient = gradient * currentLayer.neurons[j];

                        // Застосування L2 регуляризації до градієнта
                        double weightGradientWithL2 = Regularization.ApplyL2Regularization(pureGradient, lambdaL2, currentLayer.weights[j, i]);

                        // Обновление скорости и веса с учетом L2 регуляризации
                        VelocitiesWeights[layerIndex][j][i] = momentum * VelocitiesWeights[layerIndex][j][i] + learningRate * weightGradientWithL2;
                        currentLayer.weights[j, i] += VelocitiesWeights[layerIndex][j][i];

                    }
                    // Обновление смещения с учетом момента
                    VelocitiesBiases[layerIndex + 1][i] = momentum * VelocitiesBiases[layerIndex + 1][i] + learningRate * gradient;
                    nextLayer.biases[i] += VelocitiesBiases[layerIndex + 1][i];
                }

                for (int i = 0; i < currentLayer.size; i++)
                {
                    errorsNext[i] = 0;
                    for (int j = 0; j < nextLayer.size; j++)
                    {
                        errorsNext[i] += currentLayer.weights[i, j] * errors[j];
                    }
                }
                errors = errorsNext;
            }
        }

    }
}
