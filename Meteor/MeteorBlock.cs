using CellEvolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace СellEvolution.Meteor
{
    public class MeteorBlock
    {
        private Random random = new Random();

        public int PositionX;
        public int PositionY;

        public int OrbNum = 0;

        public MeteorBlock(int PositionX, int PositionY)
        {
            this.PositionX = PositionX;
            this.PositionY = PositionY;

            OrbNum = random.Next(Constants.meteorBlockOrbNumMin, Constants.meteorBlockOrbNumMax);
        }
    }
}
