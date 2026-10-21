namespace Meandermites.Sim
{
    /// <summary>
    /// Root of the deterministic simulation. Advances in fixed steps only;
    /// the Unity layer pumps Step() and renders what it reads back.
    /// Placeholder body: real entities arrive with Phase 0.
    /// </summary>
    public sealed class Simulation
    {
        /// <summary>Fixed timestep in seconds. Never read a wall clock here.</summary>
        public const float StepSeconds = 0.02f;

        private readonly SimRandom _rng;

        public Simulation(ulong seed)
        {
            Seed = seed;
            _rng = new SimRandom(seed);
        }

        public ulong Seed { get; }
        public long StepCount { get; private set; }

        public void Step()
        {
            _rng.NextUInt64();
            StepCount++;
        }

        /// <summary>
        /// Hash of everything that defines the sim state. Every new piece of
        /// state must be folded in here or determinism tests stop covering it.
        /// </summary>
        public ulong StateHash()
        {
            unchecked
            {
                ulong h = 1469598103934665603UL;
                h = (h ^ _rng.State) * 1099511628211UL;
                h = (h ^ (ulong)StepCount) * 1099511628211UL;
                return h;
            }
        }
    }
}
