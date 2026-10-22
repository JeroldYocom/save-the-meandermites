using System.Collections.Generic;
using Meandermites.Sim;
using UnityEngine;

namespace Meandermites.View
{
    /// <summary>
    /// Draws a Simulation with placeholder shapes. Read-only: it never
    /// changes Sim state, it only mirrors it. Rebuild with Build() whenever a
    /// new Simulation starts.
    /// </summary>
    public sealed class SimRenderer : MonoBehaviour
    {
        private sealed class MiteView
        {
            public SpriteRenderer Body;
            public SpriteRenderer Ring;
        }

        private static readonly Color Lavender = new Color(0.86f, 0.82f, 0.95f);
        private static readonly Color FlameColour = new Color(1f, 0.55f, 0.15f);
        private static readonly Color BarrierColour = new Color(0.3f, 0.85f, 0.95f);

        private Transform _root;
        private SpriteRenderer _flameGlow;
        private SpriteRenderer _catchRing;
        private SpriteRenderer _barrierDisc;
        private SpriteRenderer _barrierRing;
        private readonly List<MiteView> _mites = new List<MiteView>();
        private readonly List<PortalArt> _portals = new List<PortalArt>();
        private int _portalStyleMode; // 0 = a different look per portal, 1..4 = every portal the same

        public int ObjectCount => _root == null ? 0 : _root.childCount;

        /// <summary>"Mixed", or the name of the one style every portal uses.</summary>
        public string PortalStyleName => _portalStyleMode == 0 ? "Mixed" : PortalArt.StyleName((PortalStyle)(_portalStyleMode - 1));

        /// <summary>Cycle Mixed, Ring, Swirl, Rift, Bloom. Call Build again to see it.</summary>
        public void CyclePortalStyle() => _portalStyleMode = (_portalStyleMode + 1) % (PortalArt.StyleCount + 1);

        public void Build(Simulation sim)
        {
            if (_root != null) DestroyNow(_root.gameObject);
            _mites.Clear();
            _portals.Clear();

            _root = new GameObject("SimView").transform;
            _root.SetParent(transform, false);

            var field = MakeSprite("Field", ViewSprites.Square, Lavender, -10);
            field.transform.position = new Vector3(SimConstants.FieldWidth * 0.5f, SimConstants.FieldHeight * 0.5f, 0f);
            field.transform.localScale = new Vector3(SimConstants.FieldWidth, SimConstants.FieldHeight, 1f);

            var portalHolder = new GameObject("Portals").transform;
            portalHolder.SetParent(_root, false);
            for (int k = 0; k < sim.Portals.Count; k++)
            {
                PortalStyle style = _portalStyleMode == 0
                    ? (PortalStyle)(k % PortalArt.StyleCount)
                    : (PortalStyle)(_portalStyleMode - 1);
                _portals.Add(new PortalArt(portalHolder, style, sim.Portals[k].Position, k));
            }

            _barrierDisc = MakeSprite("BarrierDisc", ViewSprites.Disc, BarrierColour, 1);
            _barrierRing = MakeSprite("BarrierRing", ViewSprites.Ring, BarrierColour, 1);
            _flameGlow = MakeSprite("Flame", ViewSprites.Disc, FlameColour, 2);
            _catchRing = MakeSprite("CatchRing", ViewSprites.Ring, new Color(1f, 0.2f, 0.1f, 0.6f), 2);

            _flameGlow.transform.position = ToWorld(sim.FlamePosition);
            _flameGlow.transform.localScale = Vector3.one * 2.4f;
            _catchRing.transform.position = ToWorld(sim.FlamePosition);
            _catchRing.transform.localScale = Vector3.one * (SimConstants.CatchRadius * 2f);

            for (int i = 0; i < sim.Mites.Count; i++)
            {
                var view = new MiteView
                {
                    Body = MakeSprite("Mite" + sim.Mites[i].Id, ViewSprites.Disc, BaseColour(sim.Mites[i].Reaction), 3),
                    Ring = MakeSprite("MiteRing" + sim.Mites[i].Id, ViewSprites.Ring, Color.white, 4)
                };
                _mites.Add(view);
            }

            Refresh(sim);
        }

        public void Refresh(Simulation sim)
        {
            if (_root == null) return;

            // Flame: brighter and pulsing while it holds a Mite.
            bool holding = sim.FlameIsHolding;
            float pulse = holding ? 0.5f + 0.5f * Mathf.Sin(sim.StepCount * 0.25f) : 0f;
            _flameGlow.color = Color.Lerp(FlameColour, new Color(1f, 0.9f, 0.4f), pulse);

            // Barrier: fades as its lifetime runs out.
            bool barrier = sim.Barrier.Active;
            _barrierDisc.enabled = barrier;
            _barrierRing.enabled = barrier;
            if (barrier)
            {
                float life = (float)sim.Barrier.StepsLeft / SimConstants.BarrierDurationSteps;
                Vector3 pos = ToWorld(sim.Barrier.Position);
                float d = SimConstants.BarrierRadius * 2f;
                _barrierDisc.transform.position = pos;
                _barrierDisc.transform.localScale = Vector3.one * d;
                _barrierDisc.color = new Color(BarrierColour.r, BarrierColour.g, BarrierColour.b, 0.18f + 0.22f * life);
                _barrierRing.transform.position = pos;
                _barrierRing.transform.localScale = Vector3.one * d;
                _barrierRing.color = new Color(BarrierColour.r, BarrierColour.g, BarrierColour.b, 0.5f + 0.4f * life);
            }

            for (int k = 0; k < _portals.Count && k < sim.Portals.Count; k++)
                _portals[k].Refresh(sim.Portals[k], sim.StepCount);

            for (int i = 0; i < _mites.Count && i < sim.Mites.Count; i++)
                RefreshMite(_mites[i], sim.Mites[i], sim.StepCount);
        }

        private static void RefreshMite(MiteView view, MiteSim mite, long step)
        {
            // Still inside a portal that has not opened: nothing to draw.
            bool waiting = mite.State == MiteState.Unspawned;
            view.Body.enabled = !waiting;
            view.Ring.enabled = !waiting;
            if (waiting) return;

            Vector3 pos = ToWorld(mite.Position);
            view.Body.transform.position = pos;
            view.Ring.transform.position = pos;

            Color baseColour = BaseColour(mite.Reaction);
            float diameter = SimConstants.MiteRadius * 2f;
            float ringDiameter = diameter * 1.5f;
            Color ringColour = new Color(1f, 1f, 1f, 0f);
            Color body = Color.Lerp(baseColour, new Color(0.9f, 0.9f, 0.95f), mite.Panic * 0.55f);

            switch (mite.State)
            {
                case MiteState.Reacting:
                    diameter *= 1.15f;
                    break;

                case MiteState.Caught:
                    // The ring shrinks as the struggle runs out.
                    float progress = (float)mite.CaughtSteps / SimConstants.CatchSteps;
                    float flash = 0.5f + 0.5f * Mathf.Sin(step * 0.6f);
                    body = Color.Lerp(body, new Color(1f, 0.15f, 0.1f), 0.4f + 0.5f * flash);
                    ringDiameter = diameter * Mathf.Lerp(3.2f, 1.2f, progress);
                    ringColour = new Color(1f, 0.2f, 0.1f, 0.9f);
                    break;

                case MiteState.Dead:
                    body = new Color(0.25f, 0.25f, 0.3f, 0.45f);
                    diameter *= 0.7f;
                    break;
            }

            if (mite.State != MiteState.Dead && mite.InvincibleStepsLeft > 0)
                ringColour = new Color(1f, 1f, 1f, 0.9f);

            view.Body.color = body;
            view.Body.transform.localScale = Vector3.one * diameter;
            view.Ring.color = ringColour;
            view.Ring.transform.localScale = Vector3.one * ringDiameter;
        }

        private SpriteRenderer MakeSprite(string objectName, Sprite sprite, Color colour, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = ViewSprites.SpriteMaterial;
            sr.color = colour;
            sr.sortingOrder = order;
            return sr;
        }

        private static Color BaseColour(MiteReaction reaction)
        {
            switch (reaction)
            {
                case MiteReaction.TurnLeft: return new Color(0.35f, 0.55f, 1f);    // Leftythe
                case MiteReaction.TurnRight: return new Color(0.35f, 0.8f, 0.45f); // Riggimite
                default: return new Color(1f, 0.6f, 0.25f);                        // Scattermite
            }
        }

        public static Vector3 ToWorld(SimVec2 v) => new Vector3(v.X, v.Y, 0f);

        private static void DestroyNow(GameObject go)
        {
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }
}
