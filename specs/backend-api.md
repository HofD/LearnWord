# Backend API and Services Spec

This document fixes the current backend behavior for the Learn Word system.

## Topology

The backend solution contains these HTTP-facing services:

- `LearnWord.Gateway`: public Ocelot gateway. Public frontend calls use `/api/...`.
- `IdentityService.WebApi`: registration, login, refresh token, revoke token, and email confirmation.
- `LearnWord.Identity`: authenticated facade for user-owned collections, cards, and words.
- `LearnWord.WebApi`: internal CRUD service for collections, cards, and words.

In local Angular development the frontend uses `http://localhost:5100` as `environment.apiUrl`. In production it uses `https://learnword.online`.

Gateway route files:

- DEBUG: `LearnWord.Gateway/ocelot-dev.json`
- non-DEBUG: `LearnWord.Gateway/ocelot.json`

## Authentication

JWT bearer authentication is configured in the gateway and identity service using issuer, audience, lifetime, and signing-key validation.

Frontend-authenticated API requests carry:

```http
Authorization: Bearer <access-token>
```

The access token is produced by `POST /api/auth/login` or `POST /api/auth/refresh-token`.

Refresh tokens are stored server-side against `LwIdentityUser.RefreshTokens`. Refreshing rotates the token: the old token is revoked with reason `Replaced by new token`, and a new token is returned. Reuse of a revoked ancestor token revokes active descendants.

Identity registration requires:

- confirmed email for sign-in
- unique email
- password length at least 6
- at least one digit
- at least one non-alphanumeric character

Current backend config sets `RequireUppercase = false` and `RequireLowercase = false`.

The Identity service applies a global fixed-window rate limit of 60 requests per minute per authenticated user name, otherwise per Host header. Rejected requests return `429`.

## Public Gateway API

All public endpoints below are exposed through `LearnWord.Gateway` and are called by the frontend under `${environment.apiUrl}`.

### Account and Auth

#### Register

```http
POST /api/account/register
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com",
  "password": "string"
}
```

Current behavior:

- `400` if model validation fails.
- If a user with the same email already exists, returns `200 OK`.
- If the existing user is not confirmed, sends another confirmation email and still returns `200 OK`.
- On successful new registration, sends a confirmation email and returns `200 OK`.
- If email sending fails after the user was created, logs the error and returns `200 OK`.
- If Identity user creation fails, returns `400` with Identity errors.

#### Send Confirmation Email

```http
POST /api/account/conformation/send
Content-Type: application/json
```

Note: the route is currently spelled `conformation`, not `confirmation`.

Request:

```json
{
  "email": "user@example.com"
}
```

Current behavior:

- `400` if model validation fails.
- `404` with `User not exists.` if the email does not belong to a user.
- `400` with `Email already confirmed.` if already confirmed.
- `200 OK` after sending a confirmation email.

#### Confirm Email

```http
GET /api/account/confirm?userId=<id>&code=<token>
```

Current behavior:

- `400` if `userId` or `code` is missing.
- `404` if user is not found.
- `200 OK` when confirmation succeeds.
- `500` with Identity errors when confirmation fails.

#### Forgot Password

```http
POST /api/account/password/forgot
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com"
}
```

Expected behavior:

- `400` if model validation fails.
- `200 OK` when the request is accepted.
- If the email belongs to a registered user, sends a password reset email with a frontend callback URL.
- If the email is unknown, still returns `200 OK` and does not reveal whether an account exists.

#### Reset Password

```http
POST /api/account/password/reset
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com",
  "code": "password-reset-token",
  "password": "new-password"
}
```

Expected behavior:

- `400` if model validation fails.
- `404` with `User not exists.` if the email does not belong to a user.
- `200 OK` when the password reset succeeds.
- If Identity password reset fails, returns `400` with Identity errors.
- New password policy matches registration: length at least 6, at least one digit, at least one non-alphanumeric character.

#### Login

```http
POST /api/auth/login
Content-Type: application/json
```

Request:

```json
{
  "email": "user@example.com",
  "password": "string"
}
```

Response:

```json
{
  "email": "user@example.com",
  "token": "jwt-access-token",
  "refreshToken": "refresh-token"
}
```

Current behavior:

- `400` if model validation fails.
- `401` if user is missing or password is wrong.
- `403` with `Email not confirmed.` if email is not confirmed.
- `200 OK` with access token and refresh token when login succeeds.

#### Refresh Token

```http
POST /api/auth/refresh-token
Content-Type: application/json
```

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Response shape is the same as login.

Current behavior:

- `400` with `Invalid token` if no user owns the refresh token.
- Otherwise validates/rotates the refresh token and returns new login response.
- If token is inactive, the service throws an exception; current controller does not convert that into a typed error response.

#### Revoke Token

```http
POST /api/auth/revoke-token
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "refreshToken": "refresh-token"
}
```

Current behavior:

- `400` with `{ "message": "Token is required" }` if request token is empty.
- `400` with `Invalid token` if no user owns the refresh token.
- `200 OK` with `Token revoked` when revoked.
- If token is inactive, the service throws an exception; current controller does not convert that into a typed error response.

### Collections

Collection endpoints under `/api/collections` require bearer authentication at the gateway and are handled by `LearnWord.Identity`.

#### List Collections

```http
GET /api/collections
Authorization: Bearer <access-token>
```

Response:

```json
{
  "collections": [
    {
      "id": 1,
      "name": "English",
      "cardsCount": 10
    }
  ]
}
```

Current behavior:

- `401` if the JWT middleware did not resolve `HttpContext.Items["UserId"]`.
- Returns only collections linked to the current user in `CollectionIdentity`.

#### Get Collection

```http
GET /api/collections/{id}
Authorization: Bearer <access-token>
```

Response:

```json
{
  "id": 1,
  "name": "English",
  "cards": [
    {
      "id": 5,
      "collectionId": 1,
      "words": [
        {
          "id": 9,
          "value": "cat",
          "transcription": "kat",
          "translation": "кот"
        }
      ],
      "learnt": false
    }
  ],
  "createdAt": "2026-05-11T00:00:00+00:00",
  "modifiedAt": null,
  "deletedAt": null
}
```

Current behavior:

- `401` if user id is missing.
- `404` if the collection is not linked to the user.
- `200 OK` with collection, cards, and words otherwise.

#### Create Collection

```http
POST /api/collections
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "name": "English"
}
```

Current behavior:

- Creates the collection in `LearnWord.WebApi`.
- Creates a `CollectionIdentity` link for the current user.
- Returns `201 Created` with the created `CollectionDto`.

#### Rename Collection

```http
PUT /api/collections/{id}
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "name": "New name"
}
```

Current behavior:

- `401` if user id is missing.
- `404` if the collection is not linked to the user.
- `200 OK` with the renamed collection otherwise.

#### Delete Collection

```http
DELETE /api/collections/{id}
Authorization: Bearer <access-token>
```

Current behavior:

- `401` if user id is missing.
- `404` if the collection is not linked to the user.
- Calls internal collection delete, then removes the user link.
- `200 OK` if both succeed.
- `503` if the internal collection delete returns an unsuccessful HTTP status.

#### Cards For Review

```http
GET /api/collections/{collectionId}/review
Authorization: Bearer <access-token>
```

Response:

```json
[
  {
    "id": 5,
    "collectionId": 1,
    "words": [],
    "learnt": false,
    "dueDate": "2026-05-21T10:00:00Z",
    "intervalDays": 1,
    "easeFactor": 2.5,
    "reviewCount": 0,
    "lastReviewedAt": null
  }
]
```

Current behavior:

- Requires the collection to be linked to the current user.
- Returns cards due for review where `dueDate <= now`.
- Newly created cards are due immediately.
- If collection exists but has no cards, returns an empty array.
- If collection ownership fails, `CollectionIdentityService` throws `UnauthorizedAccessException`; the controller currently does not map this to a typed error response.

#### Generate AI Card Suggestions

```http
POST /api/collections/{collectionId}/ai/generate-cards
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "sourceText": "Yesterday I went to the market and bought fresh vegetables.",
  "sourceLanguage": "en",
  "targetLanguage": "ru",
  "level": "A2",
  "maxCards": 5
}
```

Response:

```json
{
  "cards": [
    {
      "value": "market",
      "transcription": "ˈmɑːrkɪt",
      "translation": "рынок",
      "example": "I went to the market.",
      "explanation": "A place where people buy and sell goods.",
      "difficulty": "A2"
    }
  ]
}
```

Current behavior:

- Requires bearer authentication at the gateway.
- `LearnWord.Identity` checks that the collection is linked to the current user before forwarding the request.
- `LearnWord.Identity` does not call an LLM directly; it proxies the request to internal `LearnWord.WebApi`.
- `LearnWord.WebApi` validates `sourceText` and `maxCards` before provider calls.
- `LearnWord.WebApi` filters provider suggestions after the LLM response so words already present in the target collection are not returned again. Matching trims surrounding whitespace and is case-insensitive.
- Provider prompts require `transcription` to contain a phonetic or IPA transcription of the source word, never the target-language translation.
- If the LLM provider rate-limits generation, returns `429` with ProblemDetails code `ai_provider_rate_limited` and a retry-later message.
- If the LLM provider is unavailable, returns `503` with ProblemDetails code `ai_provider_unavailable`.
- If the LLM provider credentials or route are invalid, returns `502` with ProblemDetails code `ai_provider_configuration_error`.
- Returned cards are suggestions only and are not persisted automatically.
- The frontend persists accepted suggestions through the existing `POST /api/cards` endpoint.
- OpenRouter provider configuration is read only by `LearnWord.WebApi`.
- Tests should use a fake provider and must not require a real OpenRouter API key.

### Cards

Card endpoints require bearer authentication and are handled by `LearnWord.Identity`.

#### Create Card

```http
POST /api/cards
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "collectionId": 1,
  "words": [
    {
      "value": "cat",
      "transcription": "kat",
      "translation": "кот"
    }
  ]
}
```

Current behavior:

- Creates the card in `LearnWord.WebApi`.
- Creates a `CardIdentity` link between the created card and current user.
- Returns `201 Created` with the created `CardDto`.
- Current implementation does not explicitly verify that `collectionId` belongs to the user before creating the card.

#### Delete Card

```http
DELETE /api/cards/{id}
Authorization: Bearer <access-token>
```

Current behavior:

- Checks that the card is linked to the current user.
- Calls internal card delete.
- `200 OK` when internal delete succeeds.
- `500` when internal delete returns an unsuccessful HTTP status.
- If ownership check fails, the service throws; the controller currently does not map this to `404`/`403`.

### Review

Review endpoints require bearer authentication and are handled by `LearnWord.Identity`.

#### Submit Card Review Outcome

```http
POST /api/review/cards/{cardId}/review
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "outcome": "Good"
}
```

Allowed `outcome` values:

- `Again`
- `Hard`
- `Good`
- `Easy`

Current behavior:

- Checks card ownership before forwarding the review to the internal word service.
- Updates the card spaced-repetition state:
  - `intervalDays`
  - `easeFactor`
  - `reviewCount`
  - `lastReviewedAt`
  - `dueDate`
- Sets `lastReviewedAt` and the legacy `ShowedAt` field to the current UTC time.
- Sets `Learnt = false` for `Again`; sets `Learnt = true` for `Hard`, `Good`, and `Easy`.
- Returns `200 OK` with updated `CardDto`.

### Words

Word endpoints require bearer authentication and are handled by `LearnWord.Identity`.

#### Add Word To Card

```http
POST /api/cards/{cardId}/words
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "value": "cat",
  "transcription": "kat",
  "translation": "кот"
}
```

Current behavior:

- Checks that `cardId` belongs to the current user.
- Adds the word to the card through internal `LearnWord.WebApi`.
- Internal `WordEditService` resets the card after adding:
  - `Learnt = false`
  - `LearntAt = null`
  - `ShowedAt = null`
  - SRS state reset to due immediately with `intervalDays = 0`, `easeFactor = 2.5`, `reviewCount = 0`, and `lastReviewedAt = null`
  - `ModifiedAt = now`
- Returns `201 Created` with `WordDto`.

#### Update Word

```http
PUT /api/cards/{cardId}/words/{id}
Authorization: Bearer <access-token>
Content-Type: application/json
```

Request:

```json
{
  "value": "cat",
  "transcription": "kat",
  "translation": "кот"
}
```

Current behavior:

- Checks card ownership.
- Internal service verifies that the word belongs to `cardId`.
- Updates word fields.
- Resets the card review state the same way as adding a word.
- Returns `200 OK` with `WordDto`.

#### Delete Word

```http
DELETE /api/cards/{cardId}/words/{id}
Authorization: Bearer <access-token>
```

Current behavior:

- Checks card ownership.
- Deletes the word through internal `LearnWord.WebApi`.
- If the card still has active words after deletion, resets the card review state the same way as adding/updating a word.
- If the deleted word was the card's last active word, deletes the card as well because empty cards have no learning value.
- Returns `200 OK` if internal delete succeeds.
- Returns `500` if internal delete returns an unsuccessful HTTP status.

## Internal LearnWord.WebApi

These endpoints exist on the internal CRUD service and are not called directly by the Angular frontend in the current gateway setup.

- `GET /collections/{id}`: returns collection with cards and words.
- `GET /collections?ids=1&ids=2`: returns `CollectionListDto` for matching ids.
- `POST /collections`: creates collection.
- `DELETE /collections/{id}`: deletes collection.
- `PUT /collections/{id}`: renames collection.
- `GET /collections/{id}/review-cards`: returns review candidates.
- `POST /cards`: creates card.
- `DELETE /cards/{id}`: deletes card.
- `POST /review/cards/{cardId}/review`: submits a review outcome.
- `POST /cards/{cardId}/words`: adds word.
- `PUT /cards/{cardId}/words/{id}`: updates word.
- `DELETE /cards/{cardId}/words/{id}`: deletes word.

## Data Contracts

### CollectionCreateDto

```json
{
  "name": "string"
}
```

### CollectionRenameDto

```json
{
  "name": "string"
}
```

### CollectionListDto

```json
{
  "collections": [
    {
      "id": 0,
      "name": "string",
      "cardsCount": 0
    }
  ]
}
```

### CollectionDto

```json
{
  "id": 0,
  "name": "string",
  "cards": [],
  "createdAt": "date-time",
  "modifiedAt": null,
  "deletedAt": null
}
```

### CardCreateDto

```json
{
  "collectionId": 0,
  "words": []
}
```

### CardDto

```json
{
  "id": 0,
  "collectionId": 0,
  "words": [],
  "learnt": false,
  "dueDate": "date-time",
  "intervalDays": 0,
  "easeFactor": 2.5,
  "reviewCount": 0,
  "lastReviewedAt": null
}
```

### ReviewCardRequest

```json
{
  "outcome": "Again|Hard|Good|Easy"
}
```

### WordCreateDto and WordUpdateDto

```json
{
  "value": "string",
  "transcription": "string",
  "translation": "string"
}
```

### WordDto

```json
{
  "id": 0,
  "value": "string",
  "transcription": "string",
  "translation": "string"
}
```

### AiCardGenerationRequest

```json
{
  "sourceText": "string",
  "sourceLanguage": "string",
  "targetLanguage": "string",
  "level": "string",
  "maxCards": 5
}
```

Validation:

- `sourceText` is required and must fit the configured length limit.
- `sourceLanguage` and `targetLanguage` are optional language hints.
- `level` is an optional CEFR-style hint.
- `maxCards` must be between 1 and the configured maximum.

### AiCardGenerationResponse

```json
{
  "cards": []
}
```

### AiCardSuggestionDto

```json
{
  "value": "string",
  "transcription": "string",
  "translation": "string",
  "example": "string",
  "explanation": "string",
  "difficulty": "string"
}
```

## Persistence Rules

Core domain entities include soft-delete-ish fields (`DeletedAt`) in their models, and query methods filter out rows where `DeletedAt != null`. Current repository delete methods call EF `Remove`, so deletion is currently physical from repository code.

Collection list lookup by ids currently filters by the provided ids and includes cards for count. It does not filter by `DeletedAt` in `FindByIds`.

Card review status:

- Newly created cards are due immediately with `intervalDays = 0`, `easeFactor = 2.5`, `reviewCount = 0`, and `lastReviewedAt = null`.
- Review queue selection is based on `dueDate <= now`.
- `Again`, `Hard`, `Good`, and `Easy` update `dueDate`, `intervalDays`, `easeFactor`, `reviewCount`, and `lastReviewedAt`.
- Legacy `learn` applies the same scheduler behavior as `Good`.
- Legacy `forget` applies the same scheduler behavior as `Again`.
- adding/updating a word resets card review state.
- deleting a word resets card review state when the card still contains active words.
- deleting the last active word deletes the card.
