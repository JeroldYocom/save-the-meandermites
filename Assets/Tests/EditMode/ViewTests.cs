using System.Collections.Generic;
using Meandermites.Sim;
using Meandermites.View;
using NUnit.Framework;
using UnityEngine;

namespace Meandermites.Tests.EditMode
{
    /// <summary>
    /// Runs the Unity layer without a scene or a player, so Claude can check
    /// that the view code builds, pumps and draws without exceptions. It
    /// cannot judge how anything looks or feels; that is still the Editor's job.
    /// </summary>
    public class ViewTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _made) if (go != null) Object.DestroyImmediate(go);
            _made.Clear();
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _made.Add(go);
            return go;
        }

        private SimRunner NewRunner()
        {
            var runner = NewObject("runner").AddComponent<SimRunner>();
            runner.Begin(Presets.Build(1, 7));
            return runner;
        }

        [Test]
        public void PumpRunsOneStepPerFixedInterval()
        {
            var runner = NewRunner();
            int total = 0;
            for (int i = 0; i < 50; i++) total += runner.Pump(SimConstants.StepSeconds);

            Assert.AreEqual(50, total);
            Assert.AreEqual(50, runner.Sim.StepCount);
        }

        [Test]
        public void PumpDoesNotRunStepsEarly()
        {
            var runner = NewRunner();
            Assert.AreEqual(0, runner.Pump(0.01f));
            Assert.AreEqual(0, runner.Sim.StepCount);
        }

        [Test]
        public void BreatheMakesTheSimRunAtAQuarterRate()
        {
            var runner = NewRunner();
            runner.QueueBreathe();
            Assert.AreEqual(1, runner.Pump(SimConstants.StepSeconds));
            Assert.IsTrue(runner.Sim.Breathe.Active);

            // Under Breathe one step costs four times as much real time.
            Assert.AreEqual(0, runner.Pump(0.04f));
            Assert.AreEqual(1, runner.Pump(0.04f));
        }

        [Test]
        public void BigStallsAreCappedInsteadOfFastForwarding()
        {
            var runner = NewRunner();
            int steps = runner.Pump(30f);
            Assert.LessOrEqual(steps, 13);
            Assert.Greater(steps, 0);
        }

        [Test]
        public void QueuedBarrierReachesTheSimOnTheNextStepAndIsRecorded()
        {
            var runner = NewRunner();
            runner.QueueBarrier(new SimVec2(4f, 4f));
            runner.Pump(SimConstants.StepSeconds);

            Assert.IsTrue(runner.Sim.Barrier.Active);
            Assert.IsTrue(runner.Recording[0].PlaceBarrier);
        }

        [Test]
        public void ReplayOfARecordedRunMatchesTheLiveRun()
        {
            var runner = NewRunner();
            for (int i = 0; i < 400; i++)
            {
                if (i == 30) runner.QueueBarrier(new SimVec2(8f, 4.5f));
                if (i == 90) runner.QueueBreathe();
                runner.Pump(SimConstants.StepSeconds);
            }

            Assert.IsTrue(runner.VerifyReplay(), runner.ReplayMessage);
            StringAssert.StartsWith("REPLAY MATCH", runner.ReplayMessage);
        }

        [Test]
        public void RendererBuildsOneBodyAndRingPerMitePlusTheScenery()
        {
            var runner = NewRunner();
            var renderer = NewObject("renderer").AddComponent<SimRenderer>();
            renderer.Build(runner.Sim);

            // field, portals holder, barrier disc + ring, flame, catch ring, and 2 per Mite.
            Assert.AreEqual(6 + 2 * runner.Sim.Mites.Count, renderer.ObjectCount);
        }

        [Test]
        public void RendererFollowsTheSimAndSurvivesAFullRun()
        {
            var runner = NewRunner();
            var renderer = NewObject("renderer").AddComponent<SimRenderer>();
            renderer.Build(runner.Sim);

            for (int i = 0; i < 3000 && runner.Sim.Outcome == SimOutcome.Running; i++)
            {
                if (i % 120 == 5) runner.QueueBarrier(new SimVec2(8f, 4.5f));
                runner.Pump(SimConstants.StepSeconds);
                renderer.Refresh(runner.Sim);
            }

            Assert.AreNotEqual(SimOutcome.Running, runner.Sim.Outcome);
        }

        [Test]
        public void RebuildingReplacesTheOldViewInsteadOfStackingIt()
        {
            var runner = NewRunner();
            var renderer = NewObject("renderer").AddComponent<SimRenderer>();
            renderer.Build(runner.Sim);
            int first = renderer.ObjectCount;

            runner.Begin(Presets.Build(3, 9));
            renderer.Build(runner.Sim);

            Assert.AreEqual(6 + 2 * runner.Sim.Mites.Count, renderer.ObjectCount);
            Assert.AreNotEqual(first, renderer.ObjectCount, "preset 3 has a different Mite count");
        }

        [Test]
        public void EveryPresetBuildsAndRuns()
        {
            for (int p = 1; p <= Presets.Count; p++)
            {
                var runner = NewObject("r" + p).AddComponent<SimRunner>();
                runner.Begin(Presets.Build(p, 3));
                for (int i = 0; i < 200; i++) runner.Pump(SimConstants.StepSeconds);
                Assert.Greater(runner.Sim.StepCount, 0, "preset " + p);
            }
        }

        [Test]
        public void BootstrapBuildsTheWholePrototype()
        {
            bool hadCamera = Camera.main != null;
            var game = NewObject("game").AddComponent<GameBootstrap>();
            game.Init();

            Assert.IsNotNull(game.Runner);
            Assert.IsNotNull(game.Runner.Sim);
            Assert.IsNotNull(game.GameCamera);
            Assert.IsTrue(game.GameCamera.orthographic);
            Assert.Greater(game.SimView.ObjectCount, 0);

            // The camera must show the whole 16x9 field.
            float halfWidth = game.GameCamera.orthographicSize * game.GameCamera.aspect;
            Assert.GreaterOrEqual(game.GameCamera.orthographicSize, SimConstants.FieldHeight * 0.5f);
            Assert.GreaterOrEqual(halfWidth + 1e-3f, SimConstants.FieldWidth * 0.5f);

            if (!hadCamera) Object.DestroyImmediate(game.GameCamera.gameObject);
        }
    }
}
