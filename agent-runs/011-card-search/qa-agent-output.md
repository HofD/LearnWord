# QA Agent Output

## Acceptance Criteria

- Search works inside `CardsComponent` over the already loaded local `cards` array, with no new backend/API calls.
- Search matches every word in a card by `word.value` and `word.translation`.
- Search is case-insensitive and trims surrounding query whitespace.
- If any word in a card matches, the whole card remains visible.
- Pagination is calculated from the filtered list.
- Query changes reset or normalize the current page.
- Adding and deleting cards stays consistent with the active filter.
- Search UI strings are localized through EN/RU i18n dictionaries.
- No backend API contracts or backend specs change.

## Recommended Frontend Unit Cases

- Empty query shows all cards.
- Search by `Word.value` finds a card.
- Search by `Word.translation` finds a card.
- Search is case-insensitive and ignores outer query spaces.
- A multi-word card is found when any word matches.
- No matches leaves `visibleCards` empty without breaking ranges/pagination.
- `totalPages`, `rangeStart`, `rangeEnd`, and `visibleCards` use filtered counts.
- Query change from a later page does not leave the component on an invalid empty page.
- Adding and removing cards during active search recalculates the filtered view.

## Backend/API Risk

Backend risk is low because the feature is explicitly client-side over the existing `GET /api/collections/{id}` payload. If server-side pagination or lazy loading is introduced later, this search would only cover loaded cards and should be revisited.

## Manual Regression Areas

- Collections with 0, 1, 25, and more than 25 cards.
- Search by source word, translation, partial text, and different case.
- Multi-word cards.
- No-results state with add-card panel still available.
- Clearing search restores full list and pagination.
- Delete a found card.
- Add cards that do and do not match the active search.
- Change page size and navigate pages while search is active.
- Check EN/RU localization.
