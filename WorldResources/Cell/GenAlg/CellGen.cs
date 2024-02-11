namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
    {
        private Random random = new Random();
        public GenAction[] ActionsChromosome { get; }
        public Dictionary<GenHyperparameter, double> HyperparameterChromosome = new Dictionary<GenHyperparameter, double>();
        private int CurrentGenIndex = 0;
        int turns = 0;

        public CellGen()
        {
            ActionsChromosome = new GenAction[Constants.genCycleSize];
            FillRandomGens();
            HyperparametersInit();
        }
        public CellGen(CellGen original)
        {
            turns = original.turns;

            ActionsChromosome = new GenAction[Constants.genCycleSize];

            Array.Copy(original.ActionsChromosome, ActionsChromosome, Constants.genCycleSize);

            HyperparametersCopy(original);
            RandomMutation();
        }
        public CellGen(CellGen mother, CellGen father)
        {
            turns = mother.turns;

            ActionsChromosome = new GenAction[Constants.genCycleSize];
            ConnectGens(mother, father);
            RandomMutation();
        }

        public void HyperparametersInit()
        {
            HyperparameterChromosome = new Dictionary<GenHyperparameter, double>
            {
            { GenHyperparameter.actionChromosomeMutationProbability,  Constants.actionChromosomeMutationProbabilityStart},
            { GenHyperparameter.actionChromosomeMutationDuringLiveProbability, Constants.actionChromosomeMutationDuringLiveProbabilityStart },

            { GenHyperparameter.hyperparameterChromosomeMutationProbability, Constants.hyperparameterChromosomeMutationProbabilityStart },
            { GenHyperparameter.hyperparameterChromosomeMutationDuringLiveProbability, Constants.hyperparameterChromosomeMutationDuringLiveProbabilityStart },

            { GenHyperparameter.errorCost, Constants.errorCostStart},
            { GenHyperparameter.genDoneBonusA, Constants.genDoneBonusStartA },
            { GenHyperparameter.genDoneBonusB, Constants.genDoneBonusStartB },

            { GenHyperparameter.genHyperparameterChangePower, Constants.genHyperparametrChangePower },
            { GenHyperparameter.learningRate, Constants.learningRateStart },

            { GenHyperparameter.noiseIntensity, Constants.noiseIntensityStart },
            { GenHyperparameter.cloneNoiseProbability, Constants.cloneNoiseProbabilityStart },
            { GenHyperparameter.cloneNoiseWeightsRate, Constants.cloneNoiseWeightsRateStart },

            { GenHyperparameter.discountFactor, Constants.discountFactorStart },
            { GenHyperparameter.epsilon, Constants.epsilonStart },
            { GenHyperparameter.momentumCoefficient, Constants.momentumCoefficientStart },
            { GenHyperparameter.lambdaL2, Constants.lambdaL2Start },
            { GenHyperparameter.beta, Constants.betaStart },
            { GenHyperparameter.dropoutRate, Constants.dropoutRateStart }
            };


        }
        public void HyperparametersCopy(CellGen original)
        {
            foreach (KeyValuePair<GenHyperparameter, double> gen in original.HyperparameterChromosome)
            {
                HyperparameterChromosome.Add(gen.Key, gen.Value);
            }
        }
        public GenAction GetCurrentGenAction()
        {
            GenAction genAction = ActionsChromosome[CurrentGenIndex];

            NextGenIndex();
            RandomMutationDuringLive();

            genAction = GenAction.All;
            return genAction;
        }

        public double[] FutureGenActions(int n)
        {
            double[] result = new double[n];

            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (CurrentGenIndex + i < ActionsChromosome.Length)
                {
                    result[i] = (double)ActionsChromosome[CurrentGenIndex + i];
                }
                else
                {
                    result[i] = (double)ActionsChromosome[j];
                    j++;

                    if (j > ActionsChromosome.Length)
                    {
                        j = 0;
                    }
                }
            }
            return result;
        }

        private void NextGenIndex()
        {
            CurrentGenIndex++;
            if (CurrentGenIndex >= Constants.genCycleSize)
            {
                CurrentGenIndex = 0;
            }
        }
        private void ConnectGens(CellGen mother, CellGen father)
        {
            Array.Copy(father.ActionsChromosome, ActionsChromosome, Constants.genCycleSize);
            HyperparametersCopy(father);
            do
            {
                int startGenAction = random.Next(0, ActionsChromosome.Length);
                int endGenAction = random.Next(startGenAction, ActionsChromosome.Length);

                for (int i = startGenAction; i < endGenAction + 1; i++)
                {
                    ActionsChromosome[i] = mother.ActionsChromosome[i];
                }

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
            if (random.NextDouble() < HyperparameterChromosome[GenHyperparameter.actionChromosomeMutationProbability])
            {
                ActionsChromosome[random.Next(0, ActionsChromosome.Length)] = (GenAction)random.Next(0, 8);
            }


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

            if (random.NextDouble() < HyperparameterChromosome[GenHyperparameter.actionChromosomeMutationDuringLiveProbability])
            {
                ActionsChromosome[random.Next(0, ActionsChromosome.Length)] = (GenAction)random.Next(0, 8);
            }

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
        private void FillRandomGens()
        {
            for (int i = 0; i < ActionsChromosome.Length; i++)
            {
                ActionsChromosome[i] = (GenAction)random.Next(0, 8);
            }
        }
    }
}
