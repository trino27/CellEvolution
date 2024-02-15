using System.Linq;

namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public const double hyperparameterChromosomeMutationProbabilityStart = 0.9;
   

        public const double errorFineStart = 14;
        public const double correctBonusStart = 3;
        public const double learningRateStart = 0.0005;
        public const double noiseIntensityStart = 0.01;

        public const double elitismStart = 0.3;

        public const double discountFactorStart = 0.9;
        public const double epsilonStart = 0.25; //0.25
        public const double momentumCoefficientStart = 0.9;
        public const double lambdaL2Start = 0.001;
        public const double betaStart = 0.01;
        public const double dropoutRateStart = 0.25;

        public const double genDoneBonusStartA = 2;
        public const double genDoneBonusStartB = 2;

        public const double genHyperparametrChangePower = 0.08; //0.05;

        private Random random = new Random();
        public Dictionary<GenHyperparameter, double> HyperparameterChromosome = new Dictionary<GenHyperparameter, double>();
        private List<GenHyperparameter> blockedHyperparameterGens = new List<GenHyperparameter>();

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
            blockedHyperparameterGens = original.blockedHyperparameterGens.ToList();
        }
        private void HyperparametersInit()
        {
            HyperparameterChromosome = new Dictionary<GenHyperparameter, double>
            {
            { GenHyperparameter.hyperparameterChromosomeMutationProbability, hyperparameterChromosomeMutationProbabilityStart },

            { GenHyperparameter.errorFine, errorFineStart},
            { GenHyperparameter.correctBonus, correctBonusStart},
            { GenHyperparameter.genDoneBonusA, genDoneBonusStartA },
            { GenHyperparameter.genDoneBonusB, genDoneBonusStartB },

            { GenHyperparameter.genHyperparameterChangePower, genHyperparametrChangePower },
            { GenHyperparameter.learningRate, learningRateStart },
            { GenHyperparameter.elitism, elitismStart },

            { GenHyperparameter.noiseIntensity, noiseIntensityStart },

            { GenHyperparameter.discountFactor, discountFactorStart },
            { GenHyperparameter.epsilon, epsilonStart },
            { GenHyperparameter.momentumCoefficient, momentumCoefficientStart },
            { GenHyperparameter.lambdaL2, lambdaL2Start },
            { GenHyperparameter.beta, betaStart },
            { GenHyperparameter.dropoutRate, dropoutRateStart }
            };
        }

        public void BlockHyperparameterGenChanging(GenHyperparameter hyperparameter)
        {
            if (!blockedHyperparameterGens.Contains(hyperparameter))
            {
                blockedHyperparameterGens.Add(hyperparameter);
            }
        }
        public void UnblockHyperparameterGenChanging(GenHyperparameter hyperparameter)
        {
            if (blockedHyperparameterGens.Contains(hyperparameter))
            {
                blockedHyperparameterGens.Remove(hyperparameter);
            }
        }
        public void SetHyperparameter(GenHyperparameter hyperparameter, double positiveOrZeroValue)
        {
            if (positiveOrZeroValue < 0)
            {
                throw new ArgumentOutOfRangeException("Value should be >= 0");
            }
            HyperparameterChromosome[hyperparameter] = positiveOrZeroValue;
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
                if (!blockedHyperparameterGens.Contains((GenHyperparameter)hyperInd))
                {
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
}
