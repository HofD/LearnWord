#!/usr/bin/env bash
set -u

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

projects=()
while IFS= read -r project; do
  projects+=("$project")
done < <(find "$script_dir" -mindepth 2 -maxdepth 2 -name '*Tests.csproj' | sort)

if [ "${#projects[@]}" -eq 0 ]; then
  echo "No test projects found under $script_dir"
  exit 1
fi

declare -a names=()
declare -a statuses=()
declare -a durations=()

overall_status=0

echo "Found ${#projects[@]} test project(s)."
echo

for project in "${projects[@]}"; do
  name="$(basename "$(dirname "$project")")"
  names+=("$name")

  echo "========================================"
  echo "Running $name"
  echo "Project: $project"
  echo "========================================"

  start_seconds="$SECONDS"
  dotnet test "$project" --disable-build-servers --logger "console;verbosity=minimal"
  exit_code="$?"
  duration="$((SECONDS - start_seconds))s"
  durations+=("$duration")

  if [ "$exit_code" -eq 0 ]; then
    statuses+=("PASSED")
  else
    statuses+=("FAILED")
    overall_status=1
  fi

  echo
done

echo "========================================"
echo "Test summary"
echo "========================================"

for index in "${!names[@]}"; do
  printf "%-40s %-8s %s\n" "${names[$index]}" "${statuses[$index]}" "${durations[$index]}"
done

echo

if [ "$overall_status" -eq 0 ]; then
  echo "All test projects passed."
else
  echo "One or more test projects failed."
fi

exit "$overall_status"
