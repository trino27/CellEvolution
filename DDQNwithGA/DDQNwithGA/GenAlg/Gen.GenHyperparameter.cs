namespace EvolutionNetwork.GenAlg
{
    public partial struct HyperparameterGen
    {
        public enum GenHyperparameter : byte
        {
            hyperparameterChromosomeMutationProbability,
            hyperparameterChromosomeMutationDuringLiveProbability,

            errorCost,
            genDoneBonusA,
            genDoneBonusB,

            genHyperparameterChangePower,
            learningRate,

            noiseIntensity,
            cloneNoiseProbability,
            cloneNoiseWeightsRate,

            discountFactor,
            epsilon,
            momentumCoefficient,
            lambdaL2,
            beta,
            dropoutRate
        }
    }
}
