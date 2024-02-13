namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public const double hyperparameterChromosomeMutationProbabilityStart = 0.9;
        public const double hyperparameterChromosomeMutationDuringLiveProbabilityStart = 0.06;

        public const double errorCostStart = 14;
        public const double learningRateStart = 0.0005;
        public const double noiseIntensityStart = 0.01;

        public const double cloneNoiseProbabilityStart = 0.2;
        public const double cloneNoiseWeightsRateStart = 1;

        public const double discountFactorStart = 0.9;
        public const double epsilonStart = 0.1;
        public const double momentumCoefficientStart = 0.9;
        public const double lambdaL2Start = 0.001;
        public const double betaStart = 0.01;
        public const double dropoutRateStart = 0.2;

        public const double genDoneBonusStartA = 2;
        public const double genDoneBonusStartB = 2;

        public const double genHyperparametrChangePower = 0.05; //0.05;

        private Random random = new Random();
        public Dictionary<GenHyperparameter, double> HyperparameterChromosome = new Dictionary<GenHyperparameter, double>();

        public HyperparameterGen()
        {
            HyperparametersInit();
        }
        public HyperparameterGen(HyperparameterGen original)
        {
            HyperparametersCopy(original);
            RandomMutation();
        }
        public HyperparameterGen(HyperparameterGen mother, HyperparameterGen father)
        {
            ConnectGens(mother, father);
            RandomMutation();
        }

        public void HyperparametersCopy(HyperparameterGen original)
        {
            foreach (KeyValuePair<GenHyperparameter, double> gen in original.HyperparameterChromosome)
            {
                HyperparameterChromosome.Add(gen.Key, gen.Value);
            }
        }
        private void HyperparametersInit()
        {
            HyperparameterChromosome = new Dictionary<GenHyperparameter, double>
            {
            { GenHyperparameter.hyperparameterChromosomeMutationProbability, hyperparameterChromosomeMutationProbabilityStart },
            { GenHyperparameter.hyperparameterChromosomeMutationDuringLiveProbability, hyperparameterChromosomeMutationDuringLiveProbabilityStart },

            { GenHyperparameter.errorCost, errorCostStart},
            { GenHyperparameter.genDoneBonusA, genDoneBonusStartA },
            { GenHyperparameter.genDoneBonusB, genDoneBonusStartB },

            { GenHyperparameter.genHyperparameterChangePower, genHyperparametrChangePower },
            { GenHyperparameter.learningRate, learningRateStart },

            { GenHyperparameter.noiseIntensity, noiseIntensityStart },
            { GenHyperparameter.cloneNoiseProbability, cloneNoiseProbabilityStart },
            { GenHyperparameter.cloneNoiseWeightsRate, cloneNoiseWeightsRateStart },

            { GenHyperparameter.discountFactor, discountFactorStart },
            { GenHyperparameter.epsilon, epsilonStart },
            { GenHyperparameter.momentumCoefficient, momentumCoefficientStart },
            { GenHyperparameter.lambdaL2, lambdaL2Start },
            { GenHyperparameter.beta, betaStart },
            { GenHyperparameter.dropoutRate, dropoutRateStart }
            };


        }

        private void ConnectGens(HyperparameterGen mother, HyperparameterGen father)
        {
            HyperparametersCopy(father);
            do
            {
                for (int i = 0; i < HyperparameterChromosome.Count; i++)
                {
                    if (random.Next(0, 2) == 0)
                    {
                        HyperparameterChromosome[(GenHyperparameter)i] = mother.HyperparameterChromosome[(GenHyperparameter)i];
                    }
                }

            } while (random.Next(0, 2) == 0);
        }
        private void RandomMutation()
        {
            if (random.NextDouble() < HyperparameterChromosome[GenHyperparameter.hyperparameterChromosomeMutationProbability])
            {
                int hyperInd = random.Next(0, HyperparameterChromosome.Count);
                double value = 0;
                if (random.Next(0, 2) == 0)
                {
                    value +=
                        HyperparameterChromosome[(GenHyperparameter)hyperInd] *
                        HyperparameterChromosome[GenHyperparameter.genHyperparameterChangePower];
                }
                else
                {
                    value -=
                        HyperparameterChromosome[(GenHyperparameter)hyperInd] *
                        HyperparameterChromosome[GenHyperparameter.genHyperparameterChangePower];
                }


                if (HyperparameterChromosome[(GenHyperparameter)hyperInd] + value > 0)
                {
                    HyperparameterChromosome[(GenHyperparameter)hyperInd] += value;
                }
            }
        }
        private void RandomMutationDuringLive()
        {
            if (random.NextDouble() < HyperparameterChromosome[GenHyperparameter.hyperparameterChromosomeMutationDuringLiveProbability])
            {
                int hyperInd = random.Next(0, HyperparameterChromosome.Count);
                double value = 0;
                if (random.Next(0, 2) == 0)
                {
                    value +=
                        HyperparameterChromosome[(GenHyperparameter)hyperInd] *
                        HyperparameterChromosome[GenHyperparameter.genHyperparameterChangePower];
                }
                else
                {
                    value -=
                        HyperparameterChromosome[(GenHyperparameter)hyperInd] *
                        HyperparameterChromosome[GenHyperparameter.genHyperparameterChangePower];
                }


                if (HyperparameterChromosome[(GenHyperparameter)hyperInd] + value > 0)
                {
                    HyperparameterChromosome[(GenHyperparameter)hyperInd] += value;
                }
            }
        }
        
    }
}
