#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
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
