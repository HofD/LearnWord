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

```text
Use agents/system-analyst/AGENT.md.
Plan an improvement to AI card generation. Keep LearnWord.Identity as a thin ownership facade, route LLM behavior to LearnWord.WebApi, update specs and docs, record the run if behavior changes, and coordinate backend, frontend, and QA agents as needed.
```

## Expected Inputs

Give the agent:

- the user request or product goal;
- whether implementation should start immediately or only planning is needed;
- any known constraints, deadlines, or forbidden changes;
- which checks are mandatory, if any.
- whether AI provider changes should use the fake provider, OpenRouter, or only documentation/config updates.

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
- agent-run location when the work was recorded.

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

Use this Docker stack as the default verification path. Backend sandbox `dotnet` checks are not a fallback unless explicitly requested for narrow diagnosis. Frontend `npm run build` checks are fallback or narrow diagnostic commands, not the preferred final acceptance surface.

`./deploy/local-up.sh` defaults to `LW_STANDARD_IMAGE_PULL=missing`, so standard Docker images are pulled only if absent. Use `LW_STANDARD_IMAGE_PULL=never` for offline/no-network verification and `LW_STANDARD_IMAGE_PULL=always` when explicitly refreshing base images.

For backend work, do not ask specialist agents to run direct sandbox `dotnet restore`, `dotnet build`, or broad `dotnet test`. Assign `./deploy/local-up.sh` for build/startup checks and `cd LearnWord && ./tests/run-all-tests.sh` for focused backend regression checks. If those cannot run, the agent should report the blocker instead of trying alternate sandbox builds.

For AI card generation, keep these boundaries explicit:

- `LearnWord.Identity` checks auth and collection ownership, then proxies the request.
- `LearnWord.WebApi` owns validation, prompt construction, provider selection, OpenRouter calls, parsing, and suggestion validation.
- The frontend must not contain provider keys or model credentials.
- Default local usage should stay on the fake provider unless the task explicitly asks for a live OpenRouter check.

## Guardrails

The agent should not:

- implement broad backend or frontend changes itself when a specialist agent should own them;
- assign broad, open-ended specialist tasks when a specific endpoint, flow, file area, and output limit would do;
- leave specs stale after intentional behavior changes;
- accept work without checking the original request against the delivered behavior;
- skip visual verification for UI changes when the app can run locally;
- accept sandbox-only verification when the local Docker stack is available;
- let backend agents burn time on direct sandbox `dotnet` builds;
- hide unresolved handoffs.
