# Required Coach text bounds — final acceptance, 2026-11-05

Accepted locally on feature/epic-3a-ai-coach. User approved matching mandatory text schema to existing validator after real GLM-v4-low16/18, both failures text_empty at observation explanation. No root paid calls, production deployment, push or main merge.

## Delivered

Text schema minLength1 for mandatory observation evidenceSummary/explanation, intervention title, lesson explanation/examples/tip entries, recognition prompt, options ID/text/correctOptionId and evidence ID strings. Existing maxima retained. Explicit minLength0 on root summary and optional exercise explanation preserves legal empty values. Empty lesson example/tip arrays remain legal. Prompt requires nonblank short mandatory prose; root Intervention summary remains conditionally nonblank in semantic validation while None may leave it empty.

Local Shape now enforces schema minimum with existing text_empty and safe known path. Whitespace/control/grounding checks retained, no response repair or generated fallback. minLength1 excludes literal empty strings only, not whitespace-only strings; no unsupported regex/ifthen claim. Prompt/cache coach-v5, public DTO/schemaVersion1; v4 history preserved without automatic replay. Model/card generator, budgets, reasoning and advisoryNone unchanged.

## Verification

./deploy/local-up.sh passed (log /private/tmp/lw-required-text-local-up.log). Reclaimed only disposable builder cache for low Docker disk; no volumes deleted.

Full Docker script passed505 tests,0 failed,0 skipped across9 projects (WebApi292,30new required-text cases). Log /private/tmp/lw-required-text-tests.log. Existing nullable warnings remain outside scope. Tests cover required-field empties, whitespace guard, missing/null/type, conditional summary, optional exercise explanation, empty arrays, valid one-character grounded content, actual outgoing minima, historicalv4/currentv5cache and prior provider/learner/social regressions.

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

./LearnWord/tests/run-coach-eval.sh passed18/18 FakeSelfCheck undercoach-v5 (log /private/tmp/lw-required-text-selfcheck.log); not a live model result. Original glm-v4-low.json preserved.

Personally executed12 actual authenticated gateway cases using synthetic local HTTP stub and independent Ajv validation of exact outgoing request schema. Zero-length mandatory explanation/evidence/title/lesson/example/tip/option rejected by schema and API; valid lesson with empty arrays, optional empty exercise explanation and empty None summary accepted. Whitespace observation and empty Intervention summary are schema-valid but semantically rejected, explicitly documenting remaining contextual guards rather than weakening them. Same safe text_empty/knownpaths, no failed partialinterventions, no source/translation/key in diagnostics, anonymous401/foreign404, repeatedGETstable, ordinary learner/card/history state unchanged. Fixtures deleted, stub removed and WebApi restored to Fake. Evidence gateway-results.json/wire-schema-results.json; log /private/tmp/lw-required-text-ajv.log.

No frontend changes; no browser/frontend suite rerun required. git diff --check passed. No new diagnostic fields or migration.

## Next user run

From backend repository root:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model z-ai/glm-5.3-flash --reasoning-effort low --repeat 3 --output glm-v5-low.json
```

Hidden key prompt/progress unchanged. Expect Prompt=coach-v5 MaxOutputTokens=3000 ReasoningEffort=low;18sequential calls and new reports/glm-v5-low.json. Compare to16/18 v4low baseline. This standalone evaluation needs updated source, not production deployment. Actual GLM improvement remains unmeasured until new live run; neither schema hints nor minLength guarantee valid/nonblank replies at every provider. Strict local validation remains authoritative.
