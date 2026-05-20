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

## Scenario 3: Future AI Card Generator

Goal: show the intended next portfolio feature.

1. User opens a collection.
2. User starts AI generation.
3. User pastes source text.
4. Backend returns structured draft cards.
5. User selects suggestions.
6. Accepted cards are saved to the collection.
7. User reviews the generated cards.

What this should demonstrate after implementation:

- LLM integration through a provider abstraction;
- structured output validation;
- backend/frontend contract evolution;
- fake-provider tests;
- agent-run evidence from requirements to final acceptance.

## Local Test Account

For repeatable local acceptance checks:

- email: `agent-ui-test@example.com`
- password: `Agent-test1!`

If the account does not exist, register it through the local app or gateway and confirm it through Mailpit.
