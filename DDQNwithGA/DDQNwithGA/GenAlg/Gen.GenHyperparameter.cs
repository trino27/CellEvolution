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
            temperature,
            momentumCoefficient,
            lambdaL2,
            beta,
            dropoutRate
        }
    }
}
