using System;
using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>Decision 1.3: portals on the edge, evenly spread, seeded, away from the flame.</summary>
    public class SpawnTests
    {
        private static LevelConfig Config(ulong seed, params int[] portals)
        {
            int total = 0;
            foreach (int p in portals) total += p;
            var mites = new MiteSpec[total];
            for (int i = 0; i < total; i++) mites[i] = new MiteSpec(i + 1, MiteReaction.TurnLeft);
            return new LevelConfig(seed, 2000, LevelConfig.FieldCentre, mites, portals);
        }

        private static bool OnEdge(SimVec2 p)
        {
            return p.X == 0f || p.X == SimConstants.FieldWidth ||
                   p.Y == 0f || p.Y == SimConstants.FieldHeight;
        }

        [Test]
        public void EverySpawnIsOnTheEdgeAndFarEnoughFromTheFlame()
        {
            for (ulong seed = 1; seed <= 300; seed++)
            {
                foreach (var cfg in new[] { Config(seed, 3), Config(seed, 5), Config(seed, 2, 2), Config(seed, 1, 1, 1) })
                {
                    var sim = new Simulation(cfg);
                    foreach (var m in sim.Mites)
                    {
                        Assert.IsTrue(OnEdge(m.Position), "seed " + seed + " mite " + m.Id + " at " + m.Position);
                        float d = Scenario.Distance(m.Position, sim.FlamePosition);
                        Assert.GreaterOrEqual(d, SimConstants.MinSpawnDistanceFromFlame - 1e-4f,
                            "seed " + seed + " mite " + m.Id);
                    }
                }
            }
        }

        [Test]
        public void SameSeedGivesTheSameLayout()
        {
            var a = new Simulation(Config(42, 2, 3));
            var b = new Simulation(Config(42, 2, 3));
            for (int i = 0; i < a.Mites.Count; i++)
            {
                Assert.AreEqual(a.Mites[i].Position.X, b.Mites[i].Position.X);
                Assert.AreEqual(a.Mites[i].Position.Y, b.Mites[i].Position.Y);
            }
        }

        [Test]
        public void DifferentSeedsGiveDifferentLayouts()
        {
            var distinct = new System.Collections.Generic.List<string>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var p = new Simulation(Config(seed, 3)).Mites[0].Position;
                string key = p.X + "," + p.Y;
                if (!distinct.Contains(key)) distinct.Add(key);
            }
            Assert.Greater(distinct.Count, 5, "layout must actually depend on the seed");
        }

        [Test]
        public void TwoPortalsAreSpreadApartNotClustered()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var sim = new Simulation(Config(seed, 1, 1));
                float apart = Scenario.Distance(sim.Mites[0].Position, sim.Mites[1].Position);
                Assert.GreaterOrEqual(apart, 6f, "seed " + seed);
            }
        }

        [Test]
        public void MitesFromOneBurstDoNotOverlap()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var sim = new Simulation(Config(seed, 5));
                for (int i = 0; i < sim.Mites.Count; i++)
                    for (int j = i + 1; j < sim.Mites.Count; j++)
                        Assert.Greater(Scenario.Distance(sim.Mites[i].Position, sim.Mites[j].Position),
                                       0.5f, "seed " + seed);
            }
        }

        [Test]
        public void BadConfigsAreRejectedAtSetupTime()
        {
            var mites = new[] { new MiteSpec(1, MiteReaction.TurnLeft), new MiteSpec(2, MiteReaction.TurnLeft) };
            Assert.Throws<ArgumentException>(() =>
                new LevelConfig(1, 100, LevelConfig.FieldCentre, mites, new[] { 3 }));
            Assert.Throws<ArgumentException>(() =>
                new LevelConfig(1, 100, LevelConfig.FieldCentre, mites, new[] { 2, 0 }));
            Assert.Throws<ArgumentException>(() =>
                new LevelConfig(1, 100, LevelConfig.FieldCentre, new MiteSpec[0], new[] { 1 }));
            Assert.Throws<ArgumentException>(() =>
                new LevelConfig(1, 0, LevelConfig.FieldCentre, mites, new[] { 2 }));
        }
    }
}
