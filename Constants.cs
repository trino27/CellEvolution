using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellEvolution
{
    static class Constants
    {
        public const int liveTime = 64;
        public const int poisonedDecLive = 5;
        public const int energyAreaPoisonedCorner = 50;

        public const int actionLiveCost = 1;
        public const int slipLiveCost = 1;

        public const int cloneEnergyCost = 45;
        public const int startCellEnergy = 40;

        public const int areaEnergyStartVal = 15;
        public const int minEnergyFromDeadCell = cloneEnergyCost - startCellEnergy;
        
        public const int numOfDaysInYear = 30;
        public const int eachYearEnergyCostGain = 0;
        public const int numOfTurnsInDayTime = 9;
        public const int numOfTurnsInNightTime = 7;

        public const int actionEnergyCost = 5;
        public const int slipEnergyCost = 2;

        public const int maxPhotosynthesis = 35; //+ min 
        public const int maxNightPhotosynthesisFine = 100; //+ min
        public const int minPhotosynthesis = 10; 
        public const int minNightPhotosynthesisFine = 20;
        public const int availableCellNumAroundMax = 3;
        public const int availableCellNumAroundMin = 2;

        public const int jumpEnergyCost = 3;
        public const int jumpDistance = 3;
        public const int energyBankChangeNum = 5;

        public const int numOfMemoryLastMoves = 10;
        public const int genCycleSize = liveTime;

        public const double learningRate = 0.0001;
        public const double noiseIntensity = 0.001;
        public const double dropoutProbability = 0.25;
        public const double learnFromExpProbability = 1;
        public const double cloneNoiseProbability = 0.1;

        public const double createdCloneEmotionK = 45;
        public const double energyEmotionK = 1;
        public const double normalDifEmotionProc = 10;

        public const int visionDistance = 3;
        public const int voiceDistance = 5;
        public const int energyAreaVisionDistance = 1;
        public const int energyAreaAbsorbDistance = 1;
        public const int startCellCreationDistance = 2;

        public const int areaSizeX = 105; //118 //105
        public const int areaSizeY = 47; //60 // 47

        public const char nullChar = '\0';
        public const char emptyChar = ' ';
        public const char borderChar = '#';
        public const char cellChar = '@';
        public const char poisonChar = '!';


        public const ConsoleColor emptyColor = ConsoleColor.Black;
        public const ConsoleColor wallColor = ConsoleColor.White;
        public const ConsoleColor poisonColor = ConsoleColor.White;

        public const ConsoleColor newCellColor = ConsoleColor.Yellow;
        public const ConsoleColor biteCellColor = ConsoleColor.DarkRed;
        public const ConsoleColor photoCellColor = ConsoleColor.Green;
        public const ConsoleColor absorbCellColor = ConsoleColor.Blue;
        public const ConsoleColor slipCellColor = ConsoleColor.DarkMagenta;
        public const ConsoleColor evolvingCellColor = ConsoleColor.DarkYellow;
        public const ConsoleColor deadCellColor = ConsoleColor.Gray;

        public const ConsoleColor errorColor = ConsoleColor.Red;


        //Free color -  Magenta, DarkCyan
    }
}
