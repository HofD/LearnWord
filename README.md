# LearnWord

LearnWord is a fullstack language-learning application used as a sandbox for experimenting with a spec-driven, agent-assisted software delivery workflow.

The product lets users register, confirm email, log in, manage word collections, create cards with words and translations, generate draft cards with AI, and review cards with spaced repetition. The portfolio value of the project is broader than the learning app itself: it demonstrates how a small product can be developed through explicit contracts, role-specific AI agents, Docker-first verification, and final acceptance checks.

## Live Demo

Try the published application: **[learnword.online](https://learnword.online)**

Key scenario:
1. Register and confirm email.
2. Create a collection.
3. Paste source text into AI Card Generator.
4. Review generated card suggestions.
5. Save selected cards and practice them through spaced repetition.

## Highlights

- .NET backend with gateway, identity, user-owned learning entities, and backend tests.
- Angular frontend for registration, authentication, collections, cards, words, and review flows.
- Ocelot gateway as the public `/api/...` entry point.
- JWT authentication with refresh tokens, email confirmation, and password reset.
- AI card generation from source text with a fake local provider and configurable OpenRouter provider.
- Spaced repetition review with due-card selection and four outcome grades.
- PostgreSQL-backed local Docker environment.
- Current behavior specs for backend and frontend contracts.
- Role-specific agent instructions for system analysis, backend development, backend QA, and frontend UI work.
- Docker-first verification scripts for builds, service startup, smoke checks, and regression checks.

## Portfolio Case

LearnWord is positioned as:

> A fullstack product sandbox for building and testing an agent-assisted SDLC workflow.

The project is intended to show an engineering process, not only an application feature list. The core workflow is:

1. Capture requirements and acceptance criteria.
2. Keep backend and frontend specs as the source of truth.
3. Route bounded work to specialized agents.
4. Verify changes through Docker, tests, gateway smoke checks, and browser checks where relevant.
5. Record significant agent runs as reproducible portfolio artifacts.

See:

- [Architecture](docs/architecture.md)
- [Agentic SDLC workflow](docs/agentic-sdlc.md)
- [AI features](docs/ai-features.md)
- [Demo scenarios](docs/demo-scenarios.md)
- [Agent runs](agent-runs/)

## Repository Layout

```text
LearnWord/
  LearnWord/                  backend solution and services
  agents/                     role-specific agent instructions
  specs/                      current backend and frontend behavior specs
  deploy/                     local and production Docker deployment
  docs/                       portfolio and architecture documentation
  agent-runs/                 recorded agent-assisted delivery runs
```

The Angular frontend lives in the sibling repository:

```text
../LearnWordWebApp/lw-app
```

## Architecture

At a high level:

```text
Angular frontend
      |
      v
Ocelot gateway
      |
      +--> IdentityService.WebApi
      |
      +--> LearnWord.Identity
                |
                v
          LearnWord.WebApi
                |
                v
            PostgreSQL
```

The gateway is the public API boundary. `LearnWord.Identity` owns authenticated user-facing learning operations and delegates internal CRUD to `LearnWord.WebApi`.

## Agentic SDLC Experiment

The repository includes an experimental AI-assisted delivery model:

- [System Analyst](agents/system-analyst/AGENT.md): requirements, specs, coordination, final acceptance.
- [Backend Development](agents/backend-dev/AGENT.md): .NET backend implementation and backend-focused fixes.
- [QA Backend](agents/qa-backend/AGENT.md): backend test design, regression coverage, and QA investigations.
- [Frontend UI](agents/frontend-ui/AGENT.md): Angular implementation, responsive UI, accessibility, and browser checks.

Agents work against:

- [Backend API spec](specs/backend-api.md)
- [Frontend behavior spec](specs/frontend-behavior.md)
- [Agent roster](agents/README.md)
- [Deployment docs](deploy/README.md)

Significant work should be captured under `agent-runs/` so the repository shows not only final code, but also the delivery process that produced it.

## Local Run

From this repository:

```bash
cp deploy/env/local.env.example deploy/env/local.env
./deploy/local-up.sh
```

`./deploy/local-up.sh` and `.\deploy\local-up.ps1` pull standard Docker images only when they are missing by default. Set `LW_STANDARD_IMAGE_PULL=never` to skip standard-image pulls entirely, or `LW_STANDARD_IMAGE_PULL=always` to force-refresh them.

Local endpoints:

- frontend: `http://localhost:8088`
- gateway: `http://localhost:5100`
- Mailpit inbox: `http://localhost:8025`

The local environment uses `AiCardGeneration__Provider=Fake` by default. This keeps local runs deterministic and does not require an OpenRouter key.

To test the real LLM provider, edit `deploy/env/local.env`:

```env
LW_AI_PROVIDER=OpenRouter
LW_AI_OPENROUTER_API_KEY=<secret>
LW_AI_OPENROUTER_MODEL=google/gemma-4-26b-a4b-it:free
```

Stop local services:

```bash
./deploy/local-down.sh
```

## Verification

Preferred full local verification:

```bash
./deploy/local-up.sh
```

The local helper uses `LW_STANDARD_IMAGE_PULL=missing` by default and then runs Compose with `--pull never`, so repeated verification runs do not re-check standard images unless requested.

Focused backend regression checks:

```bash
cd LearnWord
./tests/run-all-tests.sh
```

Frontend fallback checks are run from `../LearnWordWebApp/lw-app` when needed:

```bash
npm run build
npm test
```

## Roadmap

- Keep recording baseline and feature delivery agent runs.
- Improve the AI card generator based on real OpenRouter usage and prompt quality.
- Polish spaced repetition review UX and add more learning analytics.
- Add a short demo video showing architecture, agent workflow, local run, and verification.

## Project Guidance

- Current implementation specs live in `specs/`.
- Project agent roles live in `agents/`.
- Start cross-domain or ambiguous work with `agents/system-analyst/AGENT.md`.

## License

This repository is source-available for non-commercial use only. See [LICENSE](LICENSE).
