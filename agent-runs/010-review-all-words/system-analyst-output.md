# System Analyst Output

## Requirement Interpretation

Review cards are the scheduling unit, but cards can contain multiple words. Showing only `words[0]` hides valid learning content and makes multi-word cards misleading. The desired behavior is a frontend display change only: show all words as one card-level learning block and keep the single outcome submission for the card.

## Spec Impact

Updated `specs/frontend-behavior.md` so the review UI now documents:

- all words are shown during review;
- all translations are shown after reveal in the same order;
- single-word review cards preserve the previous large layout;
- multi-word cards use a compact scrollable list.

Backend API spec does not change because `CardDto.words[]` is already part of the review response.

## Direct Actions

- Updated `ReviewComponent` template to branch between single-word and multi-word rendering.
- Added CSS for compact multi-word and multi-translation lists without changing the single-word classes.
- Added a focused component spec covering preserved single-word layout and multi-word display.

## Verification Plan

- Build the Angular app through the frontend Docker build.
- Run focused frontend tests where the local environment supports Karma.
- Visually inspect single-word and multi-word review cards against a local Docker stack using the local test account.

## Known Risks

Very large cards can still require page scrolling, but the word and translation lists now have bounded internal height so the action area remains reachable. The product still scores the whole card, not individual words, by design.
