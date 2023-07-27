namespace CrazyMaze
{
    struct Transition
    {
        public int? ToCell { get; private set; }
        public float ArrowX { get; private set; }
        public float ArrowY { get; private set; }
        public float ArrowAngle { get; private set; }

        public Transition(int? toCell, float arrowX, float arrowY, float arrowAngle)
        {
            ToCell = toCell;
            ArrowX = arrowX;
            ArrowY = arrowY;
            ArrowAngle = arrowAngle;
        }
    }
}