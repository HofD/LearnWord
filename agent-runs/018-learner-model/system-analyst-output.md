# Audit and implementation decisions

The initial working trees were clean. Read repository AGENTS/role roster/system analyst guidance, backend/frontend READMEs, behavior specs, run003 scheduler artifacts and run014–017 social acceptance artifacts.

Current Card owns SRS state; CardService applies the scheduler and saves through CardRepository. Word edits reset SRS through the Identity facade. Card/collection deletion is soft deletion. Social DTOs are explicit allowlists and forks construct fresh independent rows. Current review UI has outcome buttons and a pending flag, but its submit method also needs a guard and stable retry identity.

Preserve these mechanisms. Keep learning records in the words database so review/card/history/state/session changes can commit together. Resolve JWT ownership in LearnWord.Identity before forwarding private learning operations. Use collection-scoped planning and summary, matching the current review route. No user-selectable owner identifier.

Specialist ownership: backend_learner production backend and migration; frontend_learner Angular production/tests; qa_learner backend tests and repeatable gateway acceptance script. Analyst owns specs, integration review, Docker checks and personal browser/gateway acceptance.

Docker initially had no running containers. Access requires sandbox escalation; approved read-only Docker inventory succeeded. Baseline local-up launched before feature changes to establish the existing persisted database state.

Verification and final decisions are recorded in final-acceptance.md after execution.
