# Scope and acceptance

User approved the two diagnostic/contract fixes, retaining Gemma. Existing production export proves content was returned and rejected, but not the exact rejection rule. Do not claim the suspected prompt mismatch as the production root cause.

Production specialist owns sanitized metadata, validator and Coach provider contract; QA specialist owns regression tests. Root owns specs, authenticated gateway stub acceptance, full Docker verification and review-branch commit. No frontend implementation is required because the diagnostics page displays the complete JSON.

Acceptance: exact allowlisted code/path on a controlled semantic failure; no card/model text or keys in export; old public error code preserved; private endpoint 401/404; normal learning data unchanged; structured-output parameter requirement observed by HTTP stub; full backend suite and Docker startup pass. No paid model request, automatic historical replay, production deployment or main merge.
