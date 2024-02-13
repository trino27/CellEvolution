namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
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
