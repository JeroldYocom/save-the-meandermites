namespace Meandermites.Sim
{
    /// <summary>
    /// Seeded xorshift64*. The only source of randomness allowed in the Sim,
    /// so the same seed always replays the same run (Daily Challenge, Replay).
    /// </summary>
    public sealed class SimRandom
    {
        private ulong _state;

        public SimRandom(ulong seed)
        {
            // Zero is a fixed point of xorshift; remap it.
            _state = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        public ulong State => _state;

        public ulong NextUInt64()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            return (int)(NextUInt64() % (ulong)maxExclusive);
        }
    }
}
