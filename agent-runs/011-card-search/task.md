# Card Search

## User Request

Add search for cards by word and translation. Use the full delivery flow and split work between agents. Keep the search field compact by adding it to the current paging panel.

## Scope

- Frontend card list search in `CardsComponent`.
- Search by `Word.value` and `Word.translation`.
- Frontend behavior spec update.
- Focused frontend verification and final acceptance.

## Acceptance Criteria

- The collection details card list has a compact search input in the existing card controls area.
- Search filters the full local card list before pagination.
- Search matches any word text or translation in a card.
- Search is case-insensitive, trims the query, and normalizes diacritics.
- Empty search keeps the current unfiltered behavior.
- Changing search resets the card pager to page 1.
- Active search shows a match count out of the total collection size.
- No-match search shows an empty state and keeps the add-card panel visible.
- User-facing strings are localized through the frontend i18n dictionaries.

## Out Of Scope

- Backend search endpoint.
- Server-side pagination.
- Ranking or fuzzy matching beyond substring search.
