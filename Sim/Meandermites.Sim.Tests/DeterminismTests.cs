using System.Collections.Generic;
using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    /// <summary>
    /// The Daily Challenge and Replay depend on these. Each "same" test is
    /// paired with a "different" test, so a StateHash() that returned a
    /// constant cannot make the whole file pass while testing nothing.
    /// </summary>
    public class DeterminismTests
    {
        private static LevelConfig LongLevel(ulong seed, int mites = 5)
        {
            var specs = new MiteSpec[mites];
            for (int i = 0; i < mites; i++)
                specs[i] = new MiteSpec(i + 1, (MiteReaction)(i % 3));
            return new LevelConfig(seed, 20000, LevelConfig.FieldCentre, specs, new[] { mites });
        }

        private static Simulation RunHeadless(ulong seed, int steps)
        {
            var sim = new Simulation(seed);
            for (int i = 0; i < steps; i++) sim.Step();
            return sim;
        }

        // A reproducible stream of plausible player input, derived from its own
        // seed so the same inputSeed always yields the same recording.
        private static List<SimInput> RecordInputs(ulong inputSeed, int steps)
        {
            var rng = new SimRandom(inputSeed);
            var list = new List<SimInput>(steps);
            for (int i = 0; i < steps; i++)
            {
                if (rng.NextInt(40) == 0)
                {
                    float x = rng.NextInt(161) * 0.1f;
                    float y = rng.NextInt(91) * 0.1f;
                    list.Add(SimInput.Barrier(x, y));
                }
                else if (rng.NextInt(300) == 0)
                {
                    list.Add(SimInput.BreatheNow);
                }
                else
                {
                    list.Add(SimInput.None);
                }
            }
            return list;
        }

        private static Simulation Replay(LevelConfig config, List<SimInput> inputs)
        {
            var sim = new Simulation(config);
            for (int i = 0; i < inputs.Count; i++) sim.Step(inputs[i]);
            return sim;
        }

        [Test]
        public void SameSeedProducesIdenticalState()
        {
            var a = RunHeadless(12345, 10_000);
            var b = RunHeadless(12345, 10_000);
            Assert.AreEqual(a.StateHash(), b.StateHash());
        }

        [Test]
        public void DifferentSeedsDiverge()
        {
            Assert.AreNotEqual(
                RunHeadless(1, 500).StateHash(),
                RunHeadless(2, 500).StateHash());
        }

        [Test]
        public void StateHashChangesAsStepsAdvance()
        {
            Assert.AreNotEqual(
                RunHeadless(7, 10).StateHash(),
                RunHeadless(7, 11).StateHash());
        }

        [Test]
        public void ZeroSeedIsUsable()
        {
            var r = new SimRandom(0);
            Assert.AreNotEqual(0UL, r.NextUInt64());
        }

        [Test]
        public void ReplayingTheRecordedInputsReproducesTheRunExactly()
        {
            for (ulong seed = 1; seed <= 25; seed++)
            {
                var config = LongLevel(seed);
                var inputs = RecordInputs(seed * 7919UL, 6000);

                var first = Replay(config, inputs);
                var second = Replay(config, inputs);

                Assert.AreEqual(first.StateHash(), second.StateHash(), "seed " + seed);
                Assert.AreEqual(first.Outcome, second.Outcome, "seed " + seed);
            }
        }

        [Test]
        public void DifferentInputsOnTheSameSeedDiverge()
        {
            var config = LongLevel(99);
            var withTaps = RecordInputs(1, 3000);
            var noInput = new List<SimInput>();
            for (int i = 0; i < 3000; i++) noInput.Add(SimInput.None);

            Assert.AreNotEqual(Replay(config, withTaps).StateHash(), Replay(config, noInput).StateHash());
        }

        [Test]
        public void TheSameTapOnALaterStepLeavesADifferentBarrierTimer()
        {
            var config = LongLevel(5);
            var early = new List<SimInput>();
            var late = new List<SimInput>();
            for (int i = 0; i < 50; i++)
            {
                early.Add(i == 10 ? SimInput.Barrier(8f, 4.5f) : SimInput.None);
                late.Add(i == 11 ? SimInput.Barrier(8f, 4.5f) : SimInput.None);
            }

            Assert.AreNotEqual(Replay(config, early).StateHash(), Replay(config, late).StateHash());
        }

        [Test]
        public void EveryStateChangeShowsUpInTheHash()
        {
            // Walk a run and require the hash to change on every step while the
            // level is running: if some piece of state moved without being hashed,
            // a step where only that state changed would repeat a hash.
            var sim = new Simulation(LongLevel(3));
            var seen = new HashSet<ulong>();
            for (int i = 0; i < 2000 && sim.Outcome == SimOutcome.Running; i++)
            {
                sim.Step(i % 97 == 0 ? SimInput.Barrier(8f, 4.5f) : SimInput.None);
                Assert.IsTrue(seen.Add(sim.StateHash()), "hash repeated at step " + i);
            }
        }
    }
}
