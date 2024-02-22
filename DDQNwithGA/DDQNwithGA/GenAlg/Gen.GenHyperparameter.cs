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

            genHyperparameterChangePower,
            learningRate,

            noiseIntensity,

            discountFactor,
            exploration,
            momentumCoefficient,
            lambdaL2,
            beta,
            dropoutRate,
        }
    }
}
