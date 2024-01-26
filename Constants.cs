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
        public const int minNightPhotosynthesisFine = 20; //20
        public const int availableCellNumAroundMax = 3;
        public const int availableCellNumAroundMin = 2;

        public const int jumpEnergyCost = 3;
        public const int jumpDistance = 3;
        public const int energyBankChangeNum = 5;

        public const int numOfMemoryLastMoves = 10;
        public const int genCycleSize = liveTime;

        public const int brainInputDayNightPoweredK = 100;
        public const int brainInputInitiationPoweredK = 10;
        public const int brainInputMaxClonePoweredK = 10;
        public const int brainInputCurrentAgePoweredK = 10;
        public const int brainInputEnergyBankPoweredK = 10;
        public const int brainLastMovePoweredK = 5;


        public const double randomGenMutationProbability = 0.2;
        public const double randomGenMutationDuringLiveProbability = 0.01;

        public const double learningRate = 0.00001;
        public const double noiseIntensity = 0.01;
        public const double dropoutProbability = 0.1;
        public const double learnFromExpProbability = 0.9;

        public const double cloneNoiseProbability = 0.2;
        public const double cloneNoiseWeightsChangeProc = 0.05;

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
        public const ConsoleColor borderColor = ConsoleColor.White;
        public const ConsoleColor poisonColor = ConsoleColor.White;

        public const int Kempty = 0;
        public const int Kborder = 10;
        public const int Kpoison = 20;

        public const ConsoleColor newCellColor = ConsoleColor.Yellow;
        public const ConsoleColor biteCellColor = ConsoleColor.DarkRed;
        public const ConsoleColor photoCellColor = ConsoleColor.Green;
        public const ConsoleColor absorbCellColor = ConsoleColor.Blue;
        public const ConsoleColor slipCellColor = ConsoleColor.DarkMagenta;
        public const ConsoleColor evolvingCellColor = ConsoleColor.DarkYellow;
        public const ConsoleColor deadCellColor = ConsoleColor.Gray;
        public const ConsoleColor errorCellColor = ConsoleColor.Red;

        public const int KnewCell = 30;
        public const int KbiteCell = 40;
        public const int KphotoCell = 50;
        public const int KabsorbCell = 60;
        public const int KslipCell = 70;
        public const int KevolvingCell = 80;
        public const int KerrorCell = 90;
        public const int KdeadCell = 100;

        


        //Free color -  Magenta, DarkCyan
    }
}
