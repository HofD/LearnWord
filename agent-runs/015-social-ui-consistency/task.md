# Social UI consistency — review follow-up

User approved recommended design: replace always-expanded visibility panel with compact header status and settings dialog; share action remains directly available for shared modes. Reuse established card pagination and collection/card presentation for Explore/shared collection. Use dark neutral content links. Preserve APIs, auth, ownership, existing learning/manual/AI editing and local card search. Existing pending Social Collections changes must remain intact.

Acceptance:
- Owner header has compact visibility trigger and shared-mode share action; no default settings panel above cards.
- Accessible dialog with selected/saved mode, pending/error feedback, cancel/reopen reset, keyboard close/focus restoration and mobile fit.
- Same reusable pager component uses established range, per-page, grouped previous/current/next, one-page hiding and mobile layout. Owner keeps 25/50/100 and client paging; social uses 25/50 within existing API max50 and server paging.
- Shared and owner collection reuse presentational header/card/word elements; foreign content has no editing, forms, delete modals or review-state requirement. No fake IDs or private DTO expansion.
- Dark content links incl hover/focus/visited; preserve contrast for buttons/dark navigation.
- Relevant regression tests, Docker frontend builds and authenticated desktop/mobile browser acceptance.
