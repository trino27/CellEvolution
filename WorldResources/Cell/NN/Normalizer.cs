﻿using CellEvolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.WorldResources.NN
{
    internal class Normalizer
    {
        public static double NormalizeReward(double reward)
        {
            return Math.Tanh(reward);
        }

        public static double CharNormalize(double value)
        {
            return MinMaxNormalize(value, 0, 12);
        }

        public static double CharDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, 0, 12);
        }
        public static double GenNormalize(double value)
        {
            return MinMaxNormalize(value, 0, 101);
        }

        public static double GenDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, 0, 101);
        }
        public static double EnergyNormalize(double value)
        {
            return MinMaxNormalize(value, -(Constants.maxNightPhotosynthesisFine+Constants.minNightPhotosynthesisFine), Constants.maxClone * Constants.cloneEnergyCost + Constants.startCellEnergy);
        }

        public static double EnergyDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, -(Constants.maxNightPhotosynthesisFine + Constants.minNightPhotosynthesisFine), Constants.maxClone * Constants.cloneEnergyCost + Constants.startCellEnergy);
        }

        public static double PhotosyntesNormalize(double value)
        {
            return MinMaxNormalize(value, Constants.maxNightPhotosynthesisFine + Constants.minNightPhotosynthesisFine, Constants.minPhotosynthesis + Constants.maxPhotosynthesis);
        }

        public static double PhotosyntesDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, Constants.maxNightPhotosynthesisFine + Constants.minNightPhotosynthesisFine, Constants.minPhotosynthesis + Constants.maxPhotosynthesis);
        }

        public static double ActionNormalize(double value)
        {
            return MinMaxNormalize(value, 0, 30);
        }

        public static double ActionDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, 0, 30);
        }

        public static double FutureGenNormalize(double value)
        {
            return MinMaxNormalize(value, 0, 8);
        }

        public static double FutureGenDenormalize(double normalizedValue)
        {
            return MinMaxDenormalize(normalizedValue, 0, 8);
        }
        private static double MinMaxNormalize(double value, double minValue, double maxValue)
        {
            if (value >= maxValue) return 1;
            if (value <= minValue) return 0;
            else return  (value - minValue) / (maxValue - minValue);
        }

        private static double MinMaxDenormalize(double normalizedValue, double minValue, double maxValue)
        {
            return (normalizedValue * (maxValue - minValue)) + minValue;
        }

    }

}
