# Frontend Behavior Spec

This document fixes the current Angular frontend behavior for Learn Word.

## Application Setup

The frontend is an Angular standalone-component application under `LearnWordWebApp/lw-app`.

Key setup:

- Routes are configured in `src/app/app.routes.ts`.
- `HttpClientModule` and `httpInterceptorProviders` are registered in `src/app/app.config.ts`.
- Development API base URL is `http://localhost:5100`.
- Production API base URL is `https://learnword.online`.

The app uses Bootstrap classes and components in templates.

## Routes

| Path | Component | Guard |
| --- | --- | --- |
| `/` | `HomeComponent` | none |
| `/register` | `RegisterComponent` | none |
| `/login` | `LoginComponent` | none |
| `/confirm` | `EmailConfirmComponent` | none |
| `/collections` | `CollectionsComponent` | `authGuard` |
| `/collections/:id` | `CollectionComponent` | `authGuard` |
| `/review/:collectionId` | `ReviewComponent` | `authGuard` |
| `/about` | `AboutComponent` | none |

`authGuard` allows navigation when `TokenStorageService.getToken()` returns a non-null token. Otherwise it redirects to `/login` with `returnUrl=<attempted-url>`.

## Token Storage

Tokens are stored in `window.sessionStorage`.

Keys:

- access token: `auth-token`
- refresh token: `auth-refreshtoken`

`TokenStorageService.deleteToken()` clears the entire `sessionStorage`, not only auth keys.

## HTTP Interceptor

For every outgoing HTTP request:

- If an access token exists, the interceptor adds `Authorization: Bearer <token>`.
- If a response is `401` and the request URL does not include `/login`, the interceptor attempts to refresh the token.
- While one refresh is in progress, concurrent requests wait on a shared `BehaviorSubject`.
- On refresh success, it stores the new access token and refresh token, then retries the original request.
- On refresh failure, it clears session storage.
- Current code navigates to `/login` only if `error.status == '403'` as a string comparison.

## Header

The header:

- shows brand text `Learn Word` linking to `collections`.
- shows nav links to `collections` and `about`.
- shows `Log Out` when `AuthService.authChanged` says authenticated or `AuthService.isLoggedIn()` returns true.
- logout calls `AuthService.logout()` and navigates to `/`.

Current logout behavior calls `AuthService.revokeToken()` but does not subscribe to the returned observable, so the HTTP revoke request is not guaranteed to execute. It then clears session storage and broadcasts unauthenticated state.

## Home

The home page displays a short prompt and two buttons:

- `/login`
- `/register`

## Registration

Route: `/register`

Form controls:

- `email`: required, Angular email validator.
- `password`: required, minimum length 6.
- `confirmPassword`: required, must match `password`.

Submit behavior:

- If invalid, logs form value to console and stops.
- Calls `POST /api/account/register` with the form value.
- On success, shows alert `Registration successful, please check your email for verification instructions` with `keepAfterRouteChange: true`, then navigates to `../login`.
- On error, shows the error with `autoClose: false` and resets `submitting` to false.

The template text says password must have at least one non-alphanumeric character and at least one upper-case letter. Current backend code requires non-alphanumeric and digit, but does not require uppercase.

## Login

Route: `/login`

Form controls:

- `userEmail`: required.
- `userPassword`: required.

Submit behavior:

- If invalid, sends `this.loginForm.value` to `AlertService.error()` and stops.
- Calls `AuthService.login(email, password)`.
- `AuthService.login` calls `POST /api/auth/login`, stores returned `token` and `refreshToken`, and broadcasts authenticated state.
- After login, if an access token exists, the component broadcasts authenticated state again and navigates to `returnUrl` query param, defaulting to `collections`.

For a `401` login response, `AuthService.handleError` shows alert `Wrong username or password.`.

## Email Confirmation

Route: `/confirm?userId=<id>&code=<token>`

Behavior:

- Initial status is `Verifying`.
- Reads `userId` and `code` from query params.
- Removes query params from the current URL with `router.navigate([], { replaceUrl: true })`.
- Calls `GET /api/account/confirm?userId=<id>&code=<encoded-code>`.
- On success:
  - shows alert `Verification successful, you can now login` with `keepAfterRouteChange: true`.
  - sets status to `Verified`.
  - navigates to `../login`.
- On error, sets status to `Failed`.

The failed-state template links to `forgot-password`, but no `/forgot-password` route is currently configured.

## Collections

Route: `/collections`

Initial behavior:

- `loaded = false`.
- On init, calls `GET /api/collections`.
- On success, sets `collections = data["collections"]` and `loaded = true`.

UI states:

- While not loaded, shows a Bootstrap spinner.
- If loaded and there are no collections, shows the new collection form.
- If collections exist, shows one card per collection and the new collection form below the list.

New collection form:

- Control `name`, required.
- Submit is disabled while invalid.
- Submit calls `POST /api/collections` with `{ name }`.
- On success, reloads the collection list.
- Immediately clears the form field after initiating the request.

Collection list item:

- Shows collection name.
- Shows `Cards in this collection: {{cardsCount}}`.
- `Open` links to `/collections/{{id}}`.
- `Review` links to `/review/{{id}}`.
- `Delete` opens a Bootstrap modal.
- Confirming delete calls `DELETE /api/collections/{id}` and reloads the list on success.

Rename:

- `CollectionHttpService.rename(id, name)` exists and calls `PUT /api/collections/{id}`.
- `CollectionsComponent.rename(id)` is currently empty.
- Rename UI is commented out in the template.

## Collection Details

Route: `/collections/:id`

Behavior:

- Reads `id` from route params.
- Calls `GET /api/collections/{id}`.
- Shows spinner until loaded.
- On success, stores the returned collection and shows:
  - collection name
  - `CardsComponent` with `[cards]="collection.cards"` and `[collectionId]="collection.id"`

## Cards and Words

### Card Model

Frontend `Card`:

```ts
{
  id: number | null;
  collectionId: number;
  learnt: boolean;
  words: Word[];
}
```

Frontend `Word`:

```ts
{
  id: number | null;
  value: string;
  transcription: string;
  translation: string;
}
```

### CardsComponent

Inputs:

- `cards`
- `collectionId`

Behavior:

- Renders each existing card inside a Bootstrap card.
- Each card has a `Delete` button that opens a Bootstrap confirmation modal.
- Confirming delete calls `DELETE /api/cards/{id}` and removes the card from the local `cards` array on success.
- Renders one trailing empty `CardComponent` for adding a new card to the collection.
- When the trailing child emits `onCardAdded`, pushes the returned card into the local `cards` array.

### CardComponent

Inputs:

- `collectionId`
- `card`

Output:

- `onCardAdded`

Behavior:

- If `card === null` on init, creates a placeholder `Card(null, collectionId, false, [])`.
- If `card?.id === null`, renders `WordsComponent` without a card input.
- If `card?.id !== null`, renders `WordsComponent` with the existing card input.
- Re-emits `onCardAdded` events from `WordsComponent`.

### WordsComponent

Inputs:

- `card`
- `collectionId`

Output:

- `onCardAdded`

New word form:

- `value`: required.
- `translation`: required.
- `transcription`: optional.

Display behavior:

- If `card?.id !== null`, renders the card's current words.
- Each displayed word shows value, transcription, and translation.
- Each displayed word exposes edit and delete actions.
- Delete asks for confirmation before calling the API.
- A Bootstrap collapse reveals the add-word form.

Submit behavior:

- Builds `new Word(null, value, transcription, translation)`.
- If `this.card === undefined`, creates a new card with the current `collectionId`, `learnt = false`, and the new word, then calls `POST /api/cards`.
- On card creation success, emits `onCardAdded` with the returned card.
- Otherwise, pushes the new word into the current card's local `words` array, then calls `POST /api/cards/{cardId}/words`.
- On add-word success, `updateCard(data)` currently does not update local state or emit anything.
- The form is reset immediately after initiating the request.

Edit behavior:

- Editing a word reuses the same validation rules as adding a word: `value` and `translation` are required, `transcription` is optional.
- Saving an edit calls `PUT /api/cards/{cardId}/words/{id}`.
- On success, the edited word in `card.words` is replaced with the returned `WordDto`.
- On error, the displayed word remains unchanged.

Delete behavior:

- Confirming delete calls `DELETE /api/cards/{cardId}/words/{id}`.
- On success, the word is removed from the local `card.words` array.
- If the deleted word was the last word in the card, the backend deletes the card and the frontend removes that card from the collection view.
- On error, the displayed word remains unchanged.

## Review Flow

Route: `/review/:collectionId`

Initial behavior:

- Reads `collectionId` from route params.
- Calls `GET /api/collections/{collectionId}/review`.
- On success:
  - casts response to `Card[]`.
  - sets `currentCard` to the first card or `null`.
  - sets `loaded = true`.
  - sets `showTranslation = false`.
- On error:
  - logs the error.
  - sets `loaded = true`.

Review UI:

- While loading, shows spinner.
- If `currentCard` exists and has at least one word:
  - shows progress as `(currentIndex + 1) / cards.length`.
  - shows the first word's `value`.
  - attempts to show `currentCard.transcription`.
  - toggles translation visibility when the translation container is clicked.
  - when hidden, shows `Click to show translation`.
  - when visible, shows the first word's `translation`.
  - `Forget` calls `POST /api/cards/{id}/forget`.
  - `Learn` calls `POST /api/cards/{id}/learn`.
- After successful Learn or Forget:
  - advances to the next card.
  - clears translation visibility.
- When no current card remains, shows `No Cards to Review` and a link back to `/collections`.

Current frontend `Card` interface inside `ReviewComponent` includes `transcription: string`, but backend `CardDto` does not expose a card-level transcription. The template therefore does not show word transcription correctly in review; it should currently be treated as existing behavior.

## About

Route: `/about`

Displays:

- application name
- feature list
- technology stack
- version `1.0.0`
- current year from `new Date().getFullYear()`

## Error Handling

Most domain HTTP services use this pattern:

- `status === 0`: log client/network error.
- `status === 401`: navigate to `/login` with `returnUrl` equal to current router URL.
- other status: log backend response status and body.
- return `throwError(() => new Error('Something bad happened; please try again later.'))`.

`AuthService` differs:

- `401`: shows `Wrong username or password.`
- if backend error body is an array, joins `description` or `code` values.
- if backend error body is a non-empty string, returns that string.
- otherwise returns `Something bad happened; please try again later.`

## Current API Calls Used By Frontend

| Frontend action | Method and path |
| --- | --- |
| Register | `POST /api/account/register` |
| Login | `POST /api/auth/login` |
| Refresh token | `POST /api/auth/refresh-token` |
| Revoke token | `POST /api/auth/revoke-token` |
| Confirm email | `GET /api/account/confirm?userId=...&code=...` |
| List collections | `GET /api/collections` |
| Get collection | `GET /api/collections/{id}` |
| Create collection | `POST /api/collections` |
| Rename collection | `PUT /api/collections/{id}` |
| Delete collection | `DELETE /api/collections/{id}` |
| Create card | `POST /api/cards` |
| Delete card | `DELETE /api/cards/{id}` |
| Add word | `POST /api/cards/{cardId}/words` |
| Get review cards | `GET /api/collections/{collectionId}/review` |
| Mark learned | `POST /api/cards/{cardId}/learn` |
| Mark forgotten | `POST /api/cards/{cardId}/forget` |
