# Intervention schema alignment — final acceptance, 2026-11-05

Accepted locally on feature/epic-3a-ai-coach. User approved repairing the mismatch exposed by actual Gemma baseline: schema allowed mixed lessons/practice, local validator rejected it. No paid/live model call, production deployment, push or main merge.

## Delivered

- Actual outgoing interventions.items.anyOf selects Explanation/MicroLesson with nonnull lesson + empty exercises, versus ExtraPractice with null lesson +1..4 exercises. Same public DTO fields, strict/additionalPropertiesfalse/required/bounds/grounding retained. Shared exercise shape is reused; no response repair, normalization or weaker acceptance.
- Local structural traversal safely selects the known intervention type branch, supports explicit null and rejects missing/unknown/wrong discriminator safely. Established lesson_has_exercises, lesson_required, practice_has_lesson and practice_requires_exercises codes/paths preserved. Unknown properties cannot enter paths.
- Prompt and schema descriptions explicitly require at most one observation per card, choose one grounded type and combine supported reasons. Lesson and practice may be separate interventions for the same card. Cross-item property uniqueness still checked by semantic validator, not falsely claimed from uniqueItems.
- Prompt/cache version coach-v4; DTO/schemaVersion remains1. New explicit analysis uses v4 cache; v3 records remain historical with no automatic replay. Existing model, card generator, reasoning and3000-token budgets unchanged.

## Verification

./deploy/local-up.sh passed (log /private/tmp/lw-intervention-schema-local-up.log). Reclaimed only old disposable Docker builder cache due limited disk space; no volumes deleted.

Default ./LearnWord/tests/run-coach-eval.sh passed18/18 Fake self-check runs under promptv4 (log /private/tmp/lw-intervention-schema-eval.log). This verifies harness/contract compatibility, not model quality. Original gemma.json preserved.

Full Docker test runner passed462 tests,0 failed,0 skipped across9 projects (WebApi249,21 new schema cases). Log /private/tmp/lw-intervention-schema-tests.log. Same Docker command used as prior runs:

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

Personally executed10 gateway cases using local HTTP stub with dummy key and Ajv from existing frontend-test Docker image. Stub independently validates the exact schema in actual provider request and its intended valid/invalid reply variants; it does not reuse local Shape traversal. Valid Explanation, MicroLesson, ExtraPractice andNone accepted. Mixed lesson+exercise, practice+lesson, null lesson, empty practice, unknown type rejected by independent schema and local API. Duplicate-card observation remains schema-valid but semantic-validator rejected, explicitly showing uniqueness limitation. Stable/private owner GET, anonymous401/foreign404, no source/translation/key in diagnostics, no partial failed interventions, no normal card/history/learner-state mutation. Fixtures deleted, stub removed and WebApi restored to Fake. Evidence gateway-results.json and wire-schema-results.json; log /private/tmp/lw-intervention-schema-ajv.log.

Tests additionally cover valid separate same-card interventions, malformed/missing/unknown discriminator, hostile keys/duplicate JSON property, practice bounds and v3 historical/v4 current cache behavior. Existing malformed/evidence/grounding/request/card generator regressions remain green. git diff --check passed. No frontend source changed, no browser/frontend suite rerun required.

## Next user action

From backend repository root, run the same live evaluation with a new output filename:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model google/gemma-4-26b-a4b-it --repeat 3 --output gemma-v4.json
```

Wrapper prompts for hidden key when needed. Expect Prompt=coach-v4 and18 sequential requests with immediate progress. Report under agent-runs/023-coach-behavior-eval/reports/gemma-v4.json, preserving v3 gemma.json. No production deployment needed for standalone evaluation of current source. Production API behavior requires normal reviewed backend deployment; no new migration.

Real Gemma improvement remains unmeasured until this new run. Schema support/enforcement varies by OpenRouter endpoint, so local independent validation is not a guarantee of provider compliance. None, malformed output, length/timeout and semantic quality issues can still occur; the unchanged harness will expose them rather than mask them.
