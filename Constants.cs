using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CellEvolution
{
    static class Constants
    {
        public const int maxClone = 16;
        public const int maxLive = 128;

        public const int bitePower = 80;

        public const int energyAreaPoisonedCorner = 50;
        public const int poisonedDecEnergy = energyAreaPoisonedCorner;

        public const int futureActionsInputLength = 4;

        public const int cloneEnergyCost = 45;
        public const int startCellEnergy = 40;

        public const int areaEnergyStartVal = 15;
        public const int minEnergyFromDeadCell = cloneEnergyCost - startCellEnergy;
        
        public const int numOfTurnInYear = 256;
        public const int eachYearEnergyCostGain = 0;
        public const int numOfTurnsInDayTimeMin = 8;
        public const int numOfTurnsInDayTimeMax = 8;

        public const int actionEnergyCost = 5;
        public const int slipEnergyCost = 2;

        public const int mineAmount = 50;

        public const int maxPhotosynthesis = 35; //+ min 
        public const int maxNightPhotosynthesisFine = 80; //+ min
        public const int minPhotosynthesis = 10; 
        public const int minNightPhotosynthesisFine = 5; 
        public const int availableCellNumAroundMax = 3;
        public const int availableCellNumAroundMin = 2;

        public const int jumpEnergyCost = 0;
        public const int jumpDistance = 3;

        public const int maxMemoryCapacity = 16;

        public const int meteorBlockOrbNumMin = 100;
        public const int meteorBlockOrbNumMax = 1000;

        public const int meteorMidSizeDistance = 1;
        public const int meteorBigSizeDistance = 2;
        public const int meteorHugeSizeDistance = 3;

        public const double meteorSmallSizeProbability = 0.30;
        public const double meteorMidSizeProbability = 0.05; // 0.05
        public const double meteorBigSizeProbability = 0.005; // 0.005
        public const double meteorHugeSizeProbability = 0.0008; //0.0008

        public const int visionDistance = 3;
        public const int voiceDistance = 5;
        public const int energyAreaVisionDistance = 1;
        public const int energyAreaAbsorbDistance = 1;
        public const int startCellCreationDistance = 2;

        public const int areaSizeX = 47; //118 //105
        public const int areaSizeY = 47; //60 // 47

        public const char nullChar = '\0';
        public const char emptyChar = ' ';
        public const char borderChar = '#';
        public const char cellChar = '@';
        public const char poisonChar = '!';
        public const char meteorChar = '$';

        public const ConsoleColor emptyColor = ConsoleColor.Black;
        public const ConsoleColor borderColor = ConsoleColor.White;
        public const ConsoleColor poisonColor = ConsoleColor.White;
        public const ConsoleColor meteorColor = ConsoleColor.White;

        public const int Kempty = 0;
        public const int Kborder = 1;
        public const int Kpoison = 2;
        public const int Kmeteor = 3;

        public const ConsoleColor newCellColor = ConsoleColor.Yellow;
        public const ConsoleColor biteCellColor = ConsoleColor.DarkRed;
        public const ConsoleColor photoCellColor = ConsoleColor.Green;
        public const ConsoleColor absorbCellColor = ConsoleColor.Blue;
        public const ConsoleColor slipCellColor = ConsoleColor.DarkMagenta;
        public const ConsoleColor hideCellColor = ConsoleColor.Magenta;
        public const ConsoleColor mineCellColor = ConsoleColor.DarkYellow;
        public const ConsoleColor deadCellColor = ConsoleColor.Gray;
        public const ConsoleColor errorCellColor = ConsoleColor.Red;

        public const int KnewCell = 4;
        public const int KbiteCell = 5;
        public const int KphotoCell = 6;
        public const int KabsorbCell = 7;
        public const int KslipCell = 8;
        public const int KhideCell = 9;
        public const int KmineCell = 10;
        public const int KerrorCell = 11;
        public const int KdeadCell = 12;
    }
}
