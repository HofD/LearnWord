# Automated Coach evaluation — 2026-11-05

Accepted locally. Implemented a standalone automated benchmark instead of requiring manual production study sessions. Existing feature/epic-3a-ai-coach; no production prompt/provider/model/budget/schema/learning-data changes, no migration, frontend change, deployment, push or main merge.

## Delivery

.NET8 console at LearnWord/tools/LearnWord.CoachEval references the actual production OpenRouter provider, prompt/schema and output validator. Six synthetic fixed contexts: successful reviews, one Again without repeated evidence, sixAgain, repeatedAgain RU, same-card multiword grounding and Hard afterfiveAgain. Learner state is computed by actual LearnerStateCalculator; observed counts/source/last outcome/mastery and newest-first recent evidence are consistent. No production user identifiers/content are used.

Valid JSON is scored separately from benchmark behavior. A valid None (even with supportive summary) on repeated-evidence fixtures fails as missed_help; contract errors, unexpected advice, provider errors and evaluation errors are separate outcomes. Production None semantics remain unchanged. Tests include an alwaysNone provider to prove an empty model cannot pass, not just green Fake examples.

Docker wrapper ./LearnWord/tests/run-coach-eval.sh runs without host .NET/database/browser. Default FakeSelfCheck explicitly not model quality. --live plus --model and environment LEARNWORD_COACH_EVAL_API_KEY opt in to real calls. Key never in arguments or loaded from dotenv. Repeat1..5, maximum3 models/60planned sequential calls, no runner retry. Output per-case and per-model summaries, sanitized JSON, explicit incomplete/cancelled verdicts and exit0/1/2/130. Output restricts JSON filename under configurable report directory; reports Git-ignored. No raw responses, words, credentials, provider exceptions or error bodies logged.

## Verification

- Default Docker wrapper:18/18 Fake cases passed, exit0; safe JSON parsed and mode explicitly verified. Evidence self-check-results.json, log /private/tmp/lw-coach-eval-selfcheck-final.log. This verifies harness only, not Gemma.
- Docker wrapper --live --model google/gemma-4-26b-a4b-it --repeat1 with no evaluation key: exit2, configuration rejection before any provider request. Log /private/tmp/lw-coach-eval-live-gate.log. No paid calls made.
- Full Docker script passed439 tests,0 failed,0 skipped across9 projects (WebApi226,23 new evaluation cases). Log /private/tmp/lw-coach-eval-all-tests-final.log; counts in test-results.json. Command below. Existing nullable warnings remain. First full run also passed, then rerun after correcting fixture state to use actual calculator and aligning Hard session summary with production success counting.
- git diff --check and bash -n passed. Frontend untouched; no browser/frontend suite required. No deployed service behavior changed, so standalone harness build+full Docker tests used instead of rebuilding the unchanged application stack.

```sh
docker run --rm --network learnword-local_default \
 -e LEARNWORD_E2E=true -e LEARNWORD_GATEWAY_URL=http://gateway:8080 \
 -e NUGET_FALLBACK_PACKAGES=/nuget-fallback \
 -v /Users/alekseikarpov/.nuget/packages:/nuget-fallback:ro \
 -v /Users/alekseikarpov/GitHub/lw/LearnWord/LearnWord:/src -w /src \
 mcr.microsoft.com/dotnet/sdk:8.0 bash ./tests/run-all-tests.sh
```

## Use

From backend repository root, with evaluation key supplied by shell/secret manager:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model google/gemma-4-26b-a4b-it --repeat 3 --output gemma.json
```

18 automatic calls, no manual grading. Per-case/per-model summary and reports/gemma.json identify abstention, structured grounding or provider problems. Repeated --model compares alternatives on identical fixtures. CLI does not alter production settings. Keep code revision with reports when comparing prompt changes.

## Limits

Live Gemma quality not measured here: evaluation API key absent from current process. Harness self-check and negative scorer tests are not a real-model benchmark result. This catches repeated abstention and contract/evidence/translation-pair errors; it cannot prove semantic prose quality or teaching efficacy. Synthetic contexts do not reproduce exact text of unretained production requests.

## Follow-up: interactive key entry

User requested a clear place to supply the key. Docker wrapper now prompts for hidden input on --live when no key environment variable exists; paste into the terminal prompt, never a script/source file. Existing environment key skips prompting; noninteractive missing key and empty/cancelled input exit2 before Docker/network. Console contract unchanged.

Verified with synthetic key and disposable Docker-command stub (no external request): PTY hidden input not echoed, only environment passes the key, argv excludes key, environment bypass works, empty input and noninteractive absence exit2. bash -n and git diff --check passed. Full backend suite not repeated for this shell/input/documentation-only change; previous439 tests remain last full result.

## User live baseline and progress fix

User completed live Gemma run (18/18, no cancellation, 256.115s total response time). Checked ignored reports/gemma.json, preserved sanitized aggregate evidence in gemma-baseline-summary.json. Results:8pass,1missed_help,7contract_failure,2provider_failure. All6 control runs passed; desired help passed2/12. Contract failures: lesson_has_exercises5, observation_duplicate_card1, json_malformed1. Provider failures: oversized content at3000tokens/finishlength and60s timeout; malformedJSON also finishedlength3000. One repeated-difficulty RU reply validNone. This baseline primarily exposes contract consistency, not universal abstention. No additional paid calls made by root.

The lesson/practice relationship is currently enforced semantically by validator and described in prompt, but flat JSON schema still allows contradictory combinations. Recommended next production change: express intervention alternatives/mutual exclusion in schema and explicitly document one observation per card, retain strict validator, then compare with same automated benchmark. That production change is not included in this progress-only follow-up.

Fixed quiet evaluation: wrapper announces Docker/build and exposes minimal restore/build output; runner emits timestamped [run/total] starting before each provider call and result immediately after each call, including failures. Console flushes each event and shows provider timeout60s, stage/rule/finish/duration. No raw output or credentials in events. Tests prove start event precedes provider execution, result is emitted before next call/cancellation, and failures also emit completion events. Fake wrapper18/18 passed with observed start/result lines. Final Docker test result recorded separately in progress-test-results.json; no production prompt/model/schema changes.
