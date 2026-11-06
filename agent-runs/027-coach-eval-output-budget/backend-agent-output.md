# Evaluation output-budget control

Added live-only --max-output-tokens3000|5000 with default3000 to the standalone evaluation. Optional arguments preserve existing CreateClient/Run/options call sites. Eval-only HTTP adapter changes max_tokens independently of reasoning;5000 alone does not introduce reasoning. Default3000 with no effort retains the direct untouched provider path; effort-only behavior remains. Post-rewrite request-size guard, cancellation/auth/disposal retained. Production provider/model/promptv5/schema/cardgenerator/timeout/response bounds unchanged.

Console and JSON report use requested MaxOutputTokens rather than fixed3000, while old/default reports remain3000. Existing live key gating, hidden shell input, limits/progress/scoring/output behavior unchanged. Unsupported/missing/duplicate/Fake explicit budget rejected before requests.

Added14 focused cases:2validcaps,7invalidCLI,2actual-provider HTTPdiff variants and3facts for report/verdict preservation, core/HTTP guards, final request bound/cancellation. Request capture proves only max_tokens and optional reasoning differ; same production model/messages/schema/auth otherwise, no real network or credentials. AlwaysNone still fails missed_help under5000.

Verification: git diff --check passed. Root owns Docker wrapper/fullscript; no tests or paid calls started by this agent and no commit made. Residual risk: offline captures verify configuration, not whether DeepSeek5000 improves the live benchmark; user run is still required unless root has an evaluation key.
