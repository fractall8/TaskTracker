# Deferred Work

## Deferred from: code review of 1-1-board-realtime-calls (2026-07-25)

- Global per-user single-active-call-across-boards constraint isn't enforced (a user could be "active" in calls on two different boards at once). Speculative, not covered by any stated AC.
- No compensating cleanup for a partial ACS-Room-created/DB-insert-failed (or vice versa) failure in the start/join flow — **broadened after Task 3's review to also cover `EnsureUserIdentityAsync`** (an ACS identity created via `CreateUserAsync` but the surrounding save fails, orphaning the identity on Azure's side). Same class of problem, not just Room-specific. This is Task 4 (command handler) territory; revisit once those handlers exist.
- `src/Frontend/Services/Boards/BoardActionSyncKey.cs` doesn't map the 3 new `BoardActionNotificationType` values (`CallStarted`, `CallParticipantsChanged`, `CallEnded`) — falls through to the generic default case (not fatal). Revisit in Task 6, where the frontend Store consuming these types is built.
- `User.AcsCommunicationUserId` has no issuance/rotation/revocation metadata, so a row can silently point at an ACS identity deleted independently on Azure's side. Revisit once Task 3's `IAcsCallService.EnsureUserIdentityAsync` exists to own this lifecycle.

## Deferred from: code review of 1-1-board-realtime-calls Task 3 (2026-07-25)

- Race condition in `EnsureUserIdentityAsync`'s check-then-act: two concurrent requests for the same user can both see no `AcsCommunicationUserId` and both call `CreateUserAsync`, creating two ACS identities. The DB unique index (`IX_Users_AcsCommunicationUserId`) prevents silent duplicate persistence, but the loser gets an unhandled `DbUpdateException` rather than a clean retry/conflict response. Needs Task 4's actual command handler to add catch-and-retry logic.
- `CreateRoomAsync` always passes `validFrom: null, validUntil: null`, deferring to ACS's default room TTL with no reconciliation against `BoardCall.EndedAt`'s lifecycle. Revisit once Task 4 establishes an actual call-duration business rule.
- No ACS emulator exists (unlike Azurite for Blob Storage) — local dev requires a real (or shared dev) Azure Communication Services resource just to boot the API once the connection-string check is strict, in addition to the dev-tunnel needed for Event Grid webhook testing (see Dev Notes §7).

## Deferred from: code review of 1-1-board-realtime-calls Task 4 & Task 5 (2026-07-25)

- Out-of-order Event Grid delivery isn't handled — a stale `CallParticipantAdded` arriving after a newer `CallParticipantRemoved` for the same participant would incorrectly reopen them as active. Needs per-participant sequencing state (e.g. compare `OccurredAt` against the row's last-updated timestamp) not currently modeled.
- `BoardCallEventsController` relies on the same shared static `X-Internal-Api-Key` already used by every other `/api/internal/*` endpoint — a leaked key lets anyone forge participant join/leave events for any room/user. Pre-existing `InternalApiKeyMiddleware` architecture, predates this story; revisit if a per-integration secret or Azure AD auth for Event Grid delivery is ever warranted.
- `RecordCallParticipantJoinedCommand`/`RecordCallParticipantLeftCommand`: if `GetActiveCallWithParticipantsAsync` returns `null` because the call ended concurrently with the webhook's processing, the fallback `?? call` sends a `CallParticipantsChanged` notification with an empty participant list instead of skipping it. Self-corrects on the next fetch; low priority.
