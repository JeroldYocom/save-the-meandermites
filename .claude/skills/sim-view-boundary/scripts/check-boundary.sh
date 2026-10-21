#!/usr/bin/env bash
# Check that a deterministic simulation layer has not been contaminated by
# engine or non-deterministic dependencies.
#
# Usage:  check-boundary.sh [sim-dir]
#   sim-dir defaults to "Sim"
#
# Exit 0 = clean, 1 = violations found, 2 = could not run.
#
# Uses grep deliberately rather than ripgrep: rg is often present on a tool's
# own PATH but absent from the shell a script runs in, and a linter that
# cannot run is far more dangerous than one that is slightly slower.

set -uo pipefail

SIM_DIR="${1:-Sim}"

if [[ ! -d "$SIM_DIR" ]]; then
  echo "ERROR: no such directory: $SIM_DIR" >&2
  echo "Pass the simulation directory as the first argument." >&2
  exit 2
fi

GREP_OPTS=(-rnE --include='*.cs'
           --exclude-dir=obj --exclude-dir=bin --exclude-dir='*.Tests')

violations=0
warnings=0

# Reports something that is not wrong by itself but is the precondition for a
# determinism bug, and needs a human or model to look at it. Does not fail.
warn() {
  local label="$1" why="$2" pattern="$3"
  local hits rc
  hits=$(grep "${GREP_OPTS[@]}" -e "$pattern" "$SIM_DIR" 2>&1)
  rc=$?
  if (( rc >= 2 )); then
    echo ""
    echo "ERROR: the check for '$label' could not run."
    echo "$hits" | sed 's/^/    /'
    exit 2
  fi
  if (( rc == 0 )) && [[ -n "$hits" ]]; then
    echo ""
    echo "REVIEW: $label"
    echo "  why: $why"
    echo "$hits" | sed 's/^/    /'
    warnings=$((warnings + 1))
  fi
}

report() {
  local label="$1" why="$2" pattern="$3"
  local hits rc
  # grep exit codes: 0 = matched, 1 = no match, 2+ = error.
  # An error must fail loudly: a linter that silently reports clean when its
  # own pattern is broken is worse than no linter at all.
  hits=$(grep "${GREP_OPTS[@]}" -e "$pattern" "$SIM_DIR" 2>&1)
  rc=$?
  if (( rc >= 2 )); then
    echo ""
    echo "ERROR: the check for '$label' could not run."
    echo "$hits" | sed 's/^/    /'
    echo "    Fix this before trusting the result."
    exit 2
  fi
  if (( rc == 0 )) && [[ -n "$hits" ]]; then
    echo ""
    echo "VIOLATION: $label"
    echo "  why: $why"
    echo "$hits" | sed 's/^/    /'
    violations=$((violations + 1))
  fi
}

echo "Checking simulation boundary in: $SIM_DIR"

report "UnityEngine reference" \
       "couples the sim to the engine; it can then only be tested inside Unity" \
       'using UnityEngine|UnityEngine\.'

report "Frame-dependent time" \
       "frame-rate dependent, so two machines diverge and replays desync" \
       '\bTime\.(deltaTime|time|unscaledTime|fixedDeltaTime|timeScale)\b'

report "Unseeded Random" \
       "a different sequence every run, so nothing is reproducible" \
       '\bnew (System\.)?Random\([[:space:]]*\)'

# \b already excludes RandomSource, RandomWalk etc: a trailing word boundary
# cannot match between two word characters.
report "System.Random used directly" \
       "even seeded, its sequence is not guaranteed stable across runtimes; use the sim's own RNG" \
       '\b(System\.)?Random\b'

report "Wall-clock or unrepeatable identity" \
       "unrepeatable input makes a run impossible to replay" \
       '\bDateTime\.(Now|UtcNow|Today)\b|\bGuid\.NewGuid\(\)|\bEnvironment\.TickCount\b|\bStopwatch\b'

# Catching "foreach over a Dictionary variable" needs the declared type of the
# iterated expression, which grep cannot resolve. So flag the declarations
# instead -- a lookup-only Dictionary is perfectly deterministic and fine, but
# every instance of this bug starts with one of these lines.
warn "Unordered collection declared in the sim" \
     "fine for keyed lookup, but if anything iterates it to apply forces, resolve collisions or award score, results diverge -- check each use" \
     '\b(Dictionary|HashSet|ConcurrentDictionary)[[:space:]]*<'

# Project-file level: the Sim must build with plain dotnet, no Unity present.
csproj_hits=$(grep -rnE --include='*.csproj' -e 'UnityEngine|UnityEditor' "$SIM_DIR" 2>&1)
csproj_rc=$?
if (( csproj_rc >= 2 )); then
  echo ""
  echo "ERROR: the project-file check could not run."
  echo "$csproj_hits" | sed 's/^/    /'
  exit 2
fi
if (( csproj_rc == 0 )) && [[ -n "$csproj_hits" ]]; then
  echo ""
  echo "VIOLATION: Unity assembly referenced in a project file"
  echo "  why: the sim must build and test with plain dotnet, with no Unity install present"
  echo "$csproj_hits" | sed 's/^/    /'
  violations=$((violations + 1))
fi

echo ""
if (( violations == 0 )); then
  if (( warnings > 0 )); then
    echo "CLEAN: no violations. $warnings item(s) marked REVIEW above -- read those before trusting determinism."
  else
    echo "CLEAN: simulation boundary intact."
  fi
  exit 0
fi

echo "FAILED: $violations violation category/categories found."
if (( warnings > 0 )); then
  echo "        plus $warnings item(s) marked REVIEW."
fi
echo "Each violation compiles and runs correctly -- they fail later, as replay drift."
exit 1
