using EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.Interfaces
{
    public interface IDDQNwithGACustomRemindExperiencesDefinder
    {
        public List<DQNMemory> CustomRemindExperiencesDefinder(List<DQNMemory> memory, double[] currentState);
        public double CustomCalculateSimilarityState(double[] currentState, double[] previousState);
    }
}
