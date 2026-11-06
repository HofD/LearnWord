# Evaluation output cap — final acceptance, 2026-11-06

Scope completed locally on feature/epic-3a-ai-coach: user approved one final controlled DeepSeek comparison with5000 output tokens. No production configuration/provider/prompt/model/card-generator change, deployment, push or main merge. Actual5000-token model result remains pending user key entry; root environment has no evaluation key and no paid requests were sent.

## Delivered

Live CLI --max-output-tokens3000|5000, default3000. Invalid/missing/duplicate/unsupported and explicitFake budget flags rejected before network. Optional API parameters preserve current callers. Default no-effort3000 request remains untouched;5000 alone changes max_tokens only and does not add reasoning;5000+low changes exactly those two requested fields. Existing eval-only adapter preserves auth/cancellation/disposal/request-size checks, existing production promptv5/schema/fixtures, response-size bounds,60s timeout/call caps/scoring.

Console/report show selected maxOutputTokens5000 instead of old fixed3000; historical reports/defaults remain3000. Hidden key entry/progress/report naming unchanged. No claim that raising maximum forces usage or resolves semantic errors.

## Verification

Default Docker wrapper18/18 FakeSelfCheck passed (log /private/tmp/lw-eval-budget-selfcheck.log). It is not a real DeepSeek test. Initial wrapper pulled SDK image as required; no host dotnet used.

Full Docker runner passed519 tests,0 failed,0 skipped across9projects (WebApi306). Counts in test-results.json, log /private/tmp/lw-eval-budget-tests.log; command below. New14 tests cover real production-provider request capture/deepdiff, budget-only vs combined override, unchanged auth/model/prompt/schema, CLI/core guards, report attribution/verdict, request bound and cancellation. Tests use synthetic keys/custom HTTP handlers, no external calls.

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

Actual CLI help checked for new flag. git diff --check passed. Frontend/application untouched, so no deployment/browser/frontend rerun required; Docker harness build and full regression runner used instead of rebuilding unchanged services. Existing nullable warnings outside scope remain.

## One final live run

From backend repository root:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model deepseek/deepseek-v4.1-flash-1017 --reasoning-effort low --max-output-tokens 5000 --repeat 3 --output deepseek-v5-low-5000.json
```

Key prompted hidden; expect Prompt=coach-v5 MaxOutputTokens=5000 ReasoningEffort=low and18sequential calls. New file preserves prior DeepSeek14/18at3000 andGLM16/18at3000 (comparison-baselines.json). Inspect whether three length/empty failures disappear, remaining contract failures, latency and measured token usage. This is a configuration comparison with a different budget, not a same-budget model ranking. Do not keep changing the prompt after individual failures; select the best tested candidate using these results and label the small synthetic sample honestly. Reference price estimates are not actual billing or a universal price ceiling.

## Completed user live run and final candidate selection

User supplied completed DeepSeek5000report (18/18executed,notcancelled,promptv5,low,5000). Result16/18 vs14/18at3000;3empty-provider failures disappeared, but1length-ended malformedJSON remains and1duplicate intervention appears. Total response time239.366s, median10.3755s;completion36095tokens including29152reasoning. No further paid calls initiated by root.

Compare GLM-v5-low3000:16/18,0length/provider failures,total134.77s,median6.7815s,completion12423 including5749reasoning. Relevant repeated-difficulty cases:GLM11/12 vsDeepSeek5000 10/12. Same aggregate pass count with lower latency/token consumption favors GLM for this project. Select z-ai/glm-5.3-flash,requestedreasoninglow,max_tokens3000,coach-v5 as the best tested baseline candidate. Stop this prompt/budget tuning series; no further automatic reruns.

Recorded safe aggregates/failure identifiers in final-model-comparison.json; original ignored reports preserved. Requested/observed token counts do not determine exact billing because provider prices/caching/cost were not captured. This is a small synthetic comparison and both configurations still have2contract failures; it is not a claim of perfect reliability or linguistic teaching quality. Strict output validation remains necessary. Production application/card generator configuration was not changed or deployed; choosing/separating Coach configuration is a subsequent implementation task.

Verification for this documentation/evidence update: JSON source metadata/counts checked, git diff --check passed. No code changed, no additional tests/build/paid calls needed;519 remains latest full-code test count.
