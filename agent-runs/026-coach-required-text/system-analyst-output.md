# Scope and acceptance

User approved the remaining empty-text contract fix after GLM-v4-low16/18. Both failures were text_empty at observations[0].explanation (raw output intentionally not retained, so exact empty-vs-whitespace not known). Wire schema lacked minLength while local validator rejects mandatory blank text. Add explicit nonempty minima to required strings, preserve intentional empty summary forNone and optional exercise explanation, keep semantic whitespace/control checks.

Backend specialist owns production schema/Shape/prompt-version and focused tests; root docs, independent outgoing-schema Ajv check plus actual gateway and Docker eval/fullrunner. Preserve model/budget/reasoning/cardgenerator, resultDTOschema1, advisoryNone; newpromptv5 cache separate fromhistoricalv4, no automatic replay. No paid call, deploy, push or main merge.

Minimum1 excludes literal empty strings, not whitespace-only strings; retain Text semantic guard and document this limit. Conditional Intervention summary remains a semantic nonblank constraint because None may use an empty summary; do not globally force nonempty summary or add speculative if/then features. Independent Ajv HTTP stub must demonstrate both schema rejection of empty required fields and retained semantic rejection of whitespace/emptyInterventionSummary, plus valid optional empty fields.
