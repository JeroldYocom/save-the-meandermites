using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>GDD 8.4: reaching the flame is a rescuable struggle (1.5 s after the 2026-10-22 retune).</summary>
    public class CatchTests
    {
        [Test]
        public void MiteWalksTowardFlameAndIsCaughtAtTheCatchRadius()
        {
            var sim = Scenario.LeftOfFlame();
            int steps = Scenario.RunUntilCaught(sim);

            float dist = Scenario.Distance(sim.Mites[0].Position, Scenario.Flame);
            Assert.LessOrEqual(dist, SimConstants.CatchRadius);
            // 1.1 speed + 0.45 pull = 1.55 u/s over 4.1 units is about 132 steps.
            Assert.That(steps, Is.InRange(129, 135));
        }

        [Test]
        public void CaughtMiteIsHeldInPlace()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);
            SimVec2 held = sim.Mites[0].Position;

            Scenario.Run(sim, 40);

            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State);
            Assert.AreEqual(held.X, sim.Mites[0].Position.X);
            Assert.AreEqual(held.Y, sim.Mites[0].Position.Y);
        }

        [Test]
        public void MiteDiesAfterExactlyOneAndAHalfSecondsOfStruggle()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);

            int stepsCaught = 1; // the observation right after the catch
            while (true)
            {
                sim.Step();
                if (sim.Mites[0].State != MiteState.Caught) break;
                stepsCaught++;
            }

            Assert.AreEqual(MiteState.Dead, sim.Mites[0].State);
            Assert.AreEqual(SimConstants.CatchSteps, stepsCaught);
            Assert.AreEqual(1.5f, stepsCaught * SimConstants.StepSeconds, 1e-4f);
        }

        [Test]
        public void FlamePullIsHalvedWhileItHoldsAMite()
        {
            // The first Mite is caught at about step 132 while the second,
            // eight units out, is still walking.
            var sim = Scenario.Many(20000,
                new[] { MiteReaction.TurnLeft, MiteReaction.TurnLeft },
                new[] { new SimVec2(3f, 4.5f), new SimVec2(0f, 4.5f) });

            Assert.AreEqual(SimConstants.BasePullStrength, sim.CurrentPullStrength, 1e-6f);

            Scenario.RunUntilCaught(sim);
            Assert.AreEqual(MiteState.Walking, sim.Mites[1].State);
            Assert.AreEqual(
                SimConstants.BasePullStrength * SimConstants.PullScaleWhileHolding,
                sim.CurrentPullStrength, 1e-6f);

            // Measure the walking Mite's real speed: 1.1 steering + 0.225 pull.
            SimVec2 before = sim.Mites[1].Position;
            sim.Step();
            float speed = Scenario.Distance(before, sim.Mites[1].Position) / SimConstants.StepSeconds;
            Assert.AreEqual(1.1f + 0.225f, speed, 1e-3f);
        }

        [Test]
        public void PullReductionDoesNotStackWithSeveralCaughtMites()
        {
            var sim = Scenario.Many(20000,
                new[] { MiteReaction.TurnLeft, MiteReaction.TurnLeft },
                new[] { new SimVec2(3f, 4.5f), new SimVec2(13f, 4.5f) });

            Scenario.RunUntilCaught(sim);
            Assert.AreEqual(MiteState.Caught, sim.Mites[1].State);
            Assert.AreEqual(0.225f, sim.CurrentPullStrength, 1e-6f);
        }

        [Test]
        public void BarrierOnACaughtMiteRescuesIt()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);
            SimVec2 pos = sim.Mites[0].Position;

            sim.Step(SimInput.Barrier(pos.X, pos.Y));

            MiteSim m = sim.Mites[0];
            Assert.AreEqual(MiteState.Walking, m.State);
            Assert.AreEqual(1f, m.Panic);
            Assert.AreEqual(1, m.TimesRescued);
            Assert.AreEqual(SimConstants.RescueInvincibleSteps, m.InvincibleStepsLeft);
            // Landing inside the barrier must not instantly re-trigger it.
            Assert.AreEqual(SimConstants.TriggerImmunitySteps, m.ImmunityStepsLeft);

            // Flung 3 units straight away from the flame (then one step of walking back).
            float dist = Scenario.Distance(m.Position, Scenario.Flame);
            Assert.That(dist, Is.InRange(3.8f, 3.95f));
            Assert.Less(m.Position.X, pos.X);
        }

        [Test]
        public void RescueStillWorksOnTheLastStepOfTheStruggle()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);
            Scenario.Run(sim, SimConstants.CatchSteps - 1);
            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State);

            SimVec2 pos = sim.Mites[0].Position;
            sim.Step(SimInput.Barrier(pos.X, pos.Y));

            Assert.AreEqual(MiteState.Walking, sim.Mites[0].State);
        }

        [Test]
        public void RescueArrivingOneStepLateIsTooLate()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);
            SimVec2 pos = sim.Mites[0].Position;
            Scenario.Run(sim, SimConstants.CatchSteps);
            Assert.AreEqual(MiteState.Dead, sim.Mites[0].State);

            sim.Step(SimInput.Barrier(pos.X, pos.Y));
            Assert.AreEqual(MiteState.Dead, sim.Mites[0].State);
        }

        [Test]
        public void BarrierNotCoveringTheCaughtMiteDoesNotRescue()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);

            sim.Step(SimInput.Barrier(1f, 1f));

            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State);
        }

        [Test]
        public void MitesCaughtWhenTheFlameDiesAreReleased()
        {
            var sim = Scenario.LeftOfFlame(durationSteps: 200);
            Scenario.Run(sim, 199);
            Assert.AreEqual(MiteState.Caught, sim.Mites[0].State);
            Assert.AreEqual(SimOutcome.Running, sim.Outcome);

            sim.Step();

            Assert.AreEqual(SimOutcome.Won, sim.Outcome);
            Assert.AreEqual(MiteState.Walking, sim.Mites[0].State);
        }
    }
}
