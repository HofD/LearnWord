# Agentic SDLC Workflow

LearnWord uses an experimental workflow for AI-assisted software delivery. The goal is to make AI coding work traceable, bounded, and verifiable instead of relying on broad chat prompts and unreviewed code changes.

## Problem

Naive AI-assisted development often fails in predictable ways:

- requirements are implicit;
- frontend and backend contracts drift apart;
- agents make broad unrelated edits;
- verification is skipped or done in the wrong environment;
- final acceptance checks only confirm that code was changed, not that the requested behavior works.

## Approach

The project uses:

- explicit backend and frontend behavior specs;
- role-specific agent instructions;
- narrow task assignments;
- Docker-first verification;
- automated regression checks;
- browser checks for UI changes;
- final acceptance notes;
- recorded `agent-runs/` for portfolio evidence.

## Agent Roles

- System Analyst: requirements, acceptance criteria, spec ownership, task routing, final acceptance.
- Backend Development: .NET backend implementation and backend contract fixes.
- QA Backend: backend test design, regression coverage, and QA investigations.
- Frontend UI: Angular implementation, responsive layout, accessibility, and browser verification.

The agent roster lives in `agents/README.md`.

## Sources Of Truth

- Backend behavior: `specs/backend-api.md`
- Frontend behavior: `specs/frontend-behavior.md`
- Agent responsibilities: `agents/README.md`
- Local run and deployment: `deploy/README.md`

If code, specs, and requested behavior disagree, the disagreement should be made explicit before acceptance.

## Delivery Flow

1. Requirement intake
   - Identify affected flows.
   - Decide whether this is a behavior change, maintenance task, discovery task, or documentation task.

2. Acceptance criteria
   - Define what must be true for the work to be accepted.
   - Include user-visible behavior, API contracts, ownership rules, and verification expectations.

3. Spec update
   - Update backend contract first when API behavior changes.
   - Update frontend behavior second when UI or client behavior changes.
   - Leave specs unchanged for documentation-only work.

4. Specialist assignment
   - Assign one narrow owner per bounded area.
   - State allowed files, whether production/test/spec changes are allowed, expected checks, and output limits.

5. Implementation and QA
   - Keep changes scoped.
   - Add tests according to risk and behavior surface.
   - Preserve documented quirks unless the task intentionally changes them.

6. Verification
   - Prefer `./deploy/local-up.sh` for full local verification.
   - By default, local/prod helper scripts use `LW_STANDARD_IMAGE_PULL=missing`, so standard Docker images are pulled only when absent.
   - Use `cd LearnWord && ./tests/run-all-tests.sh` for focused backend regression checks.
   - Use browser inspection for UI changes when the app can run locally.

7. Final acceptance
   - Compare the result with the original request and acceptance criteria.
   - Record what changed, what was verified, and remaining risks.

## Recording Agent Runs

Significant changes should create a directory under `agent-runs/`:

```text
agent-runs/
  001-baseline-review/
    task.md
    system-analyst-output.md
    final-acceptance.md
```

For feature work with specialist handoffs, use:

```text
agent-runs/
  002-ai-card-generator/
    task.md
    system-analyst-output.md
    backend-agent-output.md
    frontend-agent-output.md
    qa-agent-output.md
    final-acceptance.md
```

Each run should be concise. It is an audit trail and portfolio artifact, not a transcript dump.

## Recommended Run File Contents

`task.md`:

- original request or summarized task;
- scope;
- acceptance criteria;
- out of scope.

`system-analyst-output.md`:

- requirement interpretation;
- affected specs;
- assignments or direct actions;
- risks and verification plan.

Specialist output files:

- owner agent;
- allowed scope;
- changed files;
- verification performed;
- residual risk.

`final-acceptance.md`:

- accepted or needs follow-up;
- files changed;
- checks run;
- checks not run and why;
- next recommended step.

## Portfolio Value

The recorded runs show how the project was developed:

- not only what code exists;
- why the change was made;
- which contract governed it;
- how agent responsibilities were bounded;
- what verification was considered sufficient.
