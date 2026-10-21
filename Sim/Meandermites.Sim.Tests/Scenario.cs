using Meandermites.Sim;

namespace Meandermites.Sim.Tests
{
    /// <summary>Small builders so each test states only what it cares about.</summary>
    internal static class Scenario
    {
        public static readonly SimVec2 Flame = LevelConfig.FieldCentre; // (8, 4.5)

        /// <summary>One Mite at an explicit start, flame at the centre.</summary>
        public static Simulation Single(MiteReaction reaction, SimVec2 start, int durationSteps = 20000)
        {
            return Many(durationSteps, new[] { reaction }, new[] { start });
        }

        public static Simulation Many(int durationSteps, MiteReaction[] reactions, SimVec2[] starts)
        {
            var specs = new MiteSpec[reactions.Length];
            for (int i = 0; i < specs.Length; i++) specs[i] = new MiteSpec(i + 1, reactions[i]);
            var config = new LevelConfig(1, durationSteps, Flame, specs, null, starts);
            return new Simulation(config);
        }

        /// <summary>A Mite five units left of the flame, walking straight at it.</summary>
        public static Simulation LeftOfFlame(MiteReaction reaction = MiteReaction.TurnLeft, int durationSteps = 20000)
        {
            return Single(reaction, new SimVec2(3f, 4.5f), durationSteps);
        }

        /// <summary>
        /// A Mite that never reaches its flame (the flame is thousands of units
        /// away), for tests that need a long, uninterrupted run.
        /// </summary>
        public static Simulation Remote(int durationSteps = 20000)
        {
            var specs = new[] { new MiteSpec(1, MiteReaction.TurnLeft) };
            var config = new LevelConfig(1, durationSteps, new SimVec2(10000f, 4.5f), specs, null,
                                         new[] { new SimVec2(3f, 4.5f) });
            return new Simulation(config);
        }

        public static void Run(Simulation sim, int steps)
        {
            for (int i = 0; i < steps; i++) sim.Step();
        }

        /// <summary>Steps with no input until the first Mite is Caught.</summary>
        public static int RunUntilCaught(Simulation sim, int maxSteps = 1000)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                sim.Step();
                if (sim.Mites[0].State == MiteState.Caught) return i + 1;
            }
            throw new System.InvalidOperationException("Mite was never caught.");
        }

        public static float Distance(SimVec2 a, SimVec2 b) => (a - b).Length;
    }
}
