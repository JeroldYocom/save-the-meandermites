namespace Meandermites.Sim
{
    /// <summary>
    /// What the player did on one step. A replay is just the list of these
    /// plus the level seed.
    /// </summary>
    public readonly struct SimInput
    {
        public readonly bool PlaceBarrier;
        public readonly SimVec2 BarrierPosition;
        public readonly bool Breathe;

        public SimInput(bool placeBarrier, SimVec2 barrierPosition, bool breathe)
        {
            PlaceBarrier = placeBarrier;
            BarrierPosition = barrierPosition;
            Breathe = breathe;
        }

        public static SimInput None => new SimInput(false, SimVec2.Zero, false);

        public static SimInput Barrier(float x, float y) =>
            new SimInput(true, new SimVec2(x, y), false);

        public static SimInput BreatheNow => new SimInput(false, SimVec2.Zero, true);
    }
}
