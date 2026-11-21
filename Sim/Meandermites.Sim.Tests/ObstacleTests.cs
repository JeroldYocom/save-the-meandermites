using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>Stones: solid circles Mites slide around (proposal approved 2026-11-20).</summary>
    public class ObstacleTests
    {
        private static Simulation WithStones(SimObstacle[] stones, MiteReaction[] reactions, SimVec2[] starts,
                                             int durationSteps = 20000)
        {
            var specs = new MiteSpec[reactions.Length];
            for (int i = 0; i < specs.Length; i++) specs[i] = new MiteSpec(i + 1, reactions[i]);
            var config = new LevelConfig(1, durationSteps, Scenario.Flame, specs, null, starts, stones);
            return new Simulation(config);
        }

        private static void AssertClearOfStones(Simulation sim, string context)
        {
            foreach (SimObstacle o in sim.Config.Obstacles)
            {
                foreach (MiteSim m in sim.Mites)
                {
                    if (m.State != MiteState.Walking && m.State != MiteState.Reacting) continue;
                    float dist = Scenario.Distance(m.Position, o.Centre);
                    Assert.GreaterOrEqual(dist, o.Reach - 0.001f, context + ": Mite " + m.Id + " is inside a Stone");
                }
            }
        }

        [Test]
        public void MiteWalkingStraightAtAStoneSlidesAroundItAndReachesTheFlame()
        {
            // Flame at (8, 4.5); the Mite starts at (3, 4.5) with a Stone dead on the line.
            var stones = new[] { new SimObstacle(new SimVec2(5.5f, 4.5f), 0.8f) };
            var sim = WithStones(stones, new[] { MiteReaction.TurnLeft }, new[] { new SimVec2(3f, 4.5f) });

            int steps = 0;
            while (sim.Mites[0].State != MiteState.Caught && steps < 2000)
            {
                sim.Step();
                steps++;
                AssertClearOfStones(sim, "step " + steps);
            }

            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State, "the Mite must not stick behind the Stone");
        }

        [Test]
        public void AStoneAddsTimeToTheWalk()
        {
            var open = WithStones(null, new[] { MiteReaction.TurnLeft }, new[] { new SimVec2(3f, 4.5f) });
            var blocked = WithStones(new[] { new SimObstacle(new SimVec2(5.5f, 4.5f), 0.8f) },
                                     new[] { MiteReaction.TurnLeft }, new[] { new SimVec2(3f, 4.5f) });

            int openSteps = Scenario.RunUntilCaught(open);
            int blockedSteps = Scenario.RunUntilCaught(blocked, 3000);

            Assert.Greater(blockedSteps, openSteps);
        }

        [Test]
        public void NoMiteEverOverlapsAStoneOverAFullSeededRun()
        {
            var stones = new[]
            {
                new SimObstacle(new SimVec2(5.4f, 4.5f), 0.8f),
                new SimObstacle(new SimVec2(10.6f, 4.5f), 0.8f),
                new SimObstacle(new SimVec2(8f, 7f), 0.8f)
            };
            var mites = new[]
            {
                new MiteSpec(1, MiteReaction.TurnLeft),
                new MiteSpec(2, MiteReaction.TurnRight),
                new MiteSpec(3, MiteReaction.RunAway),
                new MiteSpec(4, MiteReaction.TurnLeft)
            };

            for (ulong seed = 1; seed <= 20; seed++)
            {
                var config = new LevelConfig(seed, SimConstants.Steps(40f), Scenario.Flame, mites,
                                             new[] { 2, 2 }, null, stones);
                var sim = new Simulation(config);
                for (int i = 0; i < 1500 && sim.Outcome == SimOutcome.Running; i++)
                {
                    // A barrier every second keeps Mites reacting, which pushes them into Stones.
                    sim.Step(i % 50 == 0 ? SimInput.Barrier(4f + (i % 8), 3f + (i % 3)) : SimInput.None);
                    AssertClearOfStones(sim, "seed " + seed + " step " + i);
                }
            }
        }

        [Test]
        public void AMiteDeadBehindAStoneStillGetsPast()
        {
            // Flame (8, 4.5), Stone directly between, Mite exactly on the axis: no sideways velocity at all.
            var stones = new[] { new SimObstacle(new SimVec2(6f, 4.5f), 1.0f) };
            var sim = WithStones(stones, new[] { MiteReaction.TurnLeft }, new[] { new SimVec2(2f, 4.5f) });

            for (int i = 0; i < 3000 && sim.Mites[0].State == MiteState.Walking; i++) sim.Step();

            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State);
        }

        [Test]
        public void ALevelWithNoStonesIsIdenticalToOneWithAnEmptyList()
        {
            var specs = new[] { new MiteSpec(1, MiteReaction.TurnLeft), new MiteSpec(2, MiteReaction.TurnRight) };
            var a = new Simulation(new LevelConfig(5, 5000, Scenario.Flame, specs, new[] { 2 }));
            var b = new Simulation(new LevelConfig(5, 5000, Scenario.Flame, specs, new[] { 2 }, null, new SimObstacle[0]));

            for (int i = 0; i < 800; i++)
            {
                a.Step(i == 300 ? SimInput.Barrier(6f, 4f) : SimInput.None);
                b.Step(i == 300 ? SimInput.Barrier(6f, 4f) : SimInput.None);
            }

            Assert.AreEqual(a.StateHash(), b.StateHash());
        }

        [Test]
        public void ASeededRunWithStonesReplaysIdentically()
        {
            var stones = new[] { new SimObstacle(new SimVec2(5.4f, 4.5f), 0.8f) };
            var specs = new[] { new MiteSpec(1, MiteReaction.TurnLeft), new MiteSpec(2, MiteReaction.RunAway) };
            ulong first = 0;
            for (int run = 0; run < 2; run++)
            {
                var sim = new Simulation(new LevelConfig(9, 5000, Scenario.Flame, specs, new[] { 2 }, null, stones));
                for (int i = 0; i < 1000; i++) sim.Step(i % 120 == 0 ? SimInput.Barrier(5f, 4f) : SimInput.None);
                if (run == 0) first = sim.StateHash();
                else Assert.AreEqual(first, sim.StateHash());
            }
        }

        [Test]
        public void PortalsNeverSpawnMitesInsideAStone()
        {
            // A Stone on the field edge, where portals are placed.
            var stones = new[]
            {
                new SimObstacle(new SimVec2(8f, 0f), 1.5f),
                new SimObstacle(new SimVec2(0f, 4.5f), 1.5f)
            };
            var mites = new[]
            {
                new MiteSpec(1, MiteReaction.TurnLeft),
                new MiteSpec(2, MiteReaction.TurnLeft),
                new MiteSpec(3, MiteReaction.TurnLeft)
            };

            for (ulong seed = 1; seed <= 100; seed++)
            {
                var config = new LevelConfig(seed, 2000, Scenario.Flame, mites, new[] { 3 }, null, stones);
                SimVec2[] starts = SpawnPlanner.Plan(config, new SimRandom(seed));
                foreach (SimVec2 p in starts)
                    foreach (SimObstacle o in stones)
                        Assert.GreaterOrEqual(Scenario.Distance(p, o.Centre), o.Reach - 0.001f, "seed " + seed);
            }
        }

        [Test]
        public void ARescuedMiteIsNeverFlungIntoAStone()
        {
            // The rescue fling (3 units straight away from the flame) would land on this Stone.
            var stones = new[] { new SimObstacle(new SimVec2(4.4f, 4.5f), 0.8f) };
            var sim = WithStones(stones, new[] { MiteReaction.TurnLeft }, new[] { new SimVec2(7.0f, 4.5f) });

            Scenario.RunUntilCaught(sim);
            sim.Step(SimInput.Barrier(sim.Mites[0].Position.X, sim.Mites[0].Position.Y));

            Assert.AreEqual(MiteState.Walking, sim.Mites[0].State, "the barrier should have rescued it");
            AssertClearOfStones(sim, "after rescue");
        }

        [Test]
        public void AnObstacleNeedsAPositiveRadius()
        {
            Assert.Throws<System.ArgumentException>(() => new SimObstacle(new SimVec2(1f, 1f), 0f));
        }
    }
}
