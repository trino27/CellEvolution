﻿namespace CellEvolution.WorldResources.Meteor
{
    public class MeteorModel
    {
        private Random random = new Random();

        public int MeteorBlockDist = 0;
        public int PoisonAddDist = 0;

        public int NightTurns = 0;
        public bool IsCreateMeteorBlocks = false;

        public int CenterPositionX;
        public int CenterPositionY;

        public List<MeteorBlock> MeteorBlocks = new List<MeteorBlock>();

        public MeteorModel()
        {
            double randomSize = random.NextDouble();

            if (randomSize < Constants.meteorHugeSizeProbability)
            {
                NightTurns = 5;
                IsCreateMeteorBlocks = true;

                MeteorBlockDist = Constants.meteorHugeSizeDistance;
            }
            else if (randomSize < Constants.meteorBigSizeProbability)
            {
                NightTurns = 3;
                IsCreateMeteorBlocks = true;

                MeteorBlockDist = Constants.meteorBigSizeDistance;
            }
            else if (randomSize < Constants.meteorMidSizeProbability)
            {
                IsCreateMeteorBlocks = true;

                MeteorBlockDist = Constants.meteorMidSizeDistance;
            }
            else if (randomSize < Constants.meteorSmallSizeProbability)
            {
                NightTurns = 0;
                IsCreateMeteorBlocks = true;
            }

            bool IsEnoughtArea = false;
            if (MeteorBlockDist < Constants.areaSizeX - MeteorBlockDist - 1)
            {
                CenterPositionX = random.Next(MeteorBlockDist + 1, Constants.areaSizeX - MeteorBlockDist - 1);
                IsEnoughtArea = true;
            }
            else IsEnoughtArea = false;
            if (IsEnoughtArea && MeteorBlockDist < Constants.areaSizeY - MeteorBlockDist - 1)
            {
                CenterPositionY = random.Next(MeteorBlockDist + 1, Constants.areaSizeY - MeteorBlockDist - 1);
                IsEnoughtArea = true;
            }
            else IsEnoughtArea = false;

            if (IsCreateMeteorBlocks && IsEnoughtArea) CreateMeteorBlocks();
        }

        private void CreateMeteorBlocks()
        {
            for (int x = CenterPositionX - MeteorBlockDist; x <= CenterPositionX + MeteorBlockDist; x++)
            {
                for (int y = CenterPositionY - MeteorBlockDist; y <= CenterPositionY + MeteorBlockDist; y++)
                {
                    MeteorBlocks.Add(new MeteorBlock(x, y));
                }
            }
        }
    }
}
