using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>Decision 1.4: Breathe is Sim state. Plus win/loss and the step-constant sanity checks.</summary>
    public class BreatheAndOutcomeTests
    {
        [Test]
        public void StepDurationsMatchTheirSecondsInTheGdd()
        {
            Assert.AreEqual(SimConstants.CatchSteps, SimConstants.Steps(2.0f));
            Assert.AreEqual(SimConstants.ReactionSteps, SimConstants.Steps(2.0f));
            Assert.AreEqual(SimConstants.TriggerImmunitySteps, SimConstants.Steps(0.6f));
            Assert.AreEqual(SimConstants.BarrierDurationSteps, SimConstants.Steps(3.0f));
            Assert.AreEqual(SimConstants.BarrierCooldownSteps, SimConstants.Steps(1.0f));
            Assert.AreEqual(SimConstants.RescueInvincibleSteps, SimConstants.Steps(1.0f));
            Assert.AreEqual(SimConstants.BreatheCooldownSteps, SimConstants.Steps(20.0f));
        }

        [Test]
        public void BreatheActivatesAsARecordedInputAndSlowsTheStepRate()
        {
            var sim = Scenario.LeftOfFlame();
            Assert.AreEqual(1f, sim.Breathe.StepRateScale);

            sim.Step(SimInput.BreatheNow);

            Assert.IsTrue(sim.Breathe.Active);
            Assert.AreEqual(0.25f, sim.Breathe.StepRateScale);
        }

        [Test]
        public void BreatheEndsAfterItsActiveSteps()
        {
            var sim = Scenario.LeftOfFlame();
            sim.Step(SimInput.BreatheNow);
            // Active on steps 1..25 inclusive, so the 26th step is the first inactive one.
            Scenario.Run(sim, SimConstants.BreatheActiveSteps - 1);
            Assert.IsTrue(sim.Breathe.Active);
            sim.Step();

            Assert.IsFalse(sim.Breathe.Active);
            Assert.AreEqual(1f, sim.Breathe.StepRateScale);
        }

        [Test]
        public void BreatheCooldownBlocksReactivationUntilItExpires()
        {
            var sim = Scenario.Remote();
            sim.Step(SimInput.BreatheNow);
            Scenario.Run(sim, SimConstants.BreatheActiveSteps);
            Assert.IsFalse(sim.Breathe.CanActivate);

            sim.Step(SimInput.BreatheNow);
            Assert.IsFalse(sim.Breathe.Active, "ignored during cooldown");

            // Cooldown started on step 1 and counts in sim steps.
            Scenario.Run(sim, SimConstants.BreatheCooldownSteps);
            Assert.IsTrue(sim.Breathe.CanActivate);
            sim.Step(SimInput.BreatheNow);
            Assert.IsTrue(sim.Breathe.Active);
        }

        [Test]
        public void BreatheChangesNoGameplayItOnlyChangesThePumpRate()
        {
            var plain = Scenario.LeftOfFlame();
            var breathing = Scenario.LeftOfFlame();
            plain.Step();
            breathing.Step(SimInput.BreatheNow);
            for (int i = 0; i < 60; i++)
            {
                plain.Step();
                breathing.Step();
            }
            Assert.AreEqual(plain.Mites[0].Position.X, breathing.Mites[0].Position.X);
            Assert.AreEqual(plain.Mites[0].Position.Y, breathing.Mites[0].Position.Y);
            // But the replay record differs, so the hashes must differ.
            Assert.AreNotEqual(plain.StateHash(), breathing.StateHash());
        }

        [Test]
        public void SurvivingUntilTheFlameDiesIsAWin()
        {
            var sim = Scenario.Single(MiteReaction.TurnLeft, new SimVec2(0f, 0f), durationSteps: 50);
            Scenario.Run(sim, 49);
            Assert.AreEqual(SimOutcome.Running, sim.Outcome);
            sim.Step();
            Assert.AreEqual(SimOutcome.Won, sim.Outcome);
        }

        [Test]
        public void LosingEveryMiteIsALossAtTheMomentOfTheLastDeath()
        {
            var sim = Scenario.LeftOfFlame();
            Scenario.RunUntilCaught(sim);
            Scenario.Run(sim, SimConstants.CatchSteps);

            Assert.AreEqual(MiteState.Dead, sim.Mites[0].State);
            Assert.AreEqual(SimOutcome.Lost, sim.Outcome);
        }

        [Test]
        public void OneSurvivorKeepsTheLevelRunning()
        {
            var sim = Scenario.Many(20000,
                new[] { MiteReaction.TurnLeft, MiteReaction.TurnLeft },
                new[] { new SimVec2(3f, 4.5f), new SimVec2(0f, 0f) });
            Scenario.RunUntilCaught(sim);
            Scenario.Run(sim, SimConstants.CatchSteps);

            Assert.AreEqual(MiteState.Dead, sim.Mites[0].State);
            Assert.AreEqual(SimOutcome.Running, sim.Outcome);
        }

        [Test]
        public void NothingChangesAfterTheLevelEnds()
        {
            var sim = Scenario.Single(MiteReaction.TurnLeft, new SimVec2(0f, 0f), durationSteps: 10);
            Scenario.Run(sim, 10);
            ulong hash = sim.StateHash();
            long steps = sim.StepCount;

            Scenario.Run(sim, 25);

            Assert.AreEqual(hash, sim.StateHash());
            Assert.AreEqual(steps, sim.StepCount);
        }
    }
}
