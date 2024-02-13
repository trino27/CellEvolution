namespace CellEvolution.WorldResources.Meteor
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
