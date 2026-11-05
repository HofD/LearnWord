# Evaluation reasoning control — final acceptance, 2026-11-05

Accepted locally. User authorized a controlled GLM low-effort test after default-effort baseline11/18 and three length-related failures. Existing feature/epic-3a-ai-coach; no main merge, push, production deployment or root paid calls.

## Delivered

Standalone evaluation CLI accepts --reasoning-effort low|high|max in live mode only. Omitted flag retains the exact original production request with no reasoning property; null means provider-default/unspecified, not off. Explicit Fake+effort, unsupported/duplicate/missing arguments rejected before requests. Existing hidden-key shell prompt, call caps, progress, fixtures, cancellation, scoring, output names and safe reports remain.

EvaluationHttp wraps the production provider's HttpClient only in this standalone runner. It injects reasoning:{effort:...}, disposes replaced content, preserves headers/cancellation, and rechecks the final24000-character request cap. No other field is modified. Production application provider/configuration, model selection/card generator, promptv4/schema1,60s timeout and3000 output-token limit unchanged. Report and console record requested ReasoningEffort and read-only MaxOutputTokens3000; these are not upstream confirmation of the setting.

## Verification

- ./LearnWord/tests/run-coach-eval.sh:18/18 FakeSelfCheck passed. Parsed actual report: reasoningEffort:null, maxOutputTokens3000, modeFakeSelfCheck. Log /private/tmp/lw-eval-reasoning-selfcheck.log. No inference about GLM from Fake.
- Full Docker script:475 passed,0 failed,0 skipped across9 projects (WebApi262;13new cases). Log /private/tmp/lw-eval-reasoning-tests.log. Existing nullable warnings outside scope remain.
- New actual-provider HTTP capture compares before/after request JSON: removing reasoning leaves deeply equal body, same system prompt/model/schema/max_tokens3000/auth/contenttype and one underlying call. Also tests post-injection size cap, no network on rejection, cancellation and report metadata/default attribution. Uses synthetic key/handler, no OpenRouter call.
- Docker CLI --help exposes reasoning flag. bash syntax and git diff --check passed. No frontend/application source changed; standalone Docker build plus full runner used without rebuilding/deploying unchanged services or rerunning browser suite.

Full test command:

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

## Next live run

From backend repository root:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model z-ai/glm-5.3-flash --reasoning-effort low --repeat 3 --output glm-v4-low.json
```

18sequential calls; key prompted hidden when needed. Startup should show Prompt=coach-v4 MaxOutputTokens=3000 ReasoningEffort=low. New file reports/glm-v4-low.json preserves existing glm-v4.json baseline. Lower effort does not disable mandatory thinking or guarantee a hard reasoning token cap. Actual improvement remains unmeasured until this user-authorized live run; root ran only offline tests/request captures. Compare observed token counts/finish reasons and behavioral failures, not only requested option.
