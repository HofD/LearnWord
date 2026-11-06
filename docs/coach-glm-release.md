# GLM Coach release

Prepared backend review version on feature/epic-3a-ai-coach. Coach uses independent configuration, while card generation keeps the existing provider/model settings. Selected Coach: z-ai/glm-5.3-flash, reasoning low, max output 3000, timeout 60s. Production Compose defaults to this selection; local Compose and host appsettings use Fake unless explicitly overridden. No production deployment is performed by preparing this revision.

## Server settings

Keep current LW_AI_PROVIDER and LW_AI_OPENROUTER_* variables for card generation unchanged. The new Coach variables are:

```dotenv
LW_AI_COACH_ENABLED=true
LW_AI_COACH_PROVIDER=OpenRouter
LW_AI_COACH_OPENROUTER_MODEL=z-ai/glm-5.3-flash
LW_AI_COACH_REASONING_EFFORT=low
LW_AI_COACH_MAX_OUTPUT_TOKENS=3000
LW_AI_COACH_OPENROUTER_TIMEOUT_SECONDS=60
```

Production Compose supplies these defaults, so existing server env files need no extra variables when these values are desired. Optional LW_AI_COACH_OPENROUTER_BASE_URL defaults to https://openrouter.ai/api/v1/chat/completions. Optional LW_AI_COACH_OPENROUTER_API_KEY is a separate Coach credential; empty/unset explicitly falls back to existing LW_AI_OPENROUTER_API_KEY in Compose. Reuse is just the same OpenRouter account key; model/provider settings do not inherit from card generation. No application-level fallback reads the card key. Secret values must stay in normal server env/secret storage, never Git or chat.

When running the application without these Compose files, configure the AiCoach section directly (e.g. AiCoach__Provider, AiCoach__OpenRouter__ApiKey, AiCoach__OpenRouter__Model, AiCoach__ReasoningEffort, AiCoach__MaxOutputTokens). Existing AiCardGeneration keys alone no longer select/configure Coach. Missing/invalid Coach configuration fails only Coach analysis, not application startup or card generation.

## Deploy and verify

Use the existing reviewed backend image/Compose deployment workflow after review; rebuild the backend image and update the matching Compose revision together. No new configuration database migration; previous Coach/diagnostics migrations remain part of normal deployment. Frontend API shape is unchanged and no new frontend build is required for this configuration split.

Explicitly analyze a newly completed session (or request a new version analysis for an older completed session). Expected analysis provenance: provider OpenRouter, model z-ai/glm-5.3-flash, promptVersion coach-v6, schemaVersion 1. Diagnostics should show captured generation/status and available provider token/finish metadata. New request/cache version prevents reusing a historical v5 Gemma analysis for the release; actual prompt text and structured result schema stay the tested v5 version. GET/download does not rerun analysis. Completed results within one version remain cached even if later environment settings change; do not expect a config edit to retroactively replace them.

For a real card draft, continue using the existing generation flow: it must still use your current Gemma model. Separate local gateway acceptance checks both actual wire requests with synthetic keys/HTTP stubs, including a valid Coach lesson, invalid response, missing Coach key, disabled Coach and Fake Coach while Gemma drafts remain available. This is integration proof, not an additional live-model quality benchmark.

## Controls and limits

LW_AI_COACH_ENABLED=false disables Coach only. LW_AI_COACH_PROVIDER=Fake makes deterministic offline suggestions. Neither changes card generation or normal SRS/learner history. To roll back the Coach model/provider, change only its own settings for subsequent analyses; existing historical records remain. No model fallback, response repair, automatic paid reanalysis or forced intervention is introduced.

GLM/low/3000 was the best tested project configuration at16/18 on the small synthetic benchmark. Strict validation remains required: residual invalid advice stays Failed and is not persisted as an intervention. This is not a claim of perfect reliability, prose translation quality or pedagogical effectiveness. Production settings are prepared here; actual live deployment/observation remains the normal next operational step.
