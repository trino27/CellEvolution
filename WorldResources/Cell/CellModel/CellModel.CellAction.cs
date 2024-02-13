namespace CellEvolution.Cell.CellModel
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
            Hide,
            
            //Mine
            MineTop,
            MineRightSide,
            MineBottom, 
            MineLeftSide,
        }
    }
}
