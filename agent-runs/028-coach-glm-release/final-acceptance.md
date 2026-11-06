# Independent GLM Coach release — 2026-11-06

Accepted locally on `feature/epic-3a-ai-coach`. Coach uses GLM 5.3 Flash, reasoning `low`, 3000 output tokens and a 60-second provider timeout. Card generation keeps its existing Gemma settings. No production deployment, push, main merge or paid model calls.

## Changes

`AiCoachOptions` independently owns Enabled, Provider, OpenRouter model/key/URL/timeout, ReasoningEffort and MaxOutputTokens. Coach providers no longer read AiCardGeneration settings. Host/appsettings/local Compose default to Fake; production Compose defaults to OpenRouter with the selected GLM configuration. Invalid Coach settings produce a sanitized, noncritical Configuration failure before HTTP; they do not prevent application startup or card generation.

Local and production Compose map separate `LW_AI_COACH_*` variables. An optional Coach key takes precedence; otherwise Compose explicitly reuses `LW_AI_OPENROUTER_API_KEY`. This fallback shares only an account credential, not model/provider settings. Actual secret files were not edited. Existing card-generation code and request fields are unchanged.

Request/cache version `coach-v6` separates this release from historical v5 Gemma results. System prompt text and DTO schema 1 remain the tested v5 contract. No migration, historical replay, frontend/API shape change or automatic paid retry. Completed results within a version still survive later configuration edits; this limit is documented.

The evaluation factory now constructs separate Coach options with explicit null effort and 3000 tokens before applying CLI overrides. Benchmark semantics and historical result files remain intact.

## Verification

- `./deploy/local-up.sh`: full local Docker build/startup passed. Log: `/private/tmp/lw-coach-glm-release-local-up.log`.
- Default evaluation wrapper: 18/18 Fake self-check runs passed with v6 provenance. Log: `/private/tmp/lw-coach-glm-release-eval.log`. This is not a new live-model quality result.
- Full Docker runner: **537 passed, 0 failed, 0 skipped across 9 projects**, including 324 WebApi tests and 18 new configuration-isolation cases. Log: `/private/tmp/lw-coach-glm-release-tests.log`. Existing nullable warnings remain outside scope.
- `verify-compose.py`: four read-only checks passed with synthetic secrets. Validates local/production defaults, nested secret fallback and independent key/provider/model overrides without printing rendered configuration. Evidence: `compose-results.json`.

Full test command:

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

Personally executed five authenticated gateway modes using a disposable HTTP stub, separate synthetic credentials and distinct URL paths:

1. Healthy: Coach sends GLM/low/3000/require_parameters; card generation sends Gemma without Coach parameters. A valid lesson can be accepted and completed.
2. Invalid Coach reply: atomic Failed/ai_provider_invalid_response, no partial advice; card drafts still work.
3. Missing Coach key with valid card key: Failed/ai_provider_configuration_error at Configuration, no Coach HTTP call or C# credential fallback; card drafts still work.
4. Coach disabled: Disabled without Coach HTTP; card drafts still work.
5. Coach Fake: deterministic Coach without Coach HTTP; Gemma drafts still work.

All modes verify private ACLs (Coach foreign404, card foreign403, anonymous401), content-free diagnostics, read-only draft preview and unchanged ordinary card/SRS/learner state/history. Stored snapshots remain stable. The initial Disabled fixture incorrectly expected an identical full snapshot: Disabled has no stored analysis, so ReconstructedNow legitimately gets a new capture timestamp. Corrected the test and reran it; application behavior did not change.

Wire evidence contains two Coach requests and six card requests, including the repeated Disabled fixture. It records only model/effort/budget flags, not headers/content/keys. Evidence: `gateway-results.json`, `wire-results.json`. Fixtures were deleted, stub removed and local providers restored to Fake.

After restoration, all six previous Coach gateway groups passed, including practice answers, idempotency and Social fork isolation. All five previous diagnostic groups passed. Logs: `/private/tmp/lw-coach-glm-release-coach-regression.log`, `/private/tmp/lw-coach-glm-release-diag-regression.log`. `git diff --check` passed. No frontend source changed; Docker frontend build ran with the stack, while browser/frontend unit checks were not repeated.

## Handoff

Deploy the reviewed backend image and matching Compose revision through the existing workflow. New production Coach defaults require no change to card-generation variables; the existing account key can be reused. No additional configuration migration or frontend release. See `docs/coach-glm-release.md` for environment variables and verification steps.

After deployment, explicitly analyze a completed session. Expect OpenRouter, `z-ai/glm-5.3-flash`, request/cache `coach-v6`, schema 1. Opening an old diagnostic does not regenerate it.

The selected live benchmark remains 16/18 on a small synthetic suite. Local integration proves wiring and isolation, not perfect reliability or teaching quality. Strict validation and noncritical failure handling remain authoritative. Production has not been deployed or tested by this run.
