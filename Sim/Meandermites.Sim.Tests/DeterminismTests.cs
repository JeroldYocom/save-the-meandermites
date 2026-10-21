using Meandermites.Sim;
using NUnit.Framework;

namespace Meandermites.Sim.Tests
{
    public class DeterminismTests
    {
        private static Simulation RunHeadless(ulong seed, int steps)
        {
            var sim = new Simulation(seed);
            for (int i = 0; i < steps; i++) sim.Step();
            return sim;
        }

        [Test]
        public void SameSeedProducesIdenticalState()
        {
            var a = RunHeadless(12345, 10_000);
            var b = RunHeadless(12345, 10_000);
            Assert.AreEqual(a.StateHash(), b.StateHash());
        }

        // Without this, a StateHash() that returned a constant would make the
        // test above pass forever while checking nothing.
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
    }
}
