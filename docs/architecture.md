# Architecture

LearnWord is a fullstack language-learning product with a separated Angular frontend, public gateway, identity service, authenticated learning facade, internal CRUD API, and PostgreSQL persistence.

## Runtime Topology

```text
Angular frontend
      |
      | /api/...
      v
LearnWord.Gateway
      |
      +--> IdentityService.WebApi
      |       - registration
      |       - login
      |       - refresh token
      |       - email confirmation
      |       - password reset
      |
      +--> LearnWord.Identity
              - authenticated collections
              - authenticated cards
              - authenticated words
              - user ownership checks
              |
              v
        LearnWord.WebApi
              - internal collection CRUD
              - internal card CRUD
              - internal word CRUD
              |
              v
          PostgreSQL
```

The gateway is the public API boundary. Frontend calls use `/api/...`; internal service routes remain behind the Docker network.

## Backend Services

- `LearnWord.Gateway` exposes the public gateway through Ocelot.
- `IdentityService.WebApi` owns account and authentication flows.
- `LearnWord.Identity` is the authenticated facade for user-owned learning data.
- `LearnWord.WebApi` is the internal CRUD service for collections, cards, and words.
- `LearnWord.BL`, `LearnWord.DAL`, and related model projects hold business and persistence layers.

## Frontend

The Angular frontend lives in the sibling repository:

```text
../LearnWordWebApp/lw-app
```

Current user-facing flows include:

- home and about pages;
- registration;
- email confirmation;
- login and token refresh;
- password recovery;
- collection list and collection details;
- card and word editing;
- review flow.

## Source Of Truth

Current behavior is documented in:

- `specs/backend-api.md`
- `specs/frontend-behavior.md`

These specs are descriptive. They document current behavior, including known gaps, and should move with intentional behavior changes.

## Verification Surface

The preferred verification surface is the local Docker environment:

```bash
./deploy/local-up.sh
```

The helper scripts use `LW_STANDARD_IMAGE_PULL=missing` by default to avoid re-pulling standard Docker images on repeat runs. Set it to `never` for offline/no-network checks or `always` to deliberately refresh base images.

The local stack exposes:

- frontend: `http://localhost:8088`
- gateway: `http://localhost:5100`
- Mailpit: `http://localhost:8025`

Focused backend regression checks run through:

```bash
cd LearnWord
./tests/run-all-tests.sh
```

## AI Card Generation

The first portfolio-grade AI feature is an AI card generator:

```text
User source text
      |
      v
POST /api/collections/{id}/ai/generate-cards
      |
      v
LearnWord.Identity ownership facade
      |
      v
LearnWord.WebApi
      |
      +--> provider abstraction: Fake or OpenRouter
      +--> structured output parsing
      +--> validation and safety limits
      +--> draft card suggestions
      |
      v
User accepts selected cards
```

`LearnWord.Identity` stays a lightweight authenticated facade: it checks ownership and proxies the valid request. LLM-specific behavior stays in `LearnWord.WebApi`.

The local default provider is `Fake`. OpenRouter can be enabled through environment variables without changing frontend code. The recommended starter model is `google/gemma-4-26b-a4b-it:free`, with the model kept configurable because free-model availability and quality can change.

This feature is intentionally suited to the agentic workflow because it touches specs, backend contract design, provider abstraction, frontend UX, validation, tests, and final acceptance.
