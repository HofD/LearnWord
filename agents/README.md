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

Use direct `dotnet test`, `npm run build`, or other non-Docker commands only for narrow local diagnosis or as a fallback when Docker is unavailable. If a Docker check cannot be run, the agent must say so explicitly.

## Shared Sources Of Truth

- Backend behavior: `specs/backend-api.md`
- Frontend behavior: `specs/frontend-behavior.md`
- Backend code: `LearnWord/LearnWord`
- Frontend code: `../LearnWordWebApp/lw-app`

Specs should move with intentional behavior changes. If an agent finds a mismatch between specs, code, and the requested behavior, it should report the mismatch instead of silently choosing a new contract.
