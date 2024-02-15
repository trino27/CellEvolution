namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public enum GenHyperparameter : byte
        {
            hyperparameterChromosomeMutationProbability,

            elitism,

            errorFine,
            correctBonus,
            genDoneBonusA,
            genDoneBonusB,

            genHyperparameterChangePower,
            learningRate,

            noiseIntensity,

            discountFactor,
            epsilon,
            momentumCoefficient,
            lambdaL2,
            beta,
            dropoutRate
        }
    }
}
