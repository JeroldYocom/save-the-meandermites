namespace Meandermites.Sim
{
    /// <summary>
    /// A Spawn Portal on the field edge (decision 1.3). It fades in for
    /// PortalFadeInSteps, then releases its Mites in one burst. Release is a
    /// function of the step count only, so it is deterministic. Phase 0 has
    /// one pattern (Burst); other patterns will add release times here.
    /// </summary>
    public sealed class PortalSim
    {
        public SimVec2 Position { get; }
        public int FirstMite { get; }
        public int MiteCount { get; }
        public int ReleaseStep { get; }
        public bool Released { get; internal set; }

        public PortalSim(SimVec2 position, int firstMite, int miteCount, int releaseStep)
        {
            Position = position;
            FirstMite = firstMite;
            MiteCount = miteCount;
            ReleaseStep = releaseStep;
        }

        /// <summary>0 when the portal begins to appear, 1 when it releases its Mites.</summary>
        public float FadeProgress(long stepCount)
        {
            if (Released || stepCount >= ReleaseStep) return 1f;
            return (float)stepCount / ReleaseStep;
        }
    }
}
