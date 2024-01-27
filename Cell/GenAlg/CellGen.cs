﻿namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
    {
        private Random random = new Random();
        public GenAction[] GenActionsCycle { get; }
        private int CurrentGenIndex = 0;

        public CellGen()
        {
            GenActionsCycle = new GenAction[Constants.genCycleSize];
            FillRandomGens();
        }
        public CellGen(CellGen original)
        {
            GenActionsCycle = new GenAction[Constants.genCycleSize];
            Array.Copy(original.GenActionsCycle, GenActionsCycle, Constants.genCycleSize);
            RandomMutation();
        }
        public CellGen(CellGen mother, CellGen father)
        {
            GenActionsCycle = new GenAction[Constants.genCycleSize];
            ConnectGens(mother, father);
            RandomMutation();
        }

        public GenAction GetCurrentGenAction()
        {
            GenAction genAction = GenActionsCycle[CurrentGenIndex];

            NextGenIndex();
            RandomMutationDuringLive();

            return genAction;
        }

        private void NextGenIndex()
        {
            CurrentGenIndex++;
            if (CurrentGenIndex >= Constants.genCycleSize)
            {
                CurrentGenIndex = 0;
            }
        }
        private void ConnectGens(CellGen mother, CellGen father)
        {
            Array.Copy(father.GenActionsCycle, GenActionsCycle, Constants.genCycleSize);

            do
            {
                int startGen = random.Next(0, GenActionsCycle.Length);
                int endGen = random.Next(startGen, GenActionsCycle.Length);

                for(int i = startGen; i < endGen+1; i++)
                {
                    GenActionsCycle[i] = mother.GenActionsCycle[i];
                }

            }while(random.Next(0, 2) == 0);
        }
        private void RandomMutation()
        {
            bool stopMutation = true;
            do
            {
                if (random.NextDouble() < Constants.randomGenMutationProbability)
                {
                    GenActionsCycle[random.Next(0, GenActionsCycle.Length)] = (GenAction)random.Next(0, 9);
                }
                else
                {
                    stopMutation = false;
                }
            } while (stopMutation);
        }
        private void RandomMutationDuringLive()
        {
            bool stopMutation = true;
            do
            {
                if (random.NextDouble() < Constants.randomGenMutationDuringLiveProbability)
                {
                    GenActionsCycle[random.Next(0, GenActionsCycle.Length)] = (GenAction)random.Next(0, 9);
                }
                else
                {
                    stopMutation = false;
                }
            } while (stopMutation);
        }
        private void FillRandomGens()
        {
            for (int i = 0; i < GenActionsCycle.Length; i++)
            {
                GenActionsCycle[i] = (GenAction)random.Next(0, 9);
            }
        }
    }
}
