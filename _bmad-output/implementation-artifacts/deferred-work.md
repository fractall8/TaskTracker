# Deferred Work

## Deferred from: code review of 1-1-board-realtime-calls (2026-07-25)

- Global per-user single-active-call-across-boards constraint isn't enforced (a user could be "active" in calls on two different boards at once). Speculative, not covered by any stated AC.
- No compensating cleanup for a partial ACS-Room-created/DB-insert-failed (or vice versa) failure in the start/join flow. This is Task 3/4 (command handler) territory; revisit once those handlers exist.
- `src/Frontend/Services/Boards/BoardActionSyncKey.cs` doesn't map the 3 new `BoardActionNotificationType` values (`CallStarted`, `CallParticipantsChanged`, `CallEnded`) — falls through to the generic default case (not fatal). Revisit in Task 6, where the frontend Store consuming these types is built.
- `User.AcsCommunicationUserId` has no issuance/rotation/revocation metadata, so a row can silently point at an ACS identity deleted independently on Azure's side. Revisit once Task 3's `IAcsCallService.EnsureUserIdentityAsync` exists to own this lifecycle.
