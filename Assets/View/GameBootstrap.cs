using Meandermites.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Meandermites.View
{
    /// <summary>
    /// Builds the whole prototype from code, so no scene wiring is needed: it
    /// starts itself when Play is pressed in any scene. It reads pointer and
    /// keyboard input, hands it to the SimRunner, and keeps the camera fitted.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private static readonly Color Backdrop = new Color(0.12f, 0.1f, 0.18f);

        private Camera _camera;
        private SimRunner _runner;
        private SimRenderer _renderer;
        private HudOverlay _hud;
        private int _preset = 1;
        private ulong _seed = 1;

        public SimRunner Runner => _runner;
        public SimRenderer SimView => _renderer;
        public Camera GameCamera => _camera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("Meandermites").AddComponent<GameBootstrap>();
        }

        private void Awake()
        {
            Init();
        }

        /// <summary>Separate from Awake so edit-mode tests can call it.</summary>
        public void Init()
        {
            if (_runner != null) return;

            Application.targetFrameRate = 60;

            _camera = Camera.main;
            if (_camera == null)
            {
                var camObject = new GameObject("Main Camera") { tag = "MainCamera" };
                _camera = camObject.AddComponent<Camera>();
            }
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop;
            _camera.transform.rotation = Quaternion.identity;
            FitCamera();

            _runner = gameObject.AddComponent<SimRunner>();
            _renderer = gameObject.AddComponent<SimRenderer>();
            _hud = gameObject.AddComponent<HudOverlay>();
            _hud.Initialise(_runner, _camera);

            StartLevel();
        }

        public void StartLevel()
        {
            _runner.Begin(Presets.Build(_preset, _seed));
            _renderer.Build(_runner.Sim);
            _hud.SetPresetName(Presets.Name(_preset));
            _hud.SetPortalStyleName(_renderer.PortalStyleName);
        }

        private void Update()
        {
            if (_runner == null) return;

            FitCamera();
            HandleInput();
            _runner.Pump(Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (_runner != null && _runner.Sim != null) _renderer.Refresh(_runner.Sim);
        }

        // Fit the whole 16x9 field on any aspect ratio, never cropping it.
        private void FitCamera()
        {
            float aspect = Mathf.Max(0.1f, _camera.aspect);
            float needed = Mathf.Max(SimConstants.FieldHeight * 0.5f, SimConstants.FieldWidth * 0.5f / aspect);
            _camera.orthographicSize = needed + 0.5f;
            _camera.transform.position = new Vector3(
                SimConstants.FieldWidth * 0.5f, SimConstants.FieldHeight * 0.5f, -10f);
        }

        private void HandleInput()
        {
            Pointer pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                Vector2 screen = pointer.position.ReadValue();
                if (HudOverlay.BreatheButton().Contains(screen))
                {
                    _runner.QueueBreathe();
                }
                else
                {
                    Vector3 world = _camera.ScreenToWorldPoint(
                        new Vector3(screen.x, screen.y, -_camera.transform.position.z));
                    _runner.QueueBarrier(new SimVec2(world.x, world.y));
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.bKey.wasPressedThisFrame) _runner.QueueBreathe();
            if (kb.vKey.wasPressedThisFrame) _runner.VerifyReplay();
            if (kb.rKey.wasPressedThisFrame) StartLevel();
            if (kb.nKey.wasPressedThisFrame)
            {
                _seed = (ulong)Random.Range(1, 1000000);
                StartLevel();
            }
            if (kb.pKey.wasPressedThisFrame)
            {
                _renderer.CyclePortalStyle();
                StartLevel();
            }
            if (kb.digit1Key.wasPressedThisFrame) SetPreset(1);
            if (kb.digit2Key.wasPressedThisFrame) SetPreset(2);
            if (kb.digit3Key.wasPressedThisFrame) SetPreset(3);
            if (kb.digit4Key.wasPressedThisFrame) SetPreset(4);
        }

        private void SetPreset(int preset)
        {
            _preset = preset;
            StartLevel();
        }
    }
}
