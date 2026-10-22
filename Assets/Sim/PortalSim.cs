namespace Meandermites.Sim
{
    /// <summary>
    /// A Spawn Portal on the field edge (decision 1.3). It fades in for
    /// PortalFadeInSteps, then releases its Mites one at a time,
    /// PortalSpawnIntervalSteps apart. Release is a function of the step
    /// count only, so it is deterministic. Phase 0 has one pattern
    /// (Sequence); other patterns will change the release times here.
    /// </summary>
    public sealed class PortalSim
    {
        public SimVec2 Position { get; }
        public int FirstMite { get; }
        public int MiteCount { get; }
        /// <summary>Step on which the first Mite leaves.</summary>
        public int ReleaseStep { get; }

        /// <summary>Steps between one Mite leaving and the next.</summary>
        public int IntervalSteps { get; }

        /// <summary>True once the last Mite has left.</summary>
        public bool Released { get; internal set; }

        public PortalSim(SimVec2 position, int firstMite, int miteCount, int releaseStep, int intervalSteps)
        {
            IntervalSteps = intervalSteps;
            Position = position;
            FirstMite = firstMite;
            MiteCount = miteCount;
            ReleaseStep = releaseStep;
        }

        /// <summary>Step on which the last Mite leaves.</summary>
        public int LastReleaseStep => ReleaseStep + (MiteCount - 1) * IntervalSteps;

        /// <summary>Step on which Mite number j (0-based within this portal) leaves.</summary>
        public int ReleaseStepOf(int j) => ReleaseStep + j * IntervalSteps;

        /// <summary>0 when the portal begins to appear, 1 when it releases its Mites.</summary>
        public float FadeProgress(long stepCount)
        {
            if (Released || stepCount >= ReleaseStep) return 1f;
            return (float)stepCount / ReleaseStep;
        }
    }
}
