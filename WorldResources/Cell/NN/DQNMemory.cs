using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.WorldResources.Cell.NN
{
    public struct DQNMemory
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
    }
}
