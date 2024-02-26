namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public enum GenHyperparameter : byte
        {
            hyperparameterChromosomeMutationProbability,

            errorFine,
            correctBonus,
            genDoneBonusA,
            genDoneBonusB,

            genHyperparameterPercentageChange,
            learningRate,

            noiseIntensity,

            discountFactor,
            exploration,
            momentumCoefficient,
            lambdaL2,
            beta,
            dropoutRate,

            percentageOfSimilarExperiences,
            remindProbability
        }
    }
}
