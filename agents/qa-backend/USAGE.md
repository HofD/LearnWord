# Using The QA Backend Agent

Use this agent when you want backend test coverage, scenario checks, or a QA-style investigation for LearnWord.

## How To Prompt It

Point the agent at `agents/qa-backend/AGENT.md` from the backend repository root and give it a focused testing task.

Good examples:

```text
Use agents/qa-backend/AGENT.md.
Add integration tests for collection ownership: users must not read, rename, or delete collections owned by another user.
```

```text
Use agents/qa-backend/AGENT.md.
Write tests that document current refresh-token rotation behavior and report any gaps.
```

```text
Use agents/qa-backend/AGENT.md.
Review specs/backend-api.md and propose the next 10 highest-value backend tests. Do not edit code yet.
```

## Expected Inputs

Give the agent:

- the target area: auth, collections, cards, words, review, middleware, repositories, or gateway;
- whether it should only design tests or also create them;
- whether production code fixes are allowed;
- the preferred test level if you know it: unit, integration, or E2E.
- the verification guardrail: use `./deploy/local-up.sh` or `cd LearnWord && ./tests/run-all-tests.sh`; do not run direct sandbox `dotnet` build/test commands unless explicitly requested.

If you do not specify a test level, the agent should choose the smallest reliable level.

## Expected Outputs

For implementation tasks, expect:

- added or updated backend test files;
- the exact Docker/local endpoint or repository test script command it ran;
- pass/fail result;
- a short note about remaining risks.

For analysis tasks, expect:

- prioritized scenarios;
- suggested test project placement;
- dependencies or infrastructure needed;
- any behavior that conflicts with `specs/backend-api.md`.

## Repository Conventions

Preferred local Docker run:

```bash
./deploy/local-up.sh
```

Use Docker as the default for integration, E2E, gateway, database, auth, and mail scenarios.

Local full-stack run:

```bash
./deploy/local-up.sh
```

Public local gateway:

```text
http://localhost:5100
```

Current backend contract:

```text
specs/backend-api.md
```

## Guardrails

The agent should not:

- rewrite production behavior while writing tests unless explicitly asked;
- use production databases or production credentials;
- delete user data or shared local databases without approval;
- replace documented current quirks with ideal behavior without first flagging the mismatch;
- add large new test infrastructure when a smaller local helper will do;
- run direct sandbox `dotnet restore`, `dotnet build`, or broad `dotnet test` unless the prompt explicitly asks for that diagnostic path.
