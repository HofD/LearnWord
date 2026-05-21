# Demo Scenarios

These scenarios are intended for portfolio walkthroughs, local smoke checks, and future demo videos.

## Scenario 1: Product Baseline

Goal: show the existing LearnWord product as a usable fullstack application.

1. Start the local Docker stack.
2. Open the frontend at `http://localhost:8088`.
3. Register a test user.
4. Confirm the account through Mailpit at `http://localhost:8025`.
5. Log in.
6. Create a collection.
7. Add a card with two words, translations, and transcription values.
8. Open the review flow.
9. Mark a card as learned or forgotten.

What this demonstrates:

- gateway-backed frontend flow;
- account lifecycle;
- authenticated user-owned data;
- collection/card/word CRUD;
- review behavior;
- local Docker verification.

## Scenario 2: Agentic SDLC Walkthrough

Goal: show the delivery process as the main portfolio differentiator.

1. Open `README.md` and frame LearnWord as a product sandbox for agent-assisted delivery.
2. Open `docs/architecture.md` and explain service boundaries.
3. Open `agents/README.md` and show the agent roster.
4. Open `agents/system-analyst/AGENT.md` and show the coordination role.
5. Open `specs/backend-api.md` and `specs/frontend-behavior.md` as sources of truth.
6. Open `agent-runs/001-baseline-review/` and show how work is recorded.

What this demonstrates:

- specs-driven development;
- role-specific AI agents;
- bounded assignments;
- Docker-first verification;
- final acceptance discipline.

## Scenario 3: AI Card Generator

Goal: show the implemented portfolio AI feature.

1. User opens a collection.
2. User clicks the AI generation header to expand the collapsed form.
3. User pastes source text.
4. User chooses source language, target language, CEFR level, and max cards.
5. User requests AI suggestions.
6. Backend returns structured draft cards.
7. User saves useful suggestions as normal cards.
8. User reviews the generated cards.

What this demonstrates:

- LLM integration through a provider abstraction;
- a thin `LearnWord.Identity` ownership facade;
- structured output validation;
- backend/frontend contract evolution;
- fake-provider tests;
- agent-run evidence from requirements to final acceptance.

For deterministic portfolio recording, keep `LW_AI_PROVIDER=Fake`. For a live LLM demo, set `LW_AI_PROVIDER=OpenRouter`, provide `LW_AI_OPENROUTER_API_KEY`, and use `LW_AI_OPENROUTER_MODEL=google/gemma-4-26b-a4b-it:free` unless a newer tested model is intentionally selected.

## Local Test Account

For repeatable local acceptance checks:

- email: `agent-ui-test@example.com`
- password: `Agent-test1!`

If the account does not exist, register it through the local app or gateway and confirm it through Mailpit.
