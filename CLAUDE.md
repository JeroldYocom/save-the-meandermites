# Save the Meandermites

One-finger Android game (Unity 6000.6.7f1 LTS, Universal 2D / URP). Permadeath Colony; design principle: *slow the hands, keep the stakes*. Solo developer, ~5-10 hrs/week.

## The rule everything rests on
Gameplay lives in `Assets/Sim/` (`Meandermites.Sim`): plain C#, **no `UnityEngine`**, fixed 0.02s step, seeded RNG only, deterministic. The Unity layer feeds it input and renders its state. Never put gameplay state in a MonoBehaviour. See `docs/adr/0001-sim-view-split.md`.

The same files compile twice: Unity via `Assets/Sim/Meandermites.Sim.asmdef` (`noEngineReferences`), and `dotnet` via `Sim/Meandermites.Sim.csproj`. Edit them in `Assets/Sim/` only.

## Verify your work (Claude cannot open the Editor)
- Sim tests: `cd Sim && dotnet test Meandermites.Sim.Tests --nologo -v minimal` - check the **exit code**, not a grep of the output. Skill: `sim-tests`.
- Boundary: `bash ~/.claude/skills/sim-view-boundary/scripts/check-boundary.sh Assets/Sim` (0 clean, 1 violations, 2 could not run = unknown, not clean).
- Unity compile/tests headless: skill `unity-headless`. **Unity must be closed first** - it locks `Library/`. Needs exit 0 AND zero `error CS` in the log.
- Unity exe: `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`

## Repo hygiene
- Force Text serialization stays on. `.meta` files are committed. Binaries go through Git LFS (`.gitattributes`).
- Public repo, **deliberately no LICENSE** (source-visible, all rights reserved). Do not add one.
- Keep CI to Tier 1 (`dotnet test`). Unity tests on PRs and APK builds on release tags only.
- Do not edit `ProjectSettings/*.asset` while Unity is open.

## Design source of truth
`docs/design/` (see its README). The four blocking gaps are decided in `docs/design/decisions-0001-blocking-gaps.md`: read it before writing Sim code (lock-on targeting, per-enemy barrier reactions in data, Spawn Portals, Breathe in the Sim).
