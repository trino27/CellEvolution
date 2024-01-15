namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
    {
        private Random random = new Random();
        public GenActions[] GenActionsCycle;

        public bool flag = false;

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
            Array.Copy(mother.GenActionsCycle, GenActionsCycle, Constants.genCycleSize);

            for (int i = 0; i < father.GenActionsCycle.Length; i++)
            {
                if (random.Next(0, 2) == 0)
                {
                    GenActions gen = father.GetCurrentGenAction(i);
                    GenActionsCycle[i] = gen;
                }
            }
        }
        private void RandomMutation()
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
        private void FillRandomGens()
        {
            for (int i = 0; i < GenActionsCycle.Length; i++)
            {
                GenActionsCycle[i] = (GenActions)random.Next(0, 8);
            }
        }

        private void HardCodeGens() //!!!!!!!!!!!!!
        {
            //if (flag)
            //{
            //    for (int i = 0; i < GenActionsCycle.Length; i++)
            //    {
            //        GenActionsCycle[i] = GenActions.All;
            //        if (i == GenActionsCycle.Length-1)
            //        {
            //            GenActionsCycle[i] = GenActions.Reproduction;
            //        }
            //    }
            //}
        }
    }
}
