using System;

namespace Meandermites.Sim
{
    public readonly struct MiteSpec
    {
        public readonly int Id;
        public readonly MiteReaction Reaction;

        public MiteSpec(int id, MiteReaction reaction)
        {
            Id = id;
            Reaction = reaction;
        }
    }

    /// <summary>
    /// Everything that defines a level run. The same config and the same
    /// input list always produce the same outcome.
    /// </summary>
    public sealed class LevelConfig
    {
        public ulong Seed { get; }
        public int DurationSteps { get; }
        public SimVec2 FlamePosition { get; }
        public MiteSpec[] Mites { get; }

        /// <summary>
        /// Mites released by each Spawn Portal, in order. The sum must equal
        /// the number of Mites. Phase 0 releases every portal as a Burst.
        /// </summary>
        public int[] PortalCounts { get; }

        /// <summary>
        /// Optional hand-placed start positions, one per Mite, replacing
        /// portal placement. Used by tests and later by teaching levels.
        /// </summary>
        public SimVec2[] ExplicitStarts { get; }

        public LevelConfig(ulong seed, int durationSteps, SimVec2 flamePosition,
                           MiteSpec[] mites, int[] portalCounts, SimVec2[] explicitStarts = null)
        {
            if (mites == null || mites.Length == 0)
                throw new ArgumentException("A level needs at least one Mite.");
            if (durationSteps <= 0)
                throw new ArgumentException("Duration must be positive.");

            if (explicitStarts != null)
            {
                if (explicitStarts.Length != mites.Length)
                    throw new ArgumentException("ExplicitStarts must have one entry per Mite.");
            }
            else
            {
                if (portalCounts == null || portalCounts.Length == 0)
                    throw new ArgumentException("A level needs at least one portal.");
                int total = 0;
                for (int i = 0; i < portalCounts.Length; i++)
                {
                    if (portalCounts[i] <= 0)
                        throw new ArgumentException("Every portal must release at least one Mite.");
                    total += portalCounts[i];
                }
                if (total != mites.Length)
                    throw new ArgumentException("Portal counts must add up to the number of Mites.");
            }

            Seed = seed;
            DurationSteps = durationSteps;
            FlamePosition = flamePosition;
            Mites = mites;
            PortalCounts = portalCounts;
            ExplicitStarts = explicitStarts;
        }

        public static SimVec2 FieldCentre =>
            new SimVec2(SimConstants.FieldWidth * 0.5f, SimConstants.FieldHeight * 0.5f);

        /// <summary>Three Leftythes, one portal, 40 s: the Level 1-2 shape.</summary>
        public static LevelConfig Default(ulong seed)
        {
            var mites = new[]
            {
                new MiteSpec(1, MiteReaction.TurnLeft),
                new MiteSpec(2, MiteReaction.TurnLeft),
                new MiteSpec(3, MiteReaction.TurnLeft)
            };
            return new LevelConfig(seed, SimConstants.Steps(40f), FieldCentre, mites, new[] { 3 });
        }
    }
}
