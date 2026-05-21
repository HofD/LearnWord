# QA Backend Agent Output

## Changed Files

- `LearnWord/tests/LearnWord.WebApi.Tests/LearnWord.WebApi.Tests.csproj`
- `LearnWord/tests/LearnWord.WebApi.Tests/AiCardGenerationServiceTests.cs`
- `LearnWord/tests/LearnWord.WebApi.Tests/ConfiguredAiCardGenerationProviderTests.cs`

## Coverage Added

- WebApi validation rejects missing `sourceText`, too-long `sourceText`, and `maxCards` outside configured range before provider invocation.
- Fake provider returns deterministic draft suggestions.
- OpenRouter selected without an API key falls back to fake provider and uses a throwing HTTP handler to prove no network call occurs.
- Provider responses missing `value` or `translation` are rejected with `ai_invalid_provider_response`.
- Existing Identity ownership coverage already verifies foreign collection AI generation throws `collection_forbidden` before upstream call.

## Verification

```bash
./tests/run-all-tests.sh
```

Passed 9/9 test projects.

New `LearnWord.WebApi.Tests`: 9 passed.

No OpenRouter API key or network was required.

## Residual Risk

- Coverage is service/provider focused, not full HTTP route/model-binding integration for `LearnWord.WebApi`.
- `./deploy/local-up.sh` startup verification was not run by QA.
- Existing package vulnerability warnings appeared for `MailKit` and `AutoMapper`.
