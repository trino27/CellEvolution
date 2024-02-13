using CellEvolution.Cell.CellModel;
using CellEvolution.WorldResources.Cell;
using CellEvolution.WorldResources.Meteor;
using СellEvolution;
using СellEvolution.WorldResources.World;

namespace CellEvolution.WorldResources
{
    public partial class WorldModel
    {
        private Random random = new Random();
        private object lockObject = new object();

        public readonly WorldArea WorldArea;
        public readonly CellActionHandler cellActionHandler;

        public List<CellModel> Cells = new List<CellModel>();

        public Renderer worldRenderer;

        private int numOfTurnInDay = 0;
        private int numOfTurnInNight = 0;

        private long currentTurn = 0;
        private int currentYear = 0;
        private int currentDay = 1;
        private int currentHours = 1;

        private int MeteorNight = 0;

        private DayTime currentDayTime = DayTime.Day;
        private int totalDays = 0;

        public int NumOfTurnInDay => numOfTurnInDay;
        public int NumOfTurnInNight => numOfTurnInNight;


        public long CurrentTurn => currentTurn;
        public int CurrentYear => currentYear;
        public int CurrentDay => currentDay;
        public int CurrentHours => currentHours;
        public DayTime CurrentDayTime => currentDayTime;
        public int TotalDays => totalDays;

        public bool IsRenderAllow = false;
        public bool IsRenderExists = false;

        public WorldModel()
        {
            cellActionHandler = new CellActionHandler(this);
            WorldArea = new WorldArea(this);
            CreateNewDayTime();
        }

        public CellModel GetCell(int x, int y)
        {
            lock (lockObject)
            {
                return Cells.FirstOrDefault(c => c.PositionX == x && c.PositionY == y);
            }
        }

        public void MakeTurn()
        {
            Shuffle(Cells);
            UpdateTimeAndSeason();

            PerformCellLogicParallel();

            WorldArea.ClearDeadCells();

            MeteorFalling();

            cellActionHandler.CellStartReproduction();
            cellActionHandler.CellStartCreatingClones();


        }

        private void MeteorFalling()
        {
            MeteorModel meteor = new MeteorModel();

            if (meteor.IsCreateMeteorBlocks)
            {
                foreach (var meteorBlock in meteor.MeteorBlocks)
                {
                    KillCellAtArea(meteorBlock.PositionX, meteorBlock.PositionY);
                    WorldArea.CreateMeteorBlock(meteorBlock);
                }

                if (meteor.NightTurns > MeteorNight)
                {
                    MeteorNight = meteor.NightTurns;
                }
            }
        }

        private void KillCellAtArea(int x, int y)
        {
            if (GetCell(x, y) != null)
            {
                CellModel targetCell = GetCell(x, y);
                targetCell.IsDead = true;
                Cells.Remove(targetCell);

                WorldArea.DeadCellToAreaEnergy(targetCell);
                WorldArea.ClearAreaFromDeadCell(targetCell.PositionX, targetCell.PositionY);
            }
        }

        private void PerformCellLogicParallel()
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < Cells.Count; i++)
            {
                int index = i;
                Task task = Task.Run(() =>
                {
                    cellActionHandler.CellMove(index);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void InitWorldRenderer(Renderer worldRenderer)
        {
            this.worldRenderer = worldRenderer;
            IsRenderExists = true;
        }
        public bool StartRenderIfRendererExists()
        {
            if (IsRenderExists)
            {
                IsRenderAllow = true;
                return false;
            }
            return true;
        }
        public bool CreateVisualIfRendererExists()
        {
            if (IsRenderExists)
            {
                worldRenderer.CreateWorldVisual();
                return false;
            }
            return true;
        }
        public void StopRenderer() => IsRenderAllow = false;

        private void CreateNewDayTime()
        {
            numOfTurnInNight = random.Next(Constants.numOfTurnsInDayTimeMin, Constants.numOfTurnsInDayTimeMax + 1);
            numOfTurnInDay = random.Next(numOfTurnInNight, Constants.numOfTurnsInDayTimeMax + 1);
        }
        private void UpdateTimeAndSeason()
        {
            Console.CursorVisible = false;
            currentTurn++;
            currentHours++;

            if (MeteorNight == 0)
            {
                if (CurrentHours > numOfTurnInDay)
                {
                    currentDayTime = DayTime.Night;

                }
                else
                {
                    currentDayTime = DayTime.Day;
                }

            }
            else
            {
                currentDayTime = DayTime.Night;
                MeteorNight--;
            }

            if (CurrentHours > numOfTurnInDay + numOfTurnInNight)
            {
                currentHours = 1;
                currentDay++;
                totalDays++;
            }
            if (CurrentTurn % Constants.numOfTurnInYear == 0 && CurrentTurn != 0)
            {
                currentYear++;
                CreateNewDayTime();
                currentDay = 1;
            }
        }
        private void Shuffle<T>(List<T> array)
        {
            int size = array.Count;

            while (size > 1)
            {
                size--;
                int k = random.Next(size + 1);
                T temp = array[k];
                array[k] = array[size];
                array[size] = temp;
            }
        }
    }
}
