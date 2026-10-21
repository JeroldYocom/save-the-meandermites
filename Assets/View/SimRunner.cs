using System.Collections.Generic;
using Meandermites.Sim;
using UnityEngine;

namespace Meandermites.View
{
    /// <summary>
    /// Owns one Simulation and pumps it at a fixed step. It reads Breathe
    /// state from the Sim to decide how many steps to run per real second; it
    /// never touches Time.timeScale, so replays stay exact.
    /// Input is latched between frames and handed to the next step, and every
    /// step's input is recorded so a run can be re-simulated and compared.
    /// </summary>
    public sealed class SimRunner : MonoBehaviour
    {
        // If the app stalls, drop the backlog rather than fast-forwarding.
        private const float MaxBacklogSeconds = 0.25f;

        private float _accumulator;
        private bool _pendingBarrier;
        private SimVec2 _pendingBarrierPosition;
        private bool _pendingBreathe;

        public Simulation Sim { get; private set; }
        public LevelConfig Config { get; private set; }
        public List<SimInput> Recording { get; } = new List<SimInput>();

        /// <summary>Result of the last replay check, for the overlay.</summary>
        public string ReplayMessage { get; private set; } = "";

        public void Begin(LevelConfig config)
        {
            Config = config;
            Sim = new Simulation(config);
            Recording.Clear();
            _accumulator = 0f;
            _pendingBarrier = false;
            _pendingBreathe = false;
            ReplayMessage = "";
        }

        public void QueueBarrier(SimVec2 position)
        {
            _pendingBarrier = true;
            _pendingBarrierPosition = position;
        }

        public void QueueBreathe()
        {
            _pendingBreathe = true;
        }

        /// <summary>
        /// Advance by real elapsed time. Returns how many Sim steps ran.
        /// Each step costs StepSeconds / StepRateScale of real time, so Breathe
        /// makes the Sim run slower without changing what it computes.
        /// </summary>
        public int Pump(float realDeltaSeconds)
        {
            if (Sim == null || Sim.Outcome != SimOutcome.Running) return 0;

            _accumulator += realDeltaSeconds;
            if (_accumulator > MaxBacklogSeconds) _accumulator = MaxBacklogSeconds;

            int steps = 0;
            while (Sim.Outcome == SimOutcome.Running)
            {
                float cost = SimConstants.StepSeconds / Sim.Breathe.StepRateScale;
                if (_accumulator < cost) break;

                var input = new SimInput(_pendingBarrier, _pendingBarrierPosition, _pendingBreathe);
                _pendingBarrier = false;
                _pendingBreathe = false;

                Sim.Step(input);
                Recording.Add(input);
                _accumulator -= cost;
                steps++;
            }
            return steps;
        }

        /// <summary>
        /// Re-simulate the whole run from its config and recorded inputs and
        /// compare the result with the live Sim. A mismatch means something
        /// non-deterministic got into the Sim.
        /// </summary>
        public bool VerifyReplay()
        {
            var shadow = new Simulation(Config);
            for (int i = 0; i < Recording.Count; i++) shadow.Step(Recording[i]);

            bool ok = shadow.StepCount == Sim.StepCount && shadow.StateHash() == Sim.StateHash();
            ReplayMessage = ok
                ? "REPLAY MATCH: " + Recording.Count + " steps, hash " + Sim.StateHash().ToString("X16")
                : "REPLAY MISMATCH: live " + Sim.StateHash().ToString("X16") +
                  " vs replay " + shadow.StateHash().ToString("X16");
            return ok;
        }
    }
}
