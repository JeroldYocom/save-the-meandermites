using System;

namespace Meandermites.Sim
{
    /// <summary>
    /// Every tuning constant, in one file. Values come from GDD v3.1 section 4
    /// and 8; the GDD is authoritative. Changing one means adding a line to the
    /// wiki's Feel Tuning Log.
    /// Durations are whole numbers of fixed steps so they never accumulate
    /// floating-point drift; SimConstants.Steps converts from seconds.
    /// </summary>
    public static class SimConstants
    {
        public const float StepSeconds = 0.02f;

        public static int Steps(float seconds) => (int)Math.Round(seconds / StepSeconds);

        // Playfield (1 unit = 64 px at reference resolution)
        public const float FieldWidth = 16f;
        public const float FieldHeight = 9f;

        // Movement
        public const float BaseMiteSpeed = 1.4f;
        public const float MaxMiteSpeed = 3.0f;
        public const float MiteRadius = 0.4f;

        // Flame
        public const float CatchRadius = 0.9f;
        public const float BasePullStrength = 0.45f;
        public const float PullScaleWhileHolding = 0.5f;
        public const float MinSpawnDistanceFromFlame = 5f;

        // Barrier
        public const float BarrierRadius = 1.1f;
        public const int BarrierDurationSteps = 150;   // 3.0 s
        public const int BarrierCooldownSteps = 50;    // 1.0 s after expiry
        public const int TriggerImmunitySteps = 30;    // 0.6 s

        // Reaction and The Catch
        public const int ReactionSteps = 100;          // 2.0 s
        public const int CatchSteps = 100;             // 2.0 s struggle
        public const float RescueFlingDistance = 3.0f;
        public const int RescueInvincibleSteps = 50;   // 1.0 s
        public const float RescuePanic = 1.0f;

        // Breathe. The GDD says "0.25 for 2 seconds, cooldown 20s" without
        // saying real or sim time; see docs/design/sim-interpretations.md.
        public const float BreatheRateScale = 0.25f;
        public const int BreatheActiveSteps = 25;      // 2 real s at 0.25 rate
        public const int BreatheCooldownSteps = 1000;  // 20 s of sim time

        // Spawn placement (see SpawnPlanner)
        public const float SpawnSpacing = 0.9f;
        public const float PerimeterSlideStep = 0.25f;
    }
}
