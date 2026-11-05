# Automated Coach behavior test

Run from the backend repository root (the directory containing deploy/, LearnWord/ and this docs/). Requires Docker; no browser, test learner account, database exports or installed host .NET SDK. The harness references the actual current OpenRouterAiCoachProvider, SystemPrompt/schema and CoachOutputValidator. Production code and learning state are not modified.

## One-command harness self-check

```sh
./LearnWord/tests/run-coach-eval.sh
```

Default mode FakeSelfCheck makes no network model call. It checks the complete fixture/scorer/report pipeline (6 cases ×3 repeats=18 runs). A pass here is not a Gemma result. The script uses .NET SDK8 in Docker and optionally mounts an existing host NuGet package cache read-only; package restoration may need network when dependencies are missing.

## Actual model test

Run the command below. In an interactive terminal, the script asks `OpenRouter API key (hidden):`; paste your key and press Enter. Characters are not displayed and the key is not saved to a file or placed in command history. An existing LEARNWORD_COACH_EVAL_API_KEY environment variable skips the prompt (use that for CI):

```sh
./LearnWord/tests/run-coach-eval.sh --live --model google/gemma-4-26b-a4b-it --repeat 3 --output gemma.json
```

This makes18 sequential OpenRouter calls with the current production coach-v4 prompt, schema and3000 output-token cap. --live is explicit opt-in to API usage/billing. No key is accepted in arguments or loaded automatically from dotenv/config files. Noninteractive execution without an environment key exits2 before Docker starts. Empty/cancelled interactive input also exits2 without requests. The console rejects a missing required --model before any model request. Do not paste a key into a command or commit it. Nothing is sent to the LearnWord production database or an actual user's session.

To compare models, repeat --model with the exact OpenRouter model IDs. The same contexts/repetitions are used for every model. Repeats1..5, at most3 models, at most60 planned calls; configurations exceeding bounds fail before requests. No automatic retries, parallel calls or model fallback. Pin the code revision when comparing results across prompt changes; this harness always follows the checked-out production prompt, rather than carrying a second diverging prompt. The evaluation runner itself never edits the production prompt.

## Automatic verdict

Fixed synthetic contexts, stable IDs/timestamps, EN/RU and a same-card multiword case. Observed learner states are computed using production LearnerStateCalculator, with consistent counters/source/latest outcomes and newest-first recent evidence:

- Successful reviews: expect None.
- Single Again with no repeated evidence: expect None.
- Six Again, repeated Again in Russian, repeated Again on a multiword card, and Hard after five Again: expect grounded help.

The production API still permits None. Evaluation expectations are a benchmark policy for helpfulness, not a change to API validation.

Every reply is validated by the real output validator first. A None with a supportive summary still counts as missed_help in a repeated-evidence case. A summary alone is not an intervention. Outcomes distinguish pass, missed_help, unexpected_help, contract_failure (malformed JSON, unsupported evidence/references, incorrect source-word/translation grounding or bounds), provider_failure and evaluation_error. The automated verdict does not prove prose translation quality or teaching effectiveness; it tests abstention behavior, recorded grounding and existing validation rules.

Exit0 means every planned run passed; exit1 means benchmark/provider/contract failure or incomplete evaluation; exit2 means configuration/report failure; cancellation exits130. Failures are not dropped from denominators or hidden by valid JSON. This can be called from CI; ordinary unit tests never call OpenRouter.

## Report

The script first prints Docker/build startup and restore status, then a timestamped [n/total] starting line before each request (provider timeout60s), and its verdict immediately on completion, followed by aggregate counts. No need to wait until all requests complete for progress. The script prints per-case outcomes and aggregate counts, and writes sanitized JSON to agent-runs/023-coach-behavior-eval/reports/ (default coach-eval-report.json; --output accepts a JSON filename). Override the host report directory with LEARNWORD_COACH_EVAL_REPORT_DIR. Results include mode, prompt version, model/case/repeat, planned/completed runs, contract/behavior result, timing, safe validation code/path and allowlisted provider finish/token metadata. No key, raw response, prompt/card text or provider error body is written. Local reports are ignored by Git; reviewed delivery evidence is stored separately.

Keep mode, code revision and result JSON with a comparison. Never present Fake results as real model quality. If interrupted, use the cancelled/incomplete verdict; do not treat partial successes as a pass. Downloading an old production diagnostic is not needed for this test.


The v3 Gemma baseline completed18 runs with8passes (all6 controls,2/12 repeated-difficulty cases),5 lesson/practice mix errors,1 duplicate-card observation,1 malformed JSON,1 oversized content and1 timeout, plus1 missed_help. This is recorded as bounded evidence under agent-runs/023-coach-behavior-eval/gemma-baseline-summary.json, not a general model ranking. After the v4 schema alignment, rerun the same live command with `--output gemma-v4.json`; it should print Prompt=coach-v4. Keep the previous gemma.json for comparison. No new key setup or manual study sessions are needed.

## Controlled reasoning effort comparison

The GLM-v4 user baseline passed11/18. Three failures hit the3000-token output cap while spending all or nearly all tokens on reasoning. For a controlled follow-up, request lower reasoning effort and keep the same promptv4, schema, fixtures, timeout and3000-token cap:

```sh
./LearnWord/tests/run-coach-eval.sh --live --model z-ai/glm-5.3-flash --reasoning-effort low --repeat 3 --output glm-v4-low.json
```

The wrapper prompts for a hidden API key when needed, then makes18 sequential calls with immediate progress. It writes reports/glm-v4-low.json alongside the existing reports/glm-v4.json. No production model/configuration or card-generation setting is changed. The runner still uses the production provider/prompt/schema/validator; an evaluation-only HTTP adapter adds only reasoning.effort and rechecks the final request size. No JSON repair, response filtering or token-limit increase.

Allowed explicit values are low, high and max. The selected effort applies to every model passed in the command; choose models supporting that effort. Omit the flag to preserve the original request with no reasoning property and provider-default behavior. Explicit reasoning in Fake mode is rejected rather than implying it exercised an external request. Report/console record the requested reasoningEffort and maxOutputTokens3000; missing/null effort on old/default reports means unspecified, not reasoning disabled. Requested settings are not upstream confirmation: inspect observed reasoningTokens and finishReason too.

GLM-5.3-Flash thinking is mandatory, with low/high/max effort controls. Low is not a hard token allocation or a promise that every case finishes within3000 tokens. [Z.ai thinking documentation](https://docs.z.ai/guides/capabilities/thinking), [model-author notes](https://huggingface.co/zai-org/GLM-5.3-Flash). Reasoning counts against the same output cap; excluding reasoning text does not free the token budget. [OpenRouter reasoning documentation](https://openrouter.ai/docs/guides/best-practices/reasoning-tokens).

Compare the new report with the11/18 default baseline, including three length-related failures, duplicate observation, source-word prompt mismatch, empty explanation and one missed_help. A lower effort is a hypothesis, not a verified fix; structural/behavioral passing still does not establish full teaching quality. No real-model result is inferred from offline request-capture tests.
