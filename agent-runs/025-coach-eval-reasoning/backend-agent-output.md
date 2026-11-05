# Evaluation reasoning control

Added live-only --reasoning-effort low|high|max; omitted is null/provider-default and does not inject a field. Unsupported/missing/duplicate values and explicit Fake effort are rejected before network. Existing budgets, prompt/schema, fixtures, timeouts, production provider/configuration and card generation unchanged.

Eval-only EvaluationHttp adapter adds reasoning.effort around the actual production OpenRouter provider. It preserves request values/auth/cancellation, disposes replaced content, rechecks serialized request bound after injection and reports safe RequestSize failure before sender. No exclude/none workaround or model-specific default. Report and startup output identify effort plus fixed3000token cap; APIs extended with default-null arguments.

Added13 focused xUnit cases: valid/invalidCLI, actual provider HTTP request differs only by reasoning, defaultabsence, unchanged model/prompt/schema/token budget/auth, post-injectionbound, cancellation, report attribution/secret exclusion and unchanged failing-None verdict. No external calls or tests launched by specialist; root owns Docker wrapper/fullscript verification. git diff --check passed. No commit or production edits.

Expected next live command: ./LearnWord/tests/run-coach-eval.sh --live --model z-ai/glm-5.3-flash --reasoning-effort low --repeat 3 --output glm-v4-low.json. Actual improvement unmeasured until live run.
