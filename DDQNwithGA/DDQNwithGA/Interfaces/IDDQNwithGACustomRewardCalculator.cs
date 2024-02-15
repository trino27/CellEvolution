using EvolutionNetwork.GenAlg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.Interfaces
{
    public interface IDDQNwithGACustomRewardCalculator
    {
        public double CalculateReward(bool done, double episodeSuccessValue, double targetValue, double bonus);
    }
}
