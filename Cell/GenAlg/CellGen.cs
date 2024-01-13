namespace CellEvolution.Cell.GenAlg
{
    public partial struct CellGen
    {
        private Random random = new Random();
        public GenActions[] GenActionsCycle;

        public CellGen()
        {
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            FillRandomGens();
            //HardCodeGens();
        }
        public CellGen(CellGen original)
        {
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            Array.Copy(original.GenActionsCycle, GenActionsCycle, Constants.genCycleSize);
            RandomMutation();
        }
        public CellGen(CellGen mother, CellGen father)
        {
            GenActionsCycle = new GenActions[Constants.genCycleSize];
            ConnectGens(mother, father);
            RandomMutation();
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
                    GenActionsCycle[random.Next(0, GenActionsCycle.Length)] = (GenActions)random.Next(0, 9);
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
                GenActionsCycle[i] = (GenActions)random.Next(0, 9);
            }
        }

        private void HardCodeGens()
        {

            int i = 0;
            while(i < GenActionsCycle.Length)
            {
                GenActionsCycle[i] = (GenActions)2; i++;//0
                GenActionsCycle[i] = (GenActions)2; i++;//1
                GenActionsCycle[i] = (GenActions)2; i++;//2
                GenActionsCycle[i] = (GenActions)2; i++;//3
                GenActionsCycle[i] = (GenActions)2; i++;//4
                GenActionsCycle[i] = (GenActions)2; i++;//5
                GenActionsCycle[i] = (GenActions)2; i++;//6
                GenActionsCycle[i] = (GenActions)2; i++;//7
                GenActionsCycle[i] = (GenActions)2; i++;//8
                GenActionsCycle[i] = (GenActions)7; i++;//9
                GenActionsCycle[i] = (GenActions)7; i++;//10
                GenActionsCycle[i] = (GenActions)7; i++;//11
                GenActionsCycle[i] = (GenActions)7; i++;//12
                GenActionsCycle[i] = (GenActions)7; i++;//13
                GenActionsCycle[i] = (GenActions)7; i++;//14
                GenActionsCycle[i] = (GenActions)4; i++;//15
            }
        }

    }
}
