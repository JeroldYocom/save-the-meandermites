using System.Text;
using Meandermites.Sim;
using UnityEngine;

namespace Meandermites.View
{
    /// <summary>
    /// Screen-space HUD and debug overlay, drawn with immediate-mode GUI so it
    /// needs no UI assets. Controls follow the GDD: everything interactive
    /// sits in the bottom third. Taps are hit-tested by hand in GameBootstrap
    /// (this class only draws), so IMGUI event handling is never involved.
    /// </summary>
    public sealed class HudOverlay : MonoBehaviour
    {
        private SimRunner _runner;
        private Camera _camera;
        private string _presetName = "";
        private GUIStyle _text;
        private GUIStyle _big;
        private GUIStyle _small;

        public void Initialise(SimRunner runner, Camera camera)
        {
            _runner = runner;
            _camera = camera;
        }

        public void SetPresetName(string presetName) => _presetName = presetName;

        /// <summary>Breathe button in screen pixels, origin bottom-left (input space).</summary>
        public static Rect BreatheButton()
        {
            float h = Mathf.Max(70f, Screen.height * 0.11f);
            float w = h * 2.2f;
            float margin = Mathf.Max(20f, Screen.height * 0.03f);
            return new Rect(margin, margin, w, h);
        }

        private static Rect ToGui(Rect r) => new Rect(r.x, Screen.height - r.y - r.height, r.width, r.height);

        private void EnsureStyles()
        {
            if (_text != null) return;
            int basis = Mathf.Max(14, Screen.height / 40);
            _text = new GUIStyle(GUI.skin.label) { fontSize = basis, richText = true };
            _text.normal.textColor = Color.white;
            _big = new GUIStyle(_text) { fontSize = basis * 3, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_text) { fontSize = Mathf.Max(11, basis * 2 / 3) };
        }

        private void OnGUI()
        {
            if (_runner == null || _runner.Sim == null) return;
            if (Event.current.type != EventType.Repaint) return;

            // Styles depend on the screen size; rebuild if it changed.
            if (_text != null && _text.fontSize != Mathf.Max(14, Screen.height / 40)) _text = null;
            EnsureStyles();

            Simulation sim = _runner.Sim;
            DrawTopBar(sim);
            DrawCaughtTimers(sim);
            DrawBreatheButton(sim);
            DrawBarrierStatus(sim);
            DrawDebug(sim);
            DrawOutcome(sim);
        }

        private void DrawTopBar(Simulation sim)
        {
            int alive = 0;
            for (int i = 0; i < sim.Mites.Count; i++) if (sim.Mites[i].IsAlive) alive++;
            float remaining = Mathf.Max(0f, (_runner.Config.DurationSteps - sim.StepCount) * SimConstants.StepSeconds);

            GUI.Label(new Rect(16, 8, Screen.width * 0.4f, _text.fontSize * 2f),
                "Mites " + alive + "/" + sim.Mites.Count, _text);
            var centre = new GUIStyle(_text) { alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(0, 8, Screen.width, _text.fontSize * 2f), "Flame " + remaining.ToString("0.0") + "s", centre);
        }

        // "1.4s" over each Mite the flame is holding: the thing to rescue.
        private void DrawCaughtTimers(Simulation sim)
        {
            if (_camera == null) return;
            var centre = new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            for (int i = 0; i < sim.Mites.Count; i++)
            {
                MiteSim m = sim.Mites[i];
                if (m.State != MiteState.Caught) continue;
                float left = (SimConstants.CatchSteps - m.CaughtSteps) * SimConstants.StepSeconds;
                Vector3 sp = _camera.WorldToScreenPoint(SimRenderer.ToWorld(m.Position));
                var r = new Rect(sp.x - 60f, Screen.height - sp.y - _text.fontSize * 4.2f, 120f, _text.fontSize * 1.6f);
                GUI.Label(r, left.ToString("0.0") + "s", centre);
            }
        }

        private void DrawBreatheButton(Simulation sim)
        {
            Rect r = ToGui(BreatheButton());
            BreatheSim b = sim.Breathe;
            Color fill;
            string label;
            if (b.Active)
            {
                fill = new Color(0.25f, 0.5f, 0.9f, 0.9f);
                label = "BREATHING";
            }
            else if (b.CanActivate)
            {
                fill = new Color(0.2f, 0.65f, 0.4f, 0.9f);
                label = "BREATHE";
            }
            else
            {
                fill = new Color(0.3f, 0.3f, 0.35f, 0.9f);
                label = "BREATHE  " + Mathf.CeilToInt(b.CooldownStepsLeft * SimConstants.StepSeconds) + "s";
            }

            Fill(r, fill);
            var centre = new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            GUI.Label(r, label, centre);
        }

        private void DrawBarrierStatus(Simulation sim)
        {
            BarrierSim bar = sim.Barrier;
            string s;
            if (bar.Active) s = "Barrier  " + (bar.StepsLeft * SimConstants.StepSeconds).ToString("0.0") + "s";
            else if (bar.CooldownStepsLeft > 0) s = "Barrier cooling  " + (bar.CooldownStepsLeft * SimConstants.StepSeconds).ToString("0.0") + "s";
            else s = "Barrier READY";

            Rect b = ToGui(BreatheButton());
            var centre = new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width * 0.3f, b.y, Screen.width * 0.4f, b.height), s, centre);
        }

        private void DrawDebug(Simulation sim)
        {
            var sb = new StringBuilder();
            sb.AppendLine("step " + sim.StepCount + "   rate x" + sim.Breathe.StepRateScale.ToString("0.00"));
            sb.AppendLine("hash " + sim.StateHash().ToString("X16"));
            sb.AppendLine("pull " + sim.CurrentPullStrength.ToString("0.000") + (sim.FlameIsHolding ? "  (flame holding)" : ""));
            for (int i = 0; i < sim.Mites.Count; i++)
            {
                MiteSim m = sim.Mites[i];
                sb.Append("#" + m.Id + " " + m.State);
                if (m.State == MiteState.Caught) sb.Append(" " + m.CaughtSteps + "/" + SimConstants.CatchSteps);
                if (m.State == MiteState.Reacting) sb.Append(" " + m.ReactionStepsLeft);
                if (m.Panic > 0f) sb.Append(" panic " + m.Panic.ToString("0.0"));
                if (m.TimesRescued > 0) sb.Append(" rescued x" + m.TimesRescued);
                sb.AppendLine();
            }
            sb.AppendLine();
            sb.AppendLine("seed " + _runner.Config.Seed + "   " + _presetName);
            if (_runner.ReplayMessage.Length > 0) sb.AppendLine(_runner.ReplayMessage);
            sb.AppendLine("click/tap: barrier   B: breathe   R: restart   N: new seed");
            sb.Append("1-4: preset   V: verify replay");

            float w = Screen.width * 0.42f;
            GUI.Label(new Rect(Screen.width - w - 12f, 8, w, Screen.height * 0.6f), sb.ToString(), _small);
        }

        private void DrawOutcome(Simulation sim)
        {
            if (sim.Outcome == SimOutcome.Running) return;

            int alive = 0;
            for (int i = 0; i < sim.Mites.Count; i++) if (sim.Mites[i].IsAlive) alive++;
            bool won = sim.Outcome == SimOutcome.Won;
            string text = won
                ? "THE FLAME DIES\n" + alive + " of " + sim.Mites.Count + " survived"
                : "THE COLONY IS LOST";

            var shade = new Rect(0, Screen.height * 0.3f, Screen.width, Screen.height * 0.3f);
            Fill(shade, new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(shade, text + "\n<size=" + _text.fontSize + ">press R to try again</size>", _big);
        }

        private static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
