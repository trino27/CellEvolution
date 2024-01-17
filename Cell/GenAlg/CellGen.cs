namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
    {
        private Random random = new Random();
        public GenActions[] GenActionsCycle;

        public bool flag = true;

        public CellGen()
        {
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            FillRandomGens();

            HardCodeGens();
        }
        public CellGen(CellGen original, bool flag)
        {
            this.flag = flag;
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            Array.Copy(original.GenActionsCycle, GenActionsCycle, Constants.genCycleSize);
            RandomMutation();

            HardCodeGens();
        }
        public CellGen(CellGen mother, CellGen father, bool flag)
        {
            this.flag = flag;
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            ConnectGens(mother, father);
            RandomMutation();

            HardCodeGens();
        }

        public GenActions GetCurrentGenAction(int CurrentIndex)
        {
            GenActions genAction = GenActionsCycle[CurrentIndex];

            return genAction;
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

            }while(random.Next(0, 3) != 0);
        }
        public void RandomMutation()
        {
            bool stopMutation = true;
            do
            {
                if (random.Next(0, 7) == 0)
                {
                    GenActionsCycle[random.Next(0, GenActionsCycle.Length)] = (GenActions)random.Next(0, 8);
                }
                else
                {
                    stopMutation = false;
                }
            } while (stopMutation);
        }
        public void RandomMutationDuringLive()
        {
            bool stopMutation = true;
            do
            {
                if (random.Next(0, 256) == 0)
                {
                    GenActionsCycle[random.Next(0, GenActionsCycle.Length)] = (GenActions)random.Next(0, 8);
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
                GenActionsCycle[i] = (GenActions)random.Next(0, 8);
            }
        }

        private void HardCodeGens() //!!!!!!!!!!!!!
        {
            if (flag)
            {
                for (int i = 0; i < GenActionsCycle.Length; i++)
                {
                    GenActionsCycle[i] = GenActions.All;
                }

                GenActionsCycle[^1] = GenActions.Reproduction;
            }
        }
    }
}
