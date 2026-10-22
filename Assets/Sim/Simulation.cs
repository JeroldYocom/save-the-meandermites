using System;
using System.Collections.Generic;

namespace Meandermites.Sim
{
    public enum SimOutcome
    {
        Running,
        Won,   // the flame died with at least one Mite alive
        Lost   // every Mite died
    }

    /// <summary>
    /// Root of the deterministic simulation. Advances in fixed steps only;
    /// the Unity layer pumps Step() and renders what it reads back.
    ///
    /// Order within one step is fixed and load-bearing:
    ///   0. Spawn Portals that finished fading in release their Mites
    ///   1. age timers (barrier, Breathe, per-Mite counters)
    ///   2. apply the player's input
    ///   3. barrier triggers and rescues
    ///   4. move Mites
    ///   5. tick The Catch struggle, killing Mites that run out of time
    ///   6. catch Mites that reached the flame
    ///   7. flame timer and win/loss
    /// Rescue runs before the catch check so a barrier placed on a Mite at the
    /// flame's edge saves it on the same step.
    ///
    /// Not built yet: enemies, panic effects, Super Barrier, Flame Pulse,
    /// Last Gasp, terrain, wind, abilities, Whistle.
    /// </summary>
    public sealed class Simulation
    {
        /// <summary>Fixed timestep in seconds. Never read a wall clock here.</summary>
        public const float StepSeconds = SimConstants.StepSeconds;

        private readonly SimRandom _rng;
        private readonly List<MiteSim> _mites;
        private readonly List<PortalSim> _portals = new List<PortalSim>();

        public Simulation(LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Config = config;
            Seed = config.Seed;
            _rng = new SimRandom(config.Seed);

            SimVec2[] starts = SpawnPlanner.Plan(config, _rng, out SimVec2[] portalPositions);
            _mites = new List<MiteSim>(config.Mites.Length);

            // Portal levels hold every Mite inside its portal until it opens.
            // Explicit starts (tests, teaching levels) have no portals and
            // begin walking at once.
            bool hasPortals = config.ExplicitStarts == null;
            MiteState initial = hasPortals ? MiteState.Unspawned : MiteState.Walking;
            for (int i = 0; i < config.Mites.Length; i++)
                _mites.Add(new MiteSim(config.Mites[i].Id, config.Mites[i].Reaction, starts[i], initial));

            int first = 0;
            for (int k = 0; k < portalPositions.Length; k++)
            {
                int count = config.PortalCounts[k];
                _portals.Add(new PortalSim(portalPositions[k], first, count, SimConstants.PortalFadeInSteps));
                first += count;
            }

            Barrier = new BarrierSim();
            Breathe = new BreatheSim();
            Outcome = SimOutcome.Running;
        }

        /// <summary>Convenience for tests: the default level with this seed.</summary>
        public Simulation(ulong seed) : this(LevelConfig.Default(seed)) { }

        public LevelConfig Config { get; }
        public ulong Seed { get; }
        public long StepCount { get; private set; }
        public SimOutcome Outcome { get; private set; }

        public SimVec2 FlamePosition => Config.FlamePosition;
        public IReadOnlyList<MiteSim> Mites => _mites;
        public IReadOnlyList<PortalSim> Portals => _portals;
        public BarrierSim Barrier { get; }
        public BreatheSim Breathe { get; }

        /// <summary>True while the flame holds at least one caught Mite.</summary>
        public bool FlameIsHolding
        {
            get
            {
                for (int i = 0; i < _mites.Count; i++)
                    if (_mites[i].State == MiteState.Caught) return true;
                return false;
            }
        }

        /// <summary>Flame pull in units/sec, before terrain (GDD 4.3).</summary>
        public float CurrentPullStrength =>
            FlameIsHolding
                ? SimConstants.BasePullStrength * SimConstants.PullScaleWhileHolding
                : SimConstants.BasePullStrength;

        public void Step() => Step(SimInput.None);

        public void Step(SimInput input)
        {
            if (Outcome != SimOutcome.Running) return;

            // 0. portals that have finished fading in release their Mites
            for (int i = 0; i < _portals.Count; i++) ReleasePortal(_portals[i]);

            // 1. age timers
            Barrier.Tick();
            Breathe.Tick();
            for (int i = 0; i < _mites.Count; i++) TickMiteCounters(_mites[i]);

            // 2. input
            if (input.Breathe) Breathe.TryActivate();
            if (input.PlaceBarrier) Barrier.TryPlace(input.BarrierPosition);

            // 3. barrier triggers and rescues
            if (Barrier.Active)
            {
                for (int i = 0; i < _mites.Count; i++) ApplyBarrier(_mites[i]);
            }

            // 4. move
            float pull = CurrentPullStrength;
            for (int i = 0; i < _mites.Count; i++) MoveMite(_mites[i], pull);

            // 5. struggle
            for (int i = 0; i < _mites.Count; i++) TickStruggle(_mites[i]);

            // 6. catch
            float catchSqr = SimConstants.CatchRadius * SimConstants.CatchRadius;
            for (int i = 0; i < _mites.Count; i++)
            {
                MiteSim m = _mites[i];
                if ((m.State == MiteState.Walking || m.State == MiteState.Reacting) &&
                    (m.Position - FlamePosition).SqrLength <= catchSqr)
                {
                    m.State = MiteState.Caught;
                    m.CaughtSteps = 0;
                    m.ReactionStepsLeft = 0;
                }
            }

            StepCount++;

            // 7. flame timer and outcome
            if (AllDead())
            {
                Outcome = SimOutcome.Lost;
            }
            else if (StepCount >= Config.DurationSteps)
            {
                // Mites caught at the instant the flame dies are released safely.
                for (int i = 0; i < _mites.Count; i++)
                {
                    if (_mites[i].State == MiteState.Caught)
                    {
                        _mites[i].State = MiteState.Walking;
                        _mites[i].CaughtSteps = 0;
                    }
                }
                Outcome = SimOutcome.Won;
            }
        }

        private void ReleasePortal(PortalSim p)
        {
            if (p.Released || StepCount < p.ReleaseStep) return;
            p.Released = true;
            for (int j = 0; j < p.MiteCount; j++)
            {
                MiteSim m = _mites[p.FirstMite + j];
                if (m.State == MiteState.Unspawned) m.State = MiteState.Walking;
            }
        }

        private static void TickMiteCounters(MiteSim m)
        {
            if (m.ImmunityStepsLeft > 0) m.ImmunityStepsLeft--;
            if (m.InvincibleStepsLeft > 0) m.InvincibleStepsLeft--;
            if (m.State == MiteState.Reacting)
            {
                m.ReactionStepsLeft--;
                if (m.ReactionStepsLeft <= 0)
                {
                    m.ReactionStepsLeft = 0;
                    m.State = MiteState.Walking;
                }
            }
        }

        private void ApplyBarrier(MiteSim m)
        {
            if (m.State == MiteState.Caught)
            {
                if (Barrier.Covers(m.Position)) Rescue(m);
                return;
            }

            if (m.State != MiteState.Walking && m.State != MiteState.Reacting) return;
            if (m.ImmunityStepsLeft > 0) return;
            if (!Barrier.Covers(m.Position)) return;

            // The new reaction replaces any current one and restarts its timer.
            SimVec2 heading = (FlamePosition - m.Position).Normalized;
            switch (m.Reaction)
            {
                case MiteReaction.TurnLeft:
                    m.ReactionDirection = heading.Rot90Left;
                    break;
                case MiteReaction.TurnRight:
                    m.ReactionDirection = heading.Rot90Right;
                    break;
                default: // RunAway: directly away from the barrier centre
                    SimVec2 away = (m.Position - Barrier.Position).Normalized;
                    m.ReactionDirection = away.SqrLength > 0f ? away : -heading;
                    break;
            }
            m.State = MiteState.Reacting;
            m.ReactionStepsLeft = SimConstants.ReactionSteps;
            m.ImmunityStepsLeft = SimConstants.TriggerImmunitySteps;
        }

        private void Rescue(MiteSim m)
        {
            // Flung clear, directly away from the flame.
            SimVec2 outward = (m.Position - FlamePosition).Normalized;
            if (outward.SqrLength <= 0f) outward = new SimVec2(1f, 0f);
            m.Position = m.Position + outward * SimConstants.RescueFlingDistance;
            m.State = MiteState.Walking;
            m.CaughtSteps = 0;
            m.Panic = SimConstants.RescuePanic;
            m.InvincibleStepsLeft = SimConstants.RescueInvincibleSteps;
            // Landing inside the barrier must not instantly re-trigger it.
            m.ImmunityStepsLeft = SimConstants.TriggerImmunitySteps;
            m.TimesRescued++;
        }

        private void MoveMite(MiteSim m, float pull)
        {
            if (m.State != MiteState.Walking && m.State != MiteState.Reacting) return;

            SimVec2 toFlame = (FlamePosition - m.Position).Normalized;
            SimVec2 steer = m.State == MiteState.Reacting ? m.ReactionDirection : toFlame;

            // GDD 4.3 composition: steering x speed, plus flame pull, clamped.
            // (Terrain, status and wind modifiers are not built yet.)
            SimVec2 velocity = steer * SimConstants.BaseMiteSpeed + toFlame * pull;
            velocity = velocity.ClampLength(SimConstants.MaxMiteSpeed);

            m.Position = m.Position + velocity * SimConstants.StepSeconds;
        }

        private static void TickStruggle(MiteSim m)
        {
            if (m.State != MiteState.Caught) return;
            m.CaughtSteps++;
            if (m.CaughtSteps >= SimConstants.CatchSteps) m.State = MiteState.Dead;
        }

        private bool AllDead()
        {
            for (int i = 0; i < _mites.Count; i++)
                if (_mites[i].IsAlive) return false;
            return true;
        }

        /// <summary>
        /// Hash of everything that defines the sim state. Every new piece of
        /// state must be folded in here or determinism tests stop covering it.
        /// </summary>
        public ulong StateHash()
        {
            unchecked
            {
                ulong h = 1469598103934665603UL;
                h = Mix(h, _rng.State);
                h = Mix(h, (ulong)StepCount);
                h = Mix(h, (ulong)Outcome);

                for (int i = 0; i < _mites.Count; i++)
                {
                    MiteSim m = _mites[i];
                    h = Mix(h, (ulong)m.Id);
                    h = Mix(h, (ulong)m.State);
                    h = Mix(h, Bits(m.Position.X));
                    h = Mix(h, Bits(m.Position.Y));
                    h = Mix(h, Bits(m.ReactionDirection.X));
                    h = Mix(h, Bits(m.ReactionDirection.Y));
                    h = Mix(h, (ulong)m.ReactionStepsLeft);
                    h = Mix(h, (ulong)m.ImmunityStepsLeft);
                    h = Mix(h, (ulong)m.InvincibleStepsLeft);
                    h = Mix(h, (ulong)m.CaughtSteps);
                    h = Mix(h, Bits(m.Panic));
                    h = Mix(h, (ulong)m.TimesRescued);
                }

                h = Mix(h, Barrier.Active ? 1UL : 0UL);
                h = Mix(h, Bits(Barrier.Position.X));
                h = Mix(h, Bits(Barrier.Position.Y));
                h = Mix(h, (ulong)Barrier.StepsLeft);
                h = Mix(h, (ulong)Barrier.CooldownStepsLeft);
                h = Mix(h, (ulong)Breathe.ActiveStepsLeft);
                h = Mix(h, (ulong)Breathe.CooldownStepsLeft);
                for (int i = 0; i < _portals.Count; i++) h = Mix(h, _portals[i].Released ? 1UL : 0UL);
                return h;
            }
        }

        private static ulong Mix(ulong h, ulong v)
        {
            unchecked { return (h ^ v) * 1099511628211UL; }
        }

        private static ulong Bits(float f)
        {
            return (uint)BitConverter.SingleToInt32Bits(f);
        }
    }
}
