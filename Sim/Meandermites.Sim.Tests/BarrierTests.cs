using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>GDD 4.4 and 8.1: placement, lifetime, cooldown, reactions, immunity.</summary>
    public class BarrierTests
    {
        private static Simulation AwayFromEverything()
        {
            // Mite far from the barrier spot used below; level long enough to not end.
            return Scenario.Single(MiteReaction.TurnLeft, new SimVec2(15f, 8f));
        }

        [Test]
        public void BarrierAppearsOnTheSameStepAsTheTap()
        {
            var sim = AwayFromEverything();
            sim.Step(SimInput.Barrier(2f, 2f));

            Assert.IsTrue(sim.Barrier.Active);
            Assert.AreEqual(2f, sim.Barrier.Position.X);
            Assert.AreEqual(2f, sim.Barrier.Position.Y);
        }

        [Test]
        public void SecondTapIsIgnoredWhileOneIsActive()
        {
            var sim = AwayFromEverything();
            sim.Step(SimInput.Barrier(2f, 2f));
            sim.Step(SimInput.Barrier(5f, 5f));

            Assert.AreEqual(2f, sim.Barrier.Position.X);
        }

        [Test]
        public void TapOutsideTheFieldIsRejected()
        {
            var sim = AwayFromEverything();
            sim.Step(SimInput.Barrier(-1f, 2f));
            Assert.IsFalse(sim.Barrier.Active);

            sim.Step(SimInput.Barrier(2f, 20f));
            Assert.IsFalse(sim.Barrier.Active);
        }

        [Test]
        public void BarrierLastsTwoSecondsThenCoolsDownForHalfASecond()
        {
            var sim = AwayFromEverything();
            // Tap on every step; only the accepted taps should show up.
            var active = new bool[260];
            for (int i = 0; i < active.Length; i++)
            {
                sim.Step(SimInput.Barrier(2f, 2f));
                active[i] = sim.Barrier.Active;
            }

            // Steps are 1-based in the comments: active 1..100, off 101..125, active again at 126.
            for (int step = 1; step <= 100; step++) Assert.IsTrue(active[step - 1], "step " + step);
            for (int step = 101; step <= 125; step++) Assert.IsFalse(active[step - 1], "step " + step);
            Assert.IsTrue(active[125], "step 126");
        }

        [Test]
        public void TurnLeftMiteTurnsNinetyDegreesLeftOfItsHeading()
        {
            // Heading is +x toward the flame, so left is +y.
            var sim = Scenario.LeftOfFlame(MiteReaction.TurnLeft);
            SimVec2 start = sim.Mites[0].Position;
            sim.Step(SimInput.Barrier(start.X, start.Y));

            Assert.AreEqual(MiteState.Reacting, sim.Mites[0].State);
            Assert.AreEqual(0f, sim.Mites[0].ReactionDirection.X, 1e-6f);
            Assert.AreEqual(1f, sim.Mites[0].ReactionDirection.Y, 1e-6f);

            Scenario.Run(sim, 10);
            Assert.Greater(sim.Mites[0].Position.Y, start.Y);
        }

        [Test]
        public void TurnRightMiteTurnsNinetyDegreesRight()
        {
            var sim = Scenario.LeftOfFlame(MiteReaction.TurnRight);
            SimVec2 start = sim.Mites[0].Position;
            sim.Step(SimInput.Barrier(start.X, start.Y));

            Assert.AreEqual(-1f, sim.Mites[0].ReactionDirection.Y, 1e-6f);
            Scenario.Run(sim, 10);
            Assert.Less(sim.Mites[0].Position.Y, start.Y);
        }

        [Test]
        public void RunAwayMiteFleesDirectlyFromTheBarrierCentre()
        {
            var sim = Scenario.LeftOfFlame(MiteReaction.RunAway);
            SimVec2 start = sim.Mites[0].Position;
            // Barrier centre is right of the Mite, so "away" is -x.
            sim.Step(SimInput.Barrier(start.X + 0.5f, start.Y));

            Assert.AreEqual(-1f, sim.Mites[0].ReactionDirection.X, 1e-6f);
            Scenario.Run(sim, 10);
            Assert.Less(sim.Mites[0].Position.X, start.X);
        }

        [Test]
        public void ReactionLastsExactlyTwoSeconds()
        {
            var sim = Scenario.LeftOfFlame(MiteReaction.TurnLeft);
            SimVec2 start = sim.Mites[0].Position;
            // Barrier below the Mite but covering it; the left turn carries it clear
            // of the barrier well inside the immunity window.
            sim.Step(SimInput.Barrier(start.X, start.Y - 1.0f));

            int reacting = 1;
            while (true)
            {
                sim.Step();
                if (sim.Mites[0].State != MiteState.Reacting) break;
                reacting++;
            }

            Assert.AreEqual(SimConstants.ReactionSteps, reacting);
            Assert.AreEqual(MiteState.Walking, sim.Mites[0].State);
        }

        [Test]
        public void BarrierCannotRetriggerTheSameMiteDuringTheImmunityWindow()
        {
            // Barrier centred on the Mite; a left turn takes about 39 steps to
            // leave the radius, longer than the 30-step immunity.
            var sim = Scenario.LeftOfFlame(MiteReaction.TurnLeft);
            SimVec2 start = sim.Mites[0].Position;
            sim.Step(SimInput.Barrier(start.X, start.Y));
            Assert.AreEqual(SimConstants.ReactionSteps, sim.Mites[0].ReactionStepsLeft);

            // Step 1 was the trigger. Through step 30 the timer only counts down.
            Scenario.Run(sim, SimConstants.TriggerImmunitySteps - 1);
            Assert.AreEqual(SimConstants.ReactionSteps - (SimConstants.TriggerImmunitySteps - 1),
                            sim.Mites[0].ReactionStepsLeft, "no early retrigger");

            // Immunity has expired and the Mite is still inside: a fresh reaction
            // REPLACES the old one and restarts its timer; they never stack.
            sim.Step();
            Assert.AreEqual(SimConstants.ReactionSteps, sim.Mites[0].ReactionStepsLeft);
        }

        [Test]
        public void ABarrierPlacedOnAMiteAlreadyInsideTriggersImmediately()
        {
            var sim = Scenario.LeftOfFlame();
            SimVec2 p = sim.Mites[0].Position;
            sim.Step(SimInput.Barrier(p.X + 0.3f, p.Y));
            Assert.AreEqual(MiteState.Reacting, sim.Mites[0].State);
        }
    }
}
