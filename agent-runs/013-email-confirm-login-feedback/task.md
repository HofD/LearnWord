# Task

## User Request

User reported that after registration, before confirming email, login failed without any visible error explaining that email confirmation is required.

## Scope

- Check the current registration, email confirmation, and login flow against specs and implementation.
- Ensure users are told what happened and what to do next when login is rejected because email is not confirmed.
- Ensure failed email confirmation state points to a valid recovery path.
- Update frontend behavior spec for any intentional behavior change.

## Acceptance Criteria

- Registration success still tells users to check email for verification instructions.
- Login with wrong credentials still shows the existing wrong-credentials error.
- Login with a correct password but unconfirmed email shows a persistent, localized instruction to confirm email.
- The instruction explains how to request a new confirmation link using the current product flow.
- Failed email confirmation copy points to registration with the same email, not password recovery.
- Frontend behavior spec documents the changed behavior.

## Out Of Scope

- Backend endpoint behavior changes.
- New resend-confirmation UI or a dedicated resend-confirmation page.
- Production SMTP or deliverability changes.
