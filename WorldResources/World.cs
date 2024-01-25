using CellEvolution.Cell.NN;
using CellEvolution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static СellEvolution.Cell.CellActionHandler;
using СellEvolution.Cell;

namespace СellEvolution.WorldResources
{
    public partial class World
    {
        private Random random = new Random();
        private object lockObject = new object();

        public readonly WorldArea WorldArea;
        public readonly CellActionHandler cellActionHandler;

        public List<CellModel> Cells = new List<CellModel>();

        public Renderer worldRenderer;

        private long currentTurn = 1;
        private int currentYear = 0;
        private int currentDay = 1;
        private int currentHours = 1;

        private DayTime currentDayTime = DayTime.Day;
        private int totalDays = 0;

        public long CurrentTurn => currentTurn;
        public int CurrentYear => currentYear;
        public int CurrentDay => currentDay;
        public int CurrentHours => currentHours;
        public DayTime CurrentDayTime => currentDayTime;
        public int TotalDays => totalDays;

        public bool IsRenderAllow = false;
        public bool IsRenderExists = false;

        public World()
        {
            cellActionHandler = new CellActionHandler(this);
            WorldArea = new WorldArea(this);
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
            SortByInitiation();
            PerformCellLogicParallel();

            WorldArea.ClearDeadCells();
            cellActionHandler.CellStartReproduction();
            cellActionHandler.CellStartCreatingClones();
            WorldArea.ClearAreaVoiceParallel();

            UpdateTimeAndSeason();
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

        private void UpdateTimeAndSeason()
        {
            Console.CursorVisible = false;
            if (CurrentHours >= Constants.numOfTurnsInDayTime)
            {
                currentDayTime = DayTime.Night;
                if (CurrentHours > Constants.numOfTurnsInDayTime + Constants.numOfTurnsInNightTime)
                {
                    currentDayTime = DayTime.Day;
                    currentHours = 1;
                    currentDay++;
                    totalDays++;
                }
                if (CurrentDay > Constants.numOfDaysInYear)
                {
                    currentYear++;
                    currentDay = 1;
                }
            }
            currentTurn++;
            currentHours++;
        }
        private void SortByInitiation()
        {
            Shuffle(Cells);
            Cells = Cells.OrderBy(c => c.Initiation).ToList();
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
