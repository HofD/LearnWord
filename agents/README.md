# LearnWord Agents

These agents capture the current project working model. Use the system analyst as the coordinator for cross-domain work and final acceptance, then route focused implementation or QA tasks to the specialist agents.

## Agent Roster

- [System Analyst](system-analyst/AGENT.md): owns requirements, frontend and backend specs, agent assignments, final test runs, and visual acceptance.
- [Backend Development](backend-dev/AGENT.md): owns .NET backend implementation, API behavior fixes, backend contract updates, and focused backend tests.
- [QA Backend](qa-backend/AGENT.md): owns backend test design, automated coverage, scenario checks, and QA investigations.
- [Frontend UI](frontend-ui/AGENT.md): owns Angular implementation, mobile-first UI, accessibility, responsive polish, and browser verification.

## Default Routing

- New feature or ambiguous request: start with `agents/system-analyst/AGENT.md`.
- Backend implementation: use `agents/backend-dev/AGENT.md`.
- Backend testing or regression coverage: use `agents/qa-backend/AGENT.md`.
- Frontend implementation or visual review: use `agents/frontend-ui/AGENT.md`.
- Cross-domain change: system analyst updates or confirms specs, then assigns backend, frontend, and QA work separately.
- AI feature changes: start with the system analyst, keep provider configuration in backend docs/specs, keep `LearnWord.Identity` as the ownership-checking facade, and route LLM behavior to `LearnWord.WebApi`.

## Agent Budget And Delegation Rules

Keep agent work narrow by default. Spawn or invoke a specialist only when the task has a clear owner, a bounded file area, and a concrete output. Do not ask agents to "look around", "figure out everything", or produce broad reports unless that is the task.

Every assignment should include:

- the exact area to inspect or edit;
- whether code changes are allowed;
- the expected verification command or endpoint check;
- whether the work should be recorded under `agent-runs/`;
- a short output limit: changed files, verification result, residual risk.

Prefer one specialist at a time unless work is truly independent. Do not duplicate the same investigation across agents. If the main thread can do a small read or focused edit directly, do that instead of paying for a specialist handoff.

## Agent Run Recording

Record significant work under `agent-runs/` when the task is useful as delivery evidence, changes product behavior, changes public contracts, adds a portfolio feature, or coordinates multiple agents.

Use this structure:

```text
agent-runs/
  002-short-feature-name/
    task.md
    system-analyst-output.md
    backend-agent-output.md
    frontend-agent-output.md
    qa-agent-output.md
    final-acceptance.md
```

Small maintenance tasks may omit unused specialist files. Do not paste raw chat transcripts. Keep each file as a concise run artifact: scope, decisions, changed files, verification, and residual risk.

The system analyst owns creating the run directory, writing `task.md`, collecting specialist outputs, and writing `final-acceptance.md`. Specialist agents should return a report that can be saved as their corresponding `*-agent-output.md` file when the assignment is part of a recorded run.

When a task changes the AI card generator, update the existing feature docs and create a new run only if the work changes behavior, provider integration, prompt design, tests, or user-facing UX. Tiny copy edits can stay out of `agent-runs/` unless the user explicitly asks to record them.

## Docker-First Verification

Agents should use the local Docker environment for builds, integration checks, smoke checks, and visual verification whenever feasible. The sandbox is not the source of truth for project verification.

Default local environment:

```bash
./deploy/local-up.sh
```

Local endpoints:

- frontend: `http://localhost:8088`
- gateway: `http://localhost:5100`
- Mailpit: `http://localhost:8025`

Backend agents must not run direct `dotnet restore`, `dotnet build`, or broad `dotnet test` commands in the sandbox as their normal build path. Backend build and service verification should go through:

```bash
./deploy/local-up.sh
```

Focused backend regression runs should go through the repository test script:

```bash
cd LearnWord
./tests/run-all-tests.sh
```

Direct backend sandbox commands are allowed only for a small compile or diagnostic check after the assignment explicitly asks for it. If Docker or the local script cannot be run, the agent must stop, report the blocker, and avoid spending time trying alternate sandbox builds.

Use direct frontend commands such as `npm run build` only for narrow frontend diagnosis or as a fallback when Docker is unavailable. If a Docker check cannot be run, the agent must say so explicitly.

## Local Test Account

Use this account for local Docker browser and gateway checks that need an authenticated user:

- email: `agent-ui-test@example.com`
- password: `Agent-test1!`

If the account is missing, unconfirmed, or login fails, recreate it through the local gateway:

1. `POST http://localhost:5100/api/account/register` with `{ "email": "agent-ui-test@example.com", "password": "Agent-test1!" }`.
2. Open Mailpit at `http://localhost:8025` or query `GET http://localhost:8025/api/v1/messages`, find the newest confirmation email for `agent-ui-test@example.com`, and call the confirmation URL against the gateway host if needed.
3. Log in with `POST http://localhost:5100/api/auth/login`.
4. If no learning data exists, create a small collection and one card with two words before visual checks.

Do not rely on this account in production or commit real user data. It is only for the local Docker environment.

## Shared Sources Of Truth

- Backend behavior: `specs/backend-api.md`
- Frontend behavior: `specs/frontend-behavior.md`
- Backend code: `LearnWord/LearnWord`
- Frontend code: `../LearnWordWebApp/lw-app`
- Recorded delivery runs: `agent-runs/`

Specs should move with intentional behavior changes. If an agent finds a mismatch between specs, code, and the requested behavior, it should report the mismatch instead of silently choosing a new contract.
