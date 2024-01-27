namespace CellEvolution.Cell.NN
{
    public partial class CellModel
    {
        public enum CellAction : byte
        {
            //Move
            MoveLeftUp, 
            MoveUp, 
            MoveRightUp, 
            MoveRight,
            MoveRightDown,
            MoveDown,
            MoveLeftDown,
            MoveLeft,

            JumpUp, 
            JumpRight, 
            JumpDown, 
            JumpLeft,

            //Hunt
            BiteLeftUp, 
            BiteUp,
            BiteRightUp,
            BiteRight,
            BiteRightDown, 
            BiteDown, 
            BiteLeftDown, 
            BiteLeft,

            //Photosynthesis
            Photosynthesis,

            //Absorption
            Absorption,

            //Reproduction
            Clone,
            Reproduction,

            //Action
            Slip, 
            Shout, 
            Hide,
            
            //Mine
            Mine,

            //Evolving
            GainInitiation,
            GainMaxClone, 
            GainEnergyBank, 
            DecEnergyBank
        }
    }
}
