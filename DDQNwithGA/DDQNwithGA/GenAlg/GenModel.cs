using System.Linq;

namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public const double hyperparameterChromosomeMutationProbabilityStart = 0.3;
        public const double genHyperparameterPercentageChangeStart = 25; //0.05;

        public const double errorFineStart = 30;
        public const double correctBonusStart = 2;
        public const double learningRateStart = 0.0005;

        public const double discountFactorStart = 0.9;
        public const double explorationStart = 0.35; //0.25
        public const double momentumCoefficientStart = 0.9;
        public const double lambdaL2Start = 0.001;
        public const double betaStart = 1;

        public const double noiseIntensityStart = 0.01;
        public const double dropoutRateStart = 0.2;

        public const double genDoneBonusStartA = 2;
        public const double genDoneBonusStartB = 2;

        public const double percentageOfSimilarExperiencesStart = 0.02;
        public const double remindProbabilityStart = 0.2;

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

            { GenHyperparameter.genHyperparameterPercentageChange, genHyperparameterPercentageChangeStart },
            { GenHyperparameter.learningRate, learningRateStart },

            { GenHyperparameter.noiseIntensity, noiseIntensityStart },

            { GenHyperparameter.discountFactor, discountFactorStart },
            { GenHyperparameter.exploration, explorationStart },
            { GenHyperparameter.momentumCoefficient, momentumCoefficientStart },
            { GenHyperparameter.lambdaL2, lambdaL2Start },
            { GenHyperparameter.beta, betaStart },
            { GenHyperparameter.dropoutRate, dropoutRateStart },

            { GenHyperparameter.percentageOfSimilarExperiences, percentageOfSimilarExperiencesStart },
            { GenHyperparameter.remindProbability, remindProbabilityStart }
            };
        }
        public void StartHyperparameterScatter(double scatterProc)
        {
            // Убедитесь, что значение scatter не отрицательное
            if (scatterProc < 0) scatterProc = 0;

            for (int i = 0; i < HyperparameterChromosome.Count; i++)
            {
                int hyperInd = i;

                if (!blockedHyperparameterGens.Contains((GenHyperparameter)hyperInd))
                {
                    double currentValue = HyperparameterChromosome[(GenHyperparameter)hyperInd];
                    // Определяем, будет ли изменение положительным или отрицательным
                    double direction = random.NextDouble() < 0.5 ? -1 : 1;
                    // Генерация случайного изменения в пределах заданного процента от текущего значения
                    double scatterValue = random.NextDouble() * scatterProc;
                    double scatterPercentage = direction * scatterValue; // Генерация значения от -scatter до +scatter с равными шансами
                    double change = currentValue * scatterPercentage / 100; // Рассчитываем фактическое изменение как процент от текущего значения
                    double newValue = currentValue + change; // Вычисляем новое значение гиперпараметра

                    // Если новое значение меньше 0, уменьшаем до 0, чтобы избежать отрицательных значений
                    if (newValue < 0)
                    {
                        newValue = 0; // Устанавливаем значение в 0, если результат отрицательный
                    }

                    // Присваиваем новое значение гиперпараметру, если оно больше 0
                    if (newValue > 0)
                    {
                        HyperparameterChromosome[(GenHyperparameter)hyperInd] = newValue;
                    }
                }
            }
        }
        public void StartHyperparameterScatter(Dictionary<GenHyperparameter, double> scatterProcDict)
        {
            for (int i = 0; i < HyperparameterChromosome.Count; i++)
            {
                GenHyperparameter hyperInd = (GenHyperparameter)i;

                // Пропускаем изменение для гиперпараметров, которые заблокированы или отсутствуют в словаре
                if (blockedHyperparameterGens.Contains(hyperInd) || !scatterProcDict.TryGetValue(hyperInd, out double scatterProc))
                {
                    continue; // Пропускаем текущую итерацию, если условие не выполнено
                }

                // Убедитесь, что значение scatter не отрицательное
                scatterProc = Math.Max(0, scatterProc);

                double currentValue = HyperparameterChromosome[hyperInd];
                // Определяем, будет ли изменение положительным или отрицательным
                double direction = random.NextDouble() < 0.5 ? -1 : 1;
                // Генерация случайного изменения в пределах заданного процента от текущего значения
                double scatterValue = random.NextDouble() * scatterProc;
                double scatterPercentage = direction * scatterValue; // Генерация значения от -scatter до +scatter с равными шансами
                double change = currentValue * scatterPercentage / 100; // Рассчитываем фактическое изменение как процент от текущего значения
                double newValue = currentValue + change; // Вычисляем новое значение гиперпараметра

                // Если новое значение меньше 0, уменьшаем до 0, чтобы избежать отрицательных значений
                if (newValue < 0)
                {
                    newValue = 0; // Устанавливаем значение в 0, если результат отрицательный
                }

                // Присваиваем новое значение гиперпараметру, если оно больше 0
                HyperparameterChromosome[hyperInd] = newValue;
            }
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
            for (int i = 0; i < HyperparameterChromosome.Count; i++)
            {
                int hyperInd = i;
                if (random.NextDouble() < HyperparameterChromosome[GenHyperparameter.hyperparameterChromosomeMutationProbability])
                {
                    if (!blockedHyperparameterGens.Contains((GenHyperparameter)hyperInd))
                    {
                        double currentValue = HyperparameterChromosome[(GenHyperparameter)hyperInd];
                        // Определяем, будет ли изменение положительным или отрицательным
                        double direction = random.NextDouble() < 0.5 ? -1 : 1;
                        // Генерация случайного изменения в пределах заданного процента от текущего значения
                        double scatterValue = random.NextDouble() * HyperparameterChromosome[GenHyperparameter.genHyperparameterPercentageChange];
                        double scatterPercentage = direction * scatterValue; // Генерация значения от -scatter до +scatter с равными шансами
                        double change = currentValue * scatterPercentage / 100; // Рассчитываем фактическое изменение как процент от текущего значения
                        double newValue = currentValue + change; // Вычисляем новое значение гиперпараметра

                        // Если новое значение меньше 0, уменьшаем до 0, чтобы избежать отрицательных значений
                        if (newValue < 0)
                        {
                            newValue = 0; // Устанавливаем значение в 0, если результат отрицательный
                        }

                        // Присваиваем новое значение гиперпараметру, если оно больше 0
                        if (newValue > 0)
                        {
                            HyperparameterChromosome[(GenHyperparameter)hyperInd] = newValue;
                        }
                    }
                }
            }
        }
    }
}
