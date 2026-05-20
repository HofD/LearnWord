# LearnWord Repo Agent Instructions

Use `agents/README.md` as the agent roster and working model for this repository.

## Backend Verification

Do not use direct sandbox `dotnet restore`, `dotnet build`, or broad `dotnet test` as the normal backend build or verification path.

Use the local Docker/script workflow instead:

```bash
./deploy/local-up.sh
```

For focused backend regression checks from `LearnWord/LearnWord`:

```bash
./tests/run-all-tests.sh
```

Direct sandbox `dotnet` commands are allowed only when the user or assignment explicitly requests a narrow diagnostic check. If Docker or the local script cannot run, report the blocker and residual risk instead of trying alternate sandbox builds.

## Agent Budget

Keep agent assignments narrow:

- one owner agent per task unless scopes are independent;
- exact endpoint, flow, component, or file area;
- explicit permission for production, test, and spec changes;
- expected verification command;
- whether the task should be recorded under `agent-runs/`;
- concise final output with changed files, command result, and residual risk.

Avoid broad discovery prompts and duplicate investigations across agents.

## Agent Run Recording

Use `agent-runs/` for significant work that should remain visible as portfolio or delivery evidence. The system analyst owns the run directory, `task.md`, collected specialist outputs, and `final-acceptance.md`. Specialist agents should return concise output that can be saved as `backend-agent-output.md`, `frontend-agent-output.md`, or `qa-agent-output.md` when the assignment is part of a recorded run.

Do not store raw chat transcripts. Store the useful delivery artifact: scope, decisions, changed files, checks run, checks skipped, and residual risk.
