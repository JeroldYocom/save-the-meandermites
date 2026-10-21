namespace Meandermites.Sim
{
    /// <summary>
    /// Breathe lives in the Sim (decision 1.4). It changes no gameplay by
    /// itself; the Unity layer reads StepRateScale to decide how many fixed
    /// steps to pump per real second. Because activation is a recorded input
    /// and the timers count steps, replays reproduce it exactly.
    /// </summary>
    public sealed class BreatheSim
    {
        public bool Active => ActiveStepsLeft > 0;
        public int ActiveStepsLeft { get; private set; }
        public int CooldownStepsLeft { get; private set; }

        public bool CanActivate => !Active && CooldownStepsLeft == 0;

        /// <summary>1 normally, BreatheRateScale while Breathe is active.</summary>
        public float StepRateScale => Active ? SimConstants.BreatheRateScale : 1f;

        internal void Tick()
        {
            if (ActiveStepsLeft > 0) ActiveStepsLeft--;
            if (CooldownStepsLeft > 0) CooldownStepsLeft--;
        }

        internal bool TryActivate()
        {
            if (!CanActivate) return false;
            ActiveStepsLeft = SimConstants.BreatheActiveSteps;
            CooldownStepsLeft = SimConstants.BreatheCooldownSteps;
            return true;
        }
    }
}
