# Backend implementation

Added private typed exception metadata while preserving the exact sealed UpstreamServiceException and public failure-code contract. Diagnostic exports now include nullable failureStage, validationCode, validationPath and providerHttpStatus. Persisted failure snapshots and sanitized warning logs receive these fields under the existing generation fence. Historical snapshots deserialize with null additions; retries start a clean snapshot.

Validation first traverses server-owned schema keys, reports required/type/enum/bounds/duplicate errors at known paths, and reports unknown properties only at their containing object. Array limits are checked before indexed traversal. Semantic rules distinguish card/revision/evidence/lesson/practice/option failures, including exercise_prompt_mismatch versus exercise_answer_mismatch for the exact source-word/translation pair. Model property names, raw exception messages, values and provider error bodies are never exported or logged.

Coach-only OpenRouter requests now use provider.require_parameters=true. Prompt coach-v3 explicitly requires exact currentContent.value as the exercise prompt, the translation paired with the same word, correct evidence gates and mutually exclusive lessons/practice. Schema bounds/descriptions mirror backend limits. None remains valid. Gemma/model selection, card-generator requests, token budgets and reasoning options were not changed.

Configuration, size, HTTP rejection, timeout/network, envelope/empty/oversized content, stale-context and diagnostic-size stages are separated. HTTP response status is retained when available; output-validation paths do not claim an HTTP status unavailable to that layer.

Verification: git diff --check passed. Root owns Docker build, full test runner and controlled local gateway stub acceptance; no paid or production provider requests were made by this agent. QA owns regression tests; system analyst owns spec changes and final verification evidence. No commit or merge made by this agent.
