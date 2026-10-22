using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>Spawn Portals fade in, then release their Mites (decision 1.3, TC-035).</summary>
    public class PortalTests
    {
        private static Simulation PortalLevel(ulong seed, params int[] portals)
        {
            int total = 0;
            foreach (int p in portals) total += p;
            var mites = new MiteSpec[total];
            for (int i = 0; i < total; i++) mites[i] = new MiteSpec(i + 1, MiteReaction.TurnLeft);
            return new Simulation(new LevelConfig(seed, 4000, LevelConfig.FieldCentre, mites, portals));
        }

        [Test]
        public void MitesWaitInsideThePortalUntilItHasFadedIn()
        {
            var sim = PortalLevel(7, 3);
            SimVec2[] start = new SimVec2[3];
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(MiteState.Unspawned, sim.Mites[i].State);
                start[i] = sim.Mites[i].Position;
            }

            Scenario.Run(sim, SimConstants.PortalFadeInSteps);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(MiteState.Unspawned, sim.Mites[i].State, "still waiting");
                Assert.AreEqual(start[i].X, sim.Mites[i].Position.X);
                Assert.AreEqual(start[i].Y, sim.Mites[i].Position.Y);
            }
            Assert.IsFalse(sim.Portals[0].Released);

            sim.Step();

            Assert.IsTrue(sim.Portals[0].Released);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(MiteState.Walking, sim.Mites[i].State);
                Assert.Greater(Scenario.Distance(start[i], sim.Mites[i].Position), 0f,
                    "a released Mite starts walking on the same step");
            }
        }

        [Test]
        public void FadeProgressRisesFromZeroToOne()
        {
            var sim = PortalLevel(7, 3);
            Assert.AreEqual(0f, sim.Portals[0].FadeProgress(sim.StepCount), 1e-6f);
            Scenario.Run(sim, SimConstants.PortalFadeInSteps / 2);
            Assert.AreEqual(0.5f, sim.Portals[0].FadeProgress(sim.StepCount), 1e-6f);
            Scenario.Run(sim, SimConstants.PortalFadeInSteps);
            Assert.AreEqual(1f, sim.Portals[0].FadeProgress(sim.StepCount), 1e-6f);
        }

        [Test]
        public void AWaitingMiteIsUntouchedByABarrier()
        {
            var sim = PortalLevel(7, 3);
            SimVec2 p = sim.Mites[1].Position;

            sim.Step(SimInput.Barrier(p.X, p.Y));

            Assert.IsTrue(sim.Barrier.Active);
            Assert.AreEqual(MiteState.Unspawned, sim.Mites[1].State);
            Assert.AreEqual(0, sim.Mites[1].ReactionStepsLeft);
            Assert.AreEqual(0, sim.Mites[1].ImmunityStepsLeft);
        }

        [Test]
        public void WaitingMitesCountAsAliveSoTheColonyIsNotLost()
        {
            var sim = PortalLevel(7, 2, 2);
            Scenario.Run(sim, 10);
            Assert.AreEqual(SimOutcome.Running, sim.Outcome);
            for (int i = 0; i < 4; i++) Assert.IsTrue(sim.Mites[i].IsAlive);
        }

        [Test]
        public void EachPortalSitsOnTheEdgeNextToItsOwnMites()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                var sim = PortalLevel(seed, 3, 2);
                Assert.AreEqual(2, sim.Portals.Count);
                int first = 0;
                for (int k = 0; k < 2; k++)
                {
                    PortalSim portal = sim.Portals[k];
                    Assert.AreEqual(first, portal.FirstMite);
                    bool onEdge = portal.Position.X == 0f || portal.Position.Y == 0f ||
                                  portal.Position.X == SimConstants.FieldWidth ||
                                  portal.Position.Y == SimConstants.FieldHeight;
                    Assert.IsTrue(onEdge, "seed " + seed + " portal " + k);
                    for (int j = 0; j < portal.MiteCount; j++)
                    {
                        float d = Scenario.Distance(portal.Position, sim.Mites[first + j].Position);
                        Assert.Less(d, 2.5f, "seed " + seed);
                    }
                    first += portal.MiteCount;
                }
            }
        }

        [Test]
        public void AHandPlacedLevelHasNoPortalsAndWalksAtOnce()
        {
            var sim = Scenario.LeftOfFlame();
            Assert.AreEqual(0, sim.Portals.Count);
            Assert.AreEqual(MiteState.Walking, sim.Mites[0].State);
        }

        [Test]
        public void ReleaseTimeDoesNotDependOnWhatThePlayerDoes()
        {
            var calm = PortalLevel(9, 3);
            var busy = PortalLevel(9, 3);
            for (int i = 0; i < SimConstants.PortalFadeInSteps + 1; i++)
            {
                calm.Step();
                busy.Step(i % 7 == 0 ? SimInput.Barrier(8f, 4.5f) : SimInput.None);
            }
            Assert.IsTrue(calm.Portals[0].Released);
            Assert.IsTrue(busy.Portals[0].Released);
        }
    }
}
