# Coach prompt v2 clarification — 2026-10-29

Reported issue: a valid None result justified no intervention by missing exact wrong choices/cognitive-load evidence despite repeated Again/Hard. UI hiding None remains intentional.

Changed: coach-v2 explicitly permits evidence-backed optional meaning recap/practice without guessed confusion or cognitive diagnosis. Two Again plus one Hard on Weak is an eligible example, not mandatory intervention. None remains valid when no useful supported action exists. Summary should be one or two learner-facing sentences, without numeric mastery/internal labels/psychological speculation.

Version safety: CoachService explicitly persists active prompt/schema version on new records and disabled DTO. Session/prompt cache separates old v1 from v2; old analysis is preserved. New request generates at most once for v2 then reuses cache. No automatic batch regeneration or historical mutation.

Verification: ./deploy/local-up.sh passed. Full Docker SDK ./tests/run-all-tests.sh: 339 passed, 0 failed, 0 skipped across9 projects (WebApi128). New provider request-contract test uses the reported two Again/one Hard/Weak/.21 scenario without wrong answers and validates an allowed grounded lesson; this uses deterministic HTTP handler and does not claim live model behavior. New persistence test proves v1 None remains historical, v2 new ID/version and cached subsequent request. python3 agent-runs/019-ai-coach/verify-coach.py: all6 real gateway groups passed with local Fake provider. No frontend behavior or deterministic learner rules changed; no live LLM call performed. Logs: /private/tmp/learnword-coach-v2-tests.log, learnword-coach-v2-local-up.log, learnword-coach-v2-gateway.log.

Saved as a follow-up backend commit on feature/epic-3a-ai-coach. Main is unchanged; no push or production rollout.
