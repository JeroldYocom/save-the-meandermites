# Handoff: where the project stands

Written 2026-10-21, updated 2026-10-22, so work can resume cold, on this machine or another. Read this first, then `CLAUDE.md`.

## One paragraph
Save the Meandermites is a one-finger Android game about steering tiny, mortal creatures (Mites) away from a flame, with a permadeath Colony. Design principle: **slow the hands, keep the stakes.** The design is complete and build-ready for Chapter 1. The Phase 0 simulation core and a playable placeholder prototype are built and verified by machine. A first playtest was done on 2026-10-22 and pacing was retuned from it. **The next step is a human one: replay on the new timings and judge whether pulling a Mite free at the last second feels good (TC-023).** Nothing further should be built until that is answered.

## Status
| Area | State |
|---|---|
| Engine | Unity **6000.6.7f1 LTS**, Universal 2D (URP). Built-In was rejected because Unity deprecated it from 6.5 |
| Repo | `JeroldYocom/save-the-meandermites`, public, **no LICENSE on purpose** (source visible, all rights reserved) |
| Sim core | Built. 52 tests (CI). Deterministic, no `UnityEngine`. Includes Spawn Portals |
| Unity layer | Built. 11 EditMode tests. Placeholder shapes, IMGUI HUD |
| CI | Boundary check + Sim tests on every push. Does **not** run the Unity tests |
| Wiki | 10 pages + sidebar, https://github.com/JeroldYocom/save-the-meandermites/wiki |
| Seen by a human | First playtest done 2026-10-22; retuned; the new timings and portal looks are not yet judged |

## Try it
1. Open the project folder in Unity Hub with editor 6000.6.7f1.
2. Press **Play** (it starts itself in any scene).
3. Click/tap = place barrier, `B` = Breathe, `R` = restart, `N` = new seed, `1`-`4` = preset, `V` = verify replay, `P` = cycle the portal look.

## The architecture rule everything rests on
Gameplay lives in `Assets/Sim/` (`Meandermites.Sim`): plain C#, **no `UnityEngine`**, fixed 0.02 s step, seeded RNG only, no wall-clock reads, no trig. Unity (`Assets/View/`) feeds input in and draws state out. Breathe is Sim state (not `Time.timeScale`). The same source files compile under Unity (asmdef with `noEngineReferences`) and under `dotnet` (`Sim/Meandermites.Sim.csproj`, netstandard2.1). Reason: the Daily Challenge and Replay need exact reproducibility, and gameplay logic can be tested without opening Unity. See `docs/adr/0001-sim-view-split.md`.

## Repo map
| Path | What |
|---|---|
| `Assets/Sim/` | The simulation. Edit here only |
| `Assets/View/` | Unity layer: `GameBootstrap`, `SimRunner`, `SimRenderer`, `HudOverlay`, `Presets`, `ViewSprites`, `PortalArt` |
| `Assets/Tests/EditMode/` | Unity tests of the view layer |
| `Sim/` | `dotnet` project and 52 NUnit tests |
| `docs/design/` | Decisions and open gaps (see below) |
| `docs/adr/` | Architecture decision records |
| `.claude/skills/` | Vendored boundary-check script used by CI |
| `CLAUDE.md` | Standing instructions for Claude Code in this repo |

## Design documents
- **GDD v3.1** (master design) and the design review and setup plan live as Claude artifacts: links are in `docs/design/README.md`. The GDD's markdown source was lost; only the published artifact exists.
- `docs/design/decisions-0001-blocking-gaps.md`: the four decisions made before coding (lock-on enemy targeting, per-enemy barrier reactions defined in data, Spawn Portals, Breathe in the Sim). **Where it differs from the GDD, it wins.**
- `docs/design/sim-interpretations.md`: ten places the GDD was ambiguous and what was chosen. The biggest is Breathe: "0.25 for 2 seconds" was implemented in *real* seconds. After the first playtest (2026-10-22) it was shortened to 15 steps (1.2 real s); it already felt long, so do not lengthen it to 100.
- `docs/design/open-gaps-addendum.md`: the original recommendations plus five deferred gaps (Daily Challenge generator, Survival curve, cloud-save conflicts, teaching copy for 14 levels, art style reference).

## Not built yet
Enemies and Aggro Lines, panic effects, Super Barrier, Flame Pulse, Last Gasp, terrain (Gloom Moss), wind, Mite abilities and personalities, Huddle, the Whistle, scoring, portal patterns other than one-at-a-time, obstacles in the field, the second barrier slot, touch-offset ghost preview, any Android build.

## Open items
0. **First playtest done 2026-10-22; pacing retuned** (Mite speed 1.1, barrier 2.0 s, cooldown 0.5 s, Catch 1.5 s, Breathe 15 steps; see the wiki Feel Tuning Log). **Spawn Portals built:** a 1.0 s fade-in, then Mites leave one at a time 0.24 s apart; four placeholder looks, `P` cycles them. Next: replay and re-judge TC-023 and TC-035. The player also noted there are no obstacles in the field to juggle Mites with; that needs design before building.
1. **Playtest** and answer the Phase 0 gate: does the last-second rescue feel good?
2. Decide Breathe's duration after playing.
3. Decide whether to add Unity tests to CI (needs a Unity licence secret and 15-25 min per run, so pull requests only).
4. **Add the portal to the art commission manifest** (GDD Appendix D) and change the level format to a list of portals (Appendix A). Neither is done in the GDD artifact.
5. Write the **art style reference** before commissioning any art. It is the one open gap with real schedule risk.
6. Check determinism on a real Android device (so far only on one Windows PC).
7. Get legal advice on Google Play Families / COPPA before store submission.

## Resuming on a new machine
Install: Unity Hub, Unity **6000.6.7f1** with Android Build Support (OpenJDK, SDK, NDK), Git, **Git LFS** (`git lfs install` before the first commit), the .NET 10 SDK, and the GitHub CLI (`gh auth login`). Unity projects live under `C:\UnityProjects` on the original machine.

```bash
cd /c/UnityProjects
git clone https://github.com/JeroldYocom/save-the-meandermites.git
cd save-the-meandermites
git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
cd Sim && dotnet test Meandermites.Sim.Tests --nologo -v minimal
```
The merge driver lives in `.git/config`, which is not versioned, so it must be set on each clone. Then open the folder in Unity Hub and sign in to Unity.

**Not in this repo** (local to the original machine): the personal test-tracking notes (`docs/testing/`, deliberately gitignored because the repo is public), Claude Code's memory, and four Claude Code skills (`unity-headless`, `sim-tests`, `sim-view-boundary`, `test-tracking`). A portable backup of these was made at `docs/testing/_portable-backup/` on the original machine.

## Verification commands
| Check | Command | Pass means |
|---|---|---|
| Sim tests | `cd Sim && dotnet test Meandermites.Sim.Tests --nologo -v minimal` | exit 0 |
| Boundary | `bash .claude/skills/sim-view-boundary/scripts/check-boundary.sh Assets/Sim` | exit 0 |
| Unity tests | close the Editor (an open Editor aborts the run and leaves a stale results file), then `Unity.exe -batchmode -nographics -accept-apiupdate -projectPath <path> -runTests -testPlatform EditMode -testResults out.xml -logFile log.txt` | exit 0, zero `error CS` in the log, 11 passed (delete the old results file first) |

Unity locks `Library/`, so the Editor must be closed for a headless run. Judge a run by the exit code and the pass count, not by text in the output. `dotnet test` can exit 0 with "No test is available" (Windows Application Control blocked the test DLL on 2026-10-22); if so, rely on CI, which runs the Sim tests on a clean Linux runner.

## Conventions
- All tuning constants live in `Assets/Sim/SimConstants.cs`. Durations are whole steps. Changing one means logging it in the wiki's Feel Tuning Log.
- Never put gameplay state in a MonoBehaviour. Never use `UnityEngine.Input` (new Input System only). Statics survive between Play sessions (domain reload is off), so re-check Unity-object statics for null.
- Keep CI cheap: Tier 1 only. Unity tests on pull requests, Android builds on release tags.
- Every change is followed by the test-tracking flow: machine-verify, then list what only a human can judge.
