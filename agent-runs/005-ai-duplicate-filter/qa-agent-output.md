# QA Backend Output

## Owner And Scope

Focused regression coverage for AI generation duplicate filtering.

## Changed Files

- `LearnWord/tests/LearnWord.WebApi.Tests/AiCardGenerationServiceTests.cs`

## Coverage Added

- Existing validation tests were updated for the new service signature.
- Added a regression test where the provider returns:
  - a word already present in the target collection with different casing and surrounding whitespace;
  - duplicate generated values with different casing;
  - a unique suggestion.
- The expected result keeps only non-existing, non-repeated suggestions.

## Verification

`./tests/run-all-tests.sh` from `LearnWord/LearnWord`: passed. All 9 backend test projects passed.

Observed existing dependency warnings:

- `MailKit` 4.3.0 vulnerability warning.
- `AutoMapper` 12.0.1 vulnerability warning.

## Residual Risk

The coverage is service-level. Gateway/auth behavior is unchanged and covered by existing ownership tests.
