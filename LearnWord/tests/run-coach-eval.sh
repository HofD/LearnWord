#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
live_requested=false
help_requested=false
for arg in "$@"; do
  case "$arg" in
    --live) live_requested=true ;;
    --help) help_requested=true ;;
  esac
done
if "$live_requested" && ! "$help_requested" && [ -z "${LEARNWORD_COACH_EVAL_API_KEY:-}" ]; then
  if [ ! -t 0 ]; then
    printf '%s\n' 'Live evaluation needs a key. Run in a terminal for hidden input, or set LEARNWORD_COACH_EVAL_API_KEY in your environment.' >&2
    exit 2
  fi
  if ! read -r -s -p 'OpenRouter API key (hidden): ' LEARNWORD_COACH_EVAL_API_KEY; then
    printf '\n%s\n' 'Key input cancelled. No requests sent.' >&2
    exit 2
  fi
  printf '\n' >&2
  if [ -z "$LEARNWORD_COACH_EVAL_API_KEY" ]; then
    printf '%s\n' 'No key supplied. No requests sent.' >&2
    exit 2
  fi
  export LEARNWORD_COACH_EVAL_API_KEY
fi
report_dir="${LEARNWORD_COACH_EVAL_REPORT_DIR:-$repo_root/agent-runs/023-coach-behavior-eval/reports}"
mkdir -p "$report_dir"
report_dir="$(cd "$report_dir" && pwd)"
docker_args=(run --rm --init -i -v "$repo_root/LearnWord:/src" -v "$report_dir:/reports" -w /reports)
if [ -d "${HOME}/.nuget/packages" ]; then
  docker_args+=(-v "${HOME}/.nuget/packages:/nuget-fallback:ro" -e NUGET_FALLBACK_PACKAGES=/nuget-fallback)
fi
# Passing the variable by name keeps its value out of process arguments and output.
if [ -n "${LEARNWORD_COACH_EVAL_API_KEY:-}" ]; then
  docker_args+=(-e LEARNWORD_COACH_EVAL_API_KEY)
fi
exec docker "${docker_args[@]}" mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run --project /src/tools/LearnWord.CoachEval/LearnWord.CoachEval.csproj --no-launch-profile -- "$@"
