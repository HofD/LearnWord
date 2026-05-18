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
- concise final output with changed files, command result, and residual risk.

Avoid broad discovery prompts and duplicate investigations across agents.
