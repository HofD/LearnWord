# Using The Backend Development Agent

Use this agent when you want backend implementation, API behavior fixes, service logic changes, persistence updates, or coordinated backend spec updates for LearnWord.

## How To Prompt It

Point the agent at `agents/backend-dev/AGENT.md` from the backend repository root and give it a focused backend implementation task.

Good examples:

```text
Use agents/backend-dev/AGENT.md.
Fix collection rename so unauthorized users receive the documented response. Update specs/backend-api.md if the contract changes and run focused tests.
```

```text
Use agents/backend-dev/AGENT.md.
Implement word delete review-state reset according to specs/backend-api.md and add a regression test.
```

```text
Use agents/backend-dev/AGENT.md.
Review the refresh-token endpoint against specs/backend-api.md. Propose the implementation change first; do not edit code yet.
```

```text
Use agents/backend-dev/AGENT.md.
Improve AI card generation validation in LearnWord.WebApi. Keep LearnWord.Identity as a thin ownership facade, preserve the public route, update specs/backend-api.md if the contract changes, and use fake-provider tests.
```

## Expected Inputs

Give the agent:

- the target backend area: auth, collections, cards, words, review, middleware, repositories, gateway, or migrations;
- whether it should edit production code or only investigate/propose;
- whether contract changes are allowed;
- which test level is expected, if known.
- the verification guardrail: use `./deploy/local-up.sh` or `cd LearnWord && ./tests/run-all-tests.sh`; do not run direct sandbox `dotnet` build/test commands unless explicitly requested.
- for AI work, whether provider changes affect `Fake`, `OpenRouter`, or only configuration.

If you do not specify a test level, the agent should choose the smallest reliable test for the changed behavior.

## Expected Outputs

For implementation tasks, expect:

- updated backend production code and focused tests when practical;
- updated `specs/backend-api.md` when the public contract changes;
- exact Docker/local endpoint checks run and pass/fail result;
- any handoff needed for the QA backend agent.

For analysis tasks, expect:

- current implementation summary;
- spec alignment or mismatch;
- proposed implementation plan;
- likely test placement and risks.

## Repository Conventions

Preferred local Docker run:

```bash
./deploy/local-up.sh
```

Use Docker as the default build and verification path. Direct backend sandbox `dotnet` commands are not a fallback unless the prompt explicitly asks for that diagnostic path.

Focused backend regression check:

```bash
cd LearnWord
./tests/run-all-tests.sh
```

Current backend contract:

```text
specs/backend-api.md
```

AI feature reference:

```text
docs/ai-features.md
```

## Guardrails

The agent should not:

- change public API behavior without either preserving or updating the backend spec;
- take over broad QA planning when `agents/qa-backend/AGENT.md` is the better owner;
- run direct sandbox `dotnet restore`, `dotnet build`, or broad `dotnet test` unless the prompt explicitly asks for that diagnostic path;
- modify Angular screens unless explicitly assigned as a coordinated full-stack task;
- move OpenRouter or prompt logic into `LearnWord.Identity`;
- require a real OpenRouter key for automated tests;
- introduce new backend frameworks or large dependencies without approval;
- skip reporting unverified behavior.
