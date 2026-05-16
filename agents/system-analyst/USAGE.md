# Using The System Analyst Agent

Use this agent when you want project-level requirements analysis, spec updates, agent coordination, task decomposition, or final acceptance for LearnWord changes.

## How To Prompt It

Point the agent at `agents/system-analyst/AGENT.md` from the backend repository root and give it the user request or feature goal.

Good examples:

```text
Use agents/system-analyst/AGENT.md.
Plan and coordinate a fix for review cards so transcriptions display correctly. Update specs if behavior changes and perform final acceptance.
```

```text
Use agents/system-analyst/AGENT.md.
Compare the current app behavior with specs/frontend-behavior.md and specs/backend-api.md. Produce prioritized work for the specialist agents.
```

```text
Use agents/system-analyst/AGENT.md.
Review the backend-dev and frontend-ui outputs for the collection rename change, run final checks, and decide whether the task is accepted.
```

## Expected Inputs

Give the agent:

- the user request or product goal;
- whether implementation should start immediately or only planning is needed;
- any known constraints, deadlines, or forbidden changes;
- which checks are mandatory, if any.

If you do not specify the agent roster, the system analyst should use `agents/README.md`.

## Expected Outputs

For coordination tasks, expect:

- concise acceptance criteria;
- specialist assignments with clear scope;
- spec changes needed before or during implementation;
- final verification plan.

For final acceptance tasks, expect:

- reviewed implementation summary;
- spec alignment result;
- Docker/local endpoint commands and visual checks run;
- accepted/not accepted decision with remaining risks.

## Repository Conventions

Agent registry:

```text
agents/README.md
```

Specs:

```text
specs/backend-api.md
specs/frontend-behavior.md
```

Local full-stack run:

```bash
./deploy/local-up.sh
```

Use this Docker stack as the default verification path. Direct `dotnet test` or `npm run build` checks are fallback or narrow diagnostic commands, not the preferred final acceptance surface.

## Guardrails

The agent should not:

- implement broad backend or frontend changes itself when a specialist agent should own them;
- leave specs stale after intentional behavior changes;
- accept work without checking the original request against the delivered behavior;
- skip visual verification for UI changes when the app can run locally;
- accept sandbox-only verification when the local Docker stack is available;
- hide unresolved handoffs.
