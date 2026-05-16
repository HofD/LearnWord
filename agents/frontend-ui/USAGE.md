# Using The Frontend UI Agent

Use this agent when you want Angular frontend implementation, mobile-first UI polish, responsive layout work, accessibility improvements, or a focused review of LearnWord screens.

## How To Prompt It

Point the agent at `agents/frontend-ui/AGENT.md` from the backend repository root and give it a focused frontend task.

Good examples:

```text
Use agents/frontend-ui/AGENT.md.
Redesign the collections screen for mobile first. Preserve current API behavior and run a production build.
```

```text
Use agents/frontend-ui/AGENT.md.
Make the review flow easier to use on phones and keep the desktop card centered.
```

```text
Use agents/frontend-ui/AGENT.md.
Review the login and register screens for accessibility and propose changes. Do not edit code yet.
```

## Expected Inputs

Give the agent:

- the target screen or flow: home, auth, collections, cards, words, review, header, or app shell;
- whether it should only review/propose or also edit code;
- whether behavior changes are allowed or only visual/UX changes;
- whether local browser verification is required.

If you do not specify a viewport priority, the agent should assume mobile first and center the desktop layout.

## Expected Outputs

For implementation tasks, expect:

- updated Angular templates, styles, and component code when needed;
- preserved current API and auth behavior unless behavior changes were requested;
- the exact Docker/local browser checks it ran, plus any fallback build or test commands;
- a short note about remaining risks.

For analysis tasks, expect:

- prioritized UI/UX findings;
- affected files or components;
- suggested implementation order;
- any behavior that conflicts with `specs/frontend-behavior.md`.

## Repository Conventions

Preferred local Docker run:

```bash
./deploy/local-up.sh
```

Use Docker as the default build and visual verification path, then inspect `http://localhost:8088`.

Frontend app fallback:

```bash
cd ../LearnWordWebApp/lw-app
npm run build
```

Development server:

```bash
cd ../LearnWordWebApp/lw-app
npm start
```

Current frontend contract:

```text
specs/frontend-behavior.md
```

## Guardrails

The agent should not:

- migrate away from Angular, RxJS, TypeScript, or Bootstrap unless explicitly asked;
- add a new UI framework or heavy dependency without approval;
- change documented frontend behavior while doing visual work unless explicitly asked;
- optimize desktop at the expense of mobile usability;
- stretch app screens across wide desktop viewports;
- skip saying which checks were or were not run.
