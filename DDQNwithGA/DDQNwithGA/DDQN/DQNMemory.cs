using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolutionNetwork.DDQNwithGA.DDQNwithGA.DDQN
{
    public struct DQNMemory : ICloneable
    {
        public double[] BeforeMoveState;
        public int DecidedAction;
        public double Reward;
        public double[] AfterMoveState;
        public bool Done;

        public DQNMemory(double[] beforeMoveState, int decidedAction, double reward, double[] afterMoveState, bool done)
        {
            BeforeMoveState = beforeMoveState;
            DecidedAction = decidedAction;
            Reward = reward;
            AfterMoveState = afterMoveState;
            Done = done;
        }

        public object Clone()
        {
            double[] beforeMoveStateCopy = new double[BeforeMoveState.Length];
            Array.Copy(BeforeMoveState, beforeMoveStateCopy, BeforeMoveState.Length);

            double[] afterMoveStateCopy = new double[AfterMoveState.Length];
            Array.Copy(AfterMoveState, afterMoveStateCopy, AfterMoveState.Length);

            return new DQNMemory(beforeMoveStateCopy, DecidedAction, Reward, afterMoveStateCopy, Done);
        }
    }
}
