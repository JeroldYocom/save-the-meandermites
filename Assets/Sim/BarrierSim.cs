namespace Meandermites.Sim
{
    /// <summary>
    /// The single barrier the player can have out (one active at a time in
    /// Chapter 1). Lifetime and cooldown are counted in steps.
    /// </summary>
    public sealed class BarrierSim
    {
        public bool Active { get; private set; }
        public SimVec2 Position { get; private set; }
        public int StepsLeft { get; private set; }
        public int CooldownStepsLeft { get; private set; }

        public bool CanPlace => !Active && CooldownStepsLeft == 0;

        /// <summary>Start of every step: age the barrier or its cooldown.</summary>
        internal void Tick()
        {
            if (Active)
            {
                StepsLeft--;
                if (StepsLeft <= 0)
                {
                    Active = false;
                    StepsLeft = 0;
                    CooldownStepsLeft = SimConstants.BarrierCooldownSteps;
                }
            }
            else if (CooldownStepsLeft > 0)
            {
                CooldownStepsLeft--;
            }
        }

        /// <summary>Places the barrier instantly, or returns false if it cannot.</summary>
        internal bool TryPlace(SimVec2 position)
        {
            if (!CanPlace) return false;
            if (position.X < 0f || position.X > SimConstants.FieldWidth ||
                position.Y < 0f || position.Y > SimConstants.FieldHeight)
                return false;

            Active = true;
            Position = position;
            StepsLeft = SimConstants.BarrierDurationSteps;
            return true;
        }

        public bool Covers(SimVec2 point)
        {
            if (!Active) return false;
            float r = SimConstants.BarrierRadius;
            return (point - Position).SqrLength <= r * r;
        }
    }
}
