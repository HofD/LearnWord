# Coach response metadata — final acceptance, 2026-11-02

Accepted locally. Scope: user approved missing response termination/usage, safe output-format flags and JSON parser coordinates after real coach-v3 json_malformed. Work stays on feature/epic-3a-ai-coach; no paid calls, production changes, push or main merge. Exact historical failure remains unknown because its response metadata was not retained.

## Delivered

Nullable providerResponse with HTTP status, normalized allowlisted finishReason, independently validated integer prompt/completion/total/reasoning token counts. Unknown strings become Other, missing/invalid numeric data stays null. No native finish string, provider errors/prose/cost/account data or raw generated reasoning. Metadata is immutable per invocation, preserved on successful and rejected output, and where safely available on provider content/envelope errors. Existing Task<string> provider API remains compatible via default AnalyzeWithDiagnostics; Configured forwards metadata without shared mutable response state.

Nullable outputFormat has six whitespace-trimmed object/array/fence boundary flags, not a claim that output is valid/complete. JSON parse failures preserve bounded zero-based jsonLineNumber/jsonBytePositionInLine (UTF-8 bytes within line) without exception text/path/content. Retry clears stale fields, old snapshots return null additions, and existing generation fences remain. Numeric/allowlisted metadata included in safe logs. Uses existing DiagnosticJson, no migration/frontend changes.

Gemma/model, prompt coach-v3/schema1, token/reasoning budgets, card generation, public error codes and strict acceptance are unchanged. No JSON repair or automatic historical replay.

## Verification

Docker ./deploy/local-up.sh passed. Log /private/tmp/lw-response-local-up.log. Existing nullable warnings remain outside this scope.

Full backend command (read-only host package fallback avoids failing NuGet network download; execution is entirely Docker):

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

Initial uncached run was stopped during first project restore after NuGet returned unexpected EOF for Microsoft.CodeCoverage17.8.0; it was not a code/test failure. Log /private/tmp/lw-response-all-tests.log. Final cached run passed416 tests,0 failed,0 skipped across9 projects (WebApi203 including24 new cases). Log /private/tmp/lw-response-all-tests-cached.log; counts recorded in test-results.json.

Personally executed authenticated gateway acceptance with disposable local Node HTTP stub, dummy key and real OpenRouter code path. All5 cases passed: fenced JSON, unfinished JSON with finish_reason=length, text prefix, multiline syntax error, valid None. Exports retained HTTP200, exact synthetic finish/usage; syntax failures retained numeric positions (fenced/prefix0:0, unfinished0:106, syntax4:17); valid reply had null parser coordinates. All cases enforced 401 anonymous/404 foreign, stable repeated GET, no source/translation/key/output snippets in export, no partial interventions or learner/card/history changes. See provider-stub.js, verify-response.py and gateway-results.json. Fixtures deleted, stub removed and local Fake provider restored.

All5 previous diagnostics gateway checks also passed after restoration (including edit/history persistence, private read-only behavior). Log /private/tmp/lw-response-diagnostics-regression.log. No frontend source changed, so no additional browser/frontend suite. git diff --check passed.

## Next production check

Deploy backend review revision through normal workflow. No new migration. Explicitly retry the failed analysis if its existing retry cap allows, or finish a new session and analyze it. Download diagnostics again; merely opening the diagnostics page does not rerun the model. New fields appear on new generations; older snapshots cannot acquire them retroactively. Inspect providerResponse.finishReason/usage, outputFormat and analysis.jsonLineNumber/jsonBytePositionInLine. Snapshot.truncated is still storage trimming only. Provider-reported length termination with usage is evidence to investigate the budget; stop alone does not prove valid JSON.
