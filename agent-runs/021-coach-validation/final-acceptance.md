# Coach validation diagnostics — final acceptance, 2026-11-02

Accepted locally on feature/epic-3a-ai-coach. Implements the user's points1+2 only. Gemma/model selection, card generator, reasoning and token budgets are unchanged. No production deployment, paid provider call, push or main merge.

## Delivered

- Nullable analysis.failureStage, validationCode, validationPath and providerHttpStatus in the existing stored diagnostic snapshot. Exact public error codes and UpstreamServiceException type preserved; no new migration. Old snapshots deserialize new fields as null.
- Known schema traversal identifies malformed/missing/type/enum/duplicate/unknown/limit failures safely. Unknown model property names map to containing known objects; arrays are bounded before indexing. Semantic failures separately identify evidence gates/references, card revision, lesson/practice alternatives and exact word/paired translation mismatches. No raw output, exception path/message, values or credentials in metadata/logs.
- Coach-only provider.require_parameters=true, exact source-word exercise prompts and paired translations clarified in prompt/schema, compatible bounds described; prompt coach-v3, provider schema1. None remains allowed. New explicit analyses use v3 cache; historical records remain; no automatic replay.
- Existing diagnostics page displays the additional JSON fields without frontend changes. Failure snapshots are fenced and retry snapshots clear stale metadata.

## Verification

`./deploy/local-up.sh`: passed; local Docker services and existing migrations started successfully. Log /private/tmp/lw-validation-local-up.log. Disposable old builder cache reclaimed because Docker disk was98% full; no volumes deleted.

Full backend runner:

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

392 passed,0 failed,0 skipped across9 projects (WebApi179). Log /private/tmp/lw-validation-all-tests.log. Includes exact safe paths, hostile property names, bounded indexes, paired translations, stored/retry metadata, legacy compatibility, unchanged card-generation routing and v3 caching. Existing nullable warning in CoachService's follow-up mapping remains outside this scope.

Personally executed authenticated gateway test `python3 agent-runs/021-coach-validation/verify-validation.py` with disposable HTTP stub and local dummy key, not OpenRouter. Stub refuses requests without require_parameters=true, then returns a valid structured exercise except a deliberately question-shaped prompt. Analysis failed with old public ai_provider_invalid_response and new OutputValidation / exercise_prompt_mismatch / $.interventions[0].exercises[0].prompt. Repeated private reads stable; anonymous401 and foreign404; no marker content, translation, solution or dummy key in export; no partial interventions; learner state/history/card snapshot unchanged. See gateway-results.json. Test collection deleted, stub removed and WebApi restored to local Fake provider (verified container setting).

After restoration, all5 previous diagnostics gateway checks and all6 Coach regression groups passed, including Social/fork isolation and unchanged SRS/evidence. Logs /private/tmp/lw-validation-diagnostics-regression.log and /private/tmp/lw-validation-coach-regression.log. No frontend source changed, so browser/frontend unit suite was not rerun. git diff --check passed.

## Production follow-up

Deploy this backend revision through the normal review/deployment workflow. No additional migration beyond the existing diagnostics migration. Explicitly analyze a newly completed session (or explicitly request analysis for an older session under v3), then download its JSON from /coach-diagnostics/{sessionId}. Confirm promptVersion coach-v3. For failures inspect analysis.failureStage, validationCode, validationPath and providerHttpStatus. The historical output was not retained: its exact failure cannot be reconstructed, and the controlled prompt-mismatch test is not evidence that production failed for that reason. Structured routing still does not guarantee semantic correctness or a non-None recommendation.
