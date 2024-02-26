namespace EvolutionNetwork.StaticClasses
{
    public static class Normalizer
    {
        public static double MinMaxNormalize(double value, double minValue, double maxValue)
        {
            if (value >= maxValue) return 1;
            if (value <= minValue) return 0;
            else return (value - minValue) / (maxValue - minValue);
        }

        public static double MinMaxDenormalize(double normalizedValue, double minValue, double maxValue)
        {
            return normalizedValue * (maxValue - minValue) + minValue;
        }
        public static double TanhNormalize(double value)
        {
            return Math.Tanh(value);
        }

    }

}
