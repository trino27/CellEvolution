using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.Interfaces
{
    public interface IDDQNwithGACritic
    {
        public bool IsDecidedActionError(int decidedAction, double[] LastState);
        
    }
}
