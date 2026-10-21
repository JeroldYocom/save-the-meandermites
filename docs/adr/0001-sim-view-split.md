# ADR 0001: Split the game into a deterministic Sim and a Unity View

Status: accepted, 2026-10-21

## Context
Daily Challenge (date-derived seed, no server) and Replay need identical inputs to give identical outcomes. The developer works alone in short sessions, and the AI assistant cannot open the Unity Editor.

## Decision
All gameplay lives in `Assets/Sim/` (`Meandermites.Sim`): no `UnityEngine`, fixed 0.02s step, seeded `SimRandom` only, no wall-clock reads. The Unity layer feeds recorded input in and renders state out. Breathe, including its cooldown, is sim state counted in sim time; Unity only changes how fast steps are pumped.

The same source compiles under Unity (asmdef with `noEngineReferences`) and under `dotnet` (`Sim/Meandermites.Sim.csproj`, netstandard2.1), so the engine itself rejects an accidental `UnityEngine` reference and the logic is testable without a Unity licence.

## Consequences
- Determinism is structural, not a matter of discipline; guarded by state-hash tests.
- CI is nearly free (`dotnet test`, ~1 minute).
- New Mite types become data, not classes.
- The cost is a thin adapter layer in Unity; accepted.
