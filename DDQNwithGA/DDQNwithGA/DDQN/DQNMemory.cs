using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN
{
    public class DQNMemory : ICloneable
    {
        public double[] BeforeActionState;
        public int DecidedAction;
        public double Reward;
        public double[] AfterActionState;
        public bool Done;

        public DQNMemory(double[] beforeActionState, int decidedAction, double reward, double[] afterActionState, bool done)
        {
            BeforeActionState = beforeActionState;
            DecidedAction = decidedAction;
            Reward = reward;
            AfterActionState = afterActionState;
            Done = done;
        }

        public object Clone()
        {
            double[] beforeMoveStateCopy = new double[BeforeActionState.Length];
            Array.Copy(BeforeActionState, beforeMoveStateCopy, BeforeActionState.Length);

            double[] afterMoveStateCopy = new double[AfterActionState.Length];
            Array.Copy(AfterActionState, afterMoveStateCopy, AfterActionState.Length);

            return new DQNMemory(beforeMoveStateCopy, DecidedAction, Reward, afterMoveStateCopy, Done);
        }
    }
}
