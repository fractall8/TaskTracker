---
baseline_commit: ef13ebc9f9710561a83421bfbc0ae7f35bbbb0e5
---

# Story 1.1: Real-Time Board Calls — Azure Communication Services Video & Screen Share

Status: in-progress

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **Revision note:** This story was originally specced around a custom P2P WebRTC mesh (dedicated `BoardCallHub`, hand-rolled SDP/ICE signaling, STUN/TURN). It has been **pivoted to Azure Communication Services (ACS)** — see Change Log. Tasks 1–2 are now fully implemented for the ACS design (including the `AcsRoomId`/`AcsCommunicationUserId` additions). Tasks 3–8 are rewritten from scratch for ACS and have not been implemented.

## Story

As a board member,
I want to start or join a real-time video call with screen sharing scoped to my board,
so that my team can run synchronous collaboration (e.g. Daily Scrum) without leaving the board.

## Acceptance Criteria

1. A **ScrumMaster or Admin** board member can start a call on a Board that has no active call (enforced at the database level via the existing partial unique index on `BoardCalls.BoardId WHERE "EndedAt" IS NULL`). Starting provisions an **Azure Communication Services Room** and returns the starter an ACS access token scoped to that room.
2. Any board member can see an active-call indicator ("Call in progress · N/4 joined") and join, up to a cap of **4 total participants**. The cap is a single centralized constant (not scattered across the codebase) so it can be swapped for a per-subscription-plan limit later without restructuring — see Dev Notes §7. A 5th join attempt fails with a clear "call is full" error.
3. Joining adds the user as an **ACS Room participant** — role mapped from their `BoardRoleDto` (`ScrumMaster`/`Admin` → ACS `Presenter`, `User` → ACS `Attendee`) — and returns them an ACS access token. The Blazor frontend uses the **`@azure/communication-calling`** JS SDK (via JS interop) to connect audio/video/screen-share. **No raw `RTCPeerConnection`, SDP, or ICE candidate handling exists anywhere in this codebase** — ACS owns that entirely.
4. If a user denies camera/microphone permission, or has no such device, they can still join **audio-only or view-only** rather than being blocked from joining entirely.
5. Participant join/leave state in **our** database is driven **exclusively by Azure Event Grid webhooks** (`Microsoft.Communication.CallParticipantAdded` / `CallParticipantRemoved`) relayed from the ACS Room's call — never written directly by a client-triggered REST call. When the last confirmed participant leaves (per the webhook), the call is automatically marked ended.
6. A ScrumMaster/Admin can explicitly **end the call for everyone** at any time; this also revokes ACS Room participant access so no one can silently continue the call afterward.
7. Starting, participant changes, and ending a call all notify board members via the **existing** `BoardActionsHub`/`BoardActionNotification` channel — **no new SignalR hub exists for this feature.**

## Tasks / Subtasks

- [x] **Task 1 — Domain & Persistence** (AC: 1, 2, 5, 6) — *fully implemented, including the ACS pivot additions (`AcsRoomId`, `AcsCommunicationUserId`)*
  - [x] Add `Domain/Entities/BoardCall.cs` and `Domain/Entities/BoardCallParticipant.cs`
  - [x] Add `Persistence/Configurations/BoardCallConfiguration.cs` and `BoardCallParticipantConfiguration.cs`
  - [x] Add `Database/Scripts/0020_CreateBoardCallTables.sql` — two tables + FKs + the two partial unique indexes (one active call per board, one active participant per user per call)
  - [x] Add `Application/Interfaces/Repositories/IBoardCallRepository.cs` + `Persistence/Repositories/BoardCallRepository.cs`
  - [x] Register the new repository in `RepositoriesModule`
  - [x] **NEW — add `AcsRoomId` (required `string`) to `BoardCall`.** Since `0020_CreateBoardCallTables.sql` has not been applied to any shared database yet (this session only), **amend it in place** rather than adding a new script: add `"AcsRoomId" character varying(128) NOT NULL` to the `BoardCalls` table definition, plus `CREATE UNIQUE INDEX "UX_BoardCalls_AcsRoomId" ON "BoardCalls" ("AcsRoomId");` (one ACS Room maps 1:1 to one `BoardCall`). Update `BoardCallConfiguration.cs` (`builder.Property(c => c.AcsRoomId).IsRequired().HasMaxLength(128);`) to match.
  - [x] **NEW — persist one ACS identity per `User`.** Add `public string? AcsCommunicationUserId { get; set; }` to `Domain/Entities/User.cs`. Unlike `BoardCalls`, the `Users` table already exists and has been through prior migrations — this **must** be a new script, `Database/Scripts/0021_AddAcsCommunicationUserIdToUsers.sql`:
    ```sql
    BEGIN;

    ALTER TABLE "Users" ADD COLUMN "AcsCommunicationUserId" character varying(255) NULL;

    CREATE UNIQUE INDEX "UX_Users_AcsCommunicationUserId"
        ON "Users" ("AcsCommunicationUserId") WHERE "AcsCommunicationUserId" IS NOT NULL;

    COMMIT;
    ```
    Update `UserConfiguration.cs` to map the new nullable property (`HasMaxLength(255)`).

- [x] **Task 2 — Contracts (Shared)** (AC: all) — *fully implemented, including `AcsCallCredentialsDto`*
  - [x] `BoardCallDto` / `BoardCallParticipantDto`
  - [x] `BoardActionNotificationType.CallStarted/CallParticipantsChanged/CallEnded` (19–21)
  - [x] `CallStartedPayload` / `CallParticipantsChangedPayload` / `CallEndedPayload`
  - [x] **NEW** — `Contracts/DTOs/BoardCalls/AcsCallCredentialsDto.cs`: `public record AcsCallCredentialsDto(string Token, DateTimeOffset ExpiresOn, string AcsUserId, string RoomId);` — this is our own wrapper; **never expose an `Azure.Communication.*` SDK type through `Shared/Contracts`** (AD-3). `StartBoardCallCommand`/`JoinBoardCallCommand` return this alongside `BoardCallDto`.

- [x] **Task 3 — Backend: ACS infrastructure service** (AC: 1, 2, 3, 6)
  - [x] Added `Azure.Communication.Identity` 1.3.1 and `Azure.Communication.Rooms` 1.2.0 to `Infrastructure.csproj` only (both re-verified as current stable via the NuGet flat-container API at implementation time — no beta versions used). `Application.csproj` does not reference either package, per AD-1.
  - [x] `Application/Common/Enums/CallParticipantRole.cs` — `{ Attendee, Presenter }`, exactly as scoped.
  - [x] `Application/Interfaces/Services/IAcsCallService.cs` — implemented as scoped, with **one signature refinement**: `IssueTokenAsync` gained a `string roomId` parameter (`IssueTokenAsync(string acsUserId, string roomId, ...)`) so it can return a fully-formed `AcsCallCredentialsDto` (the original signature had no way to populate `AcsCallCredentialsDto.RoomId`).
  - [x] `Infrastructure/BoardCalls/AcsCallService.cs` — implemented against the real SDK surface (verified via Microsoft Learn API docs, not assumed): `CommunicationIdentityClient.CreateUserAsync()` / `.GetTokenAsync(CommunicationUserIdentifier, IEnumerable<CommunicationTokenScope>, ct)`; `RoomsClient.CreateRoomAsync(validFrom, validUntil, participants, ct)` / `.AddOrUpdateParticipantsAsync(roomId, participants, ct)` / `.RemoveParticipantsAsync(roomId, identifiers, ct)` / `.DeleteRoomAsync(roomId, ct)`. `RoomParticipant` takes a `CommunicationIdentifier` in its constructor and an settable `Role` (`ParticipantRole.Presenter`/`.Attendee` — both are static properties on an extensible `readonly struct`, not a plain enum). `EnsureUserIdentityAsync` is the only place a new ACS identity is ever created, persisted via `IUserRepository.Update` + `IUnitOfWork.SaveChangesAsync`.
  - [x] `Infrastructure/DI/Modules/AcsModule.cs` — registers `CommunicationIdentityClient`/`RoomsClient` as singletons (both constructed from the `AzureCommunicationServices` connection string via their `(string connectionString)` constructor overload, mirroring `BlobModule.cs`'s `BlobServiceClient` registration) and `IAcsCallService` as scoped; wired into `InfrastructureServiceCollectionExtensions.AddInfrastructure(...)`.
  - [x] Added `AzureCommunicationServices` connection string to `appsettings.json` (`ConnectionStrings`), `docker-compose.yml`'s `api` service environment block (`ConnectionStrings__AzureCommunicationServices=${AZURE_COMMUNICATION_SERVICES_CONNECTION}`), and `.env.example` (placeholder ACS connection-string format).

- [ ] **Task 4 — Backend: CQRS lifecycle commands** (AC: 1, 2, 3, 6, 7)
  - [ ] Extend `IBoardAccessService` with `EnsureCanStartCallAsync` (ScrumMaster/Admin) exactly as previously scoped — unaffected by the pivot
  - [ ] `StartBoardCallCommand`: `EnsureCanStartCallAsync` → throw `ConflictException` if an active call exists → `acsCallService.CreateRoomAsync()` → create the `BoardCall` row (`BoardId`, `StartedByUserId`, `StartedAt`, `AcsRoomId`) → `acsCallService.EnsureUserIdentityAsync(starterId)` → `AddOrUpdateParticipantAsync(roomId, acsUserId, Presenter)` (the starter is always a `Presenter`, regardless of `BoardRoleDto`, since only ScrumMaster/Admin can start) → `IssueTokenAsync` → publish `CallStarted` → return `(BoardCallDto, AcsCallCredentialsDto)`. **Do not write a `BoardCallParticipant` row here** — the starter's row is created by the Event Grid webhook once their Calling SDK client actually connects (AC 5).
  - [ ] `JoinBoardCallCommand`: `EnsureCanViewBoardAsync` (any member) → load the active call (404 if none) → `boardCallRepository.CountActiveParticipantsAsync` against `BoardCallConstants.MaxParticipants`, throw `ConflictException` if at cap → resolve the caller's `BoardRoleDto` → map to `CallParticipantRole` (ScrumMaster/Admin→Presenter, User→Attendee) → `EnsureUserIdentityAsync` → `AddOrUpdateParticipantAsync` → `IssueTokenAsync` → return `(BoardCallDto, AcsCallCredentialsDto)`. **Also does not write a participant row** — same reasoning as Start.
  - [ ] `LeaveBoardCallCommand`: best-effort revoke — `acsCallService.RemoveParticipantAsync(roomId, callerAcsUserId)`. This is defense-in-depth (stops them silently rejoining without a fresh `JoinBoardCallCommand`); it does **not** touch `BoardCallParticipant.LeftAt` or check "is the call now empty" — that is exclusively the webhook handler's job (Task 5) once `CallParticipantRemoved` actually fires.
  - [ ] `EndBoardCallCommand`: `EnsureCanStartCallAsync` (ScrumMaster/Admin) → `acsCallService.DeleteRoomAsync(roomId)` (revokes everyone's access at the ACS layer) → `IBoardCallLifecycleService.EndCallAsync` (sets `EndedAt`, idempotent, publishes `CallEnded`) — unconditional regardless of remaining participant count, same shape as originally designed.
  - [ ] `GetActiveBoardCallQuery`: unchanged from the original design — reads only our own `BoardCall`/`BoardCallParticipant` tables (which mirror Event-Grid-confirmed state), no ACS call needed.
  - [ ] `Application/Interfaces/Services/IBoardCallLifecycleService.cs` + implementation: **unchanged in shape** from the original design (`EndCallAsync` unconditional-idempotent, `EndCallIfEmptyAsync` idempotent check-then-act) — only its *caller* changes, from a Hangfire grace-period job to the Event Grid webhook handler (Task 5).
  - [ ] `Application/Common/Constants/BoardCallConstants.cs` — `public const int MaxParticipants = 4;` — the **single** place this number appears; every capacity check reads from here. See Dev Notes §7 for the future per-plan migration path this is designed to make trivial.
  - [ ] ~~`GetIceServersConfigurationQuery`~~ — **removed.** ACS manages its own relay infrastructure internally; there is no STUN/TURN configuration anywhere in this codebase.

- [ ] **Task 5 — Backend: Event Grid webhook ingestion** (AC: 5, 6)
  - [ ] Add `Azure.Messaging.EventGrid` 5.0.0 to `Presentation.csproj` (or `Infrastructure.csproj` if parsing lives there — controllers should stay thin per AD-1, so parse in a small Application/Infrastructure-level handler the controller just calls)
  - [ ] `Presentation/Controllers/BoardCallEventsController.cs` — thin controller, `[Route("api/internal/board-call-events")]` (lives under the existing `/api/internal` prefix so `InternalApiKeyMiddleware` already guards it with `X-Internal-Api-Key` — **no new auth mechanism needed**, per the confirmed decision). Body:
    1. Deserialize the request body as `EventGridEvent[]`.
    2. If the batch contains a `Microsoft.EventGrid.SubscriptionValidationEvent` (this fires once, at subscription-creation time in the Azure Portal/CLI), extract `ValidationCode` from its data and return it in a `SubscriptionValidationResponse` — **this handshake must be implemented correctly or the Event Grid subscription can never be created.**
    3. For every other event, dispatch via `ISender` based on `EventType`:
       - `Microsoft.Communication.CallParticipantAdded` → `RecordCallParticipantJoinedCommand(AcsRoomId, AcsParticipantRawId, OccurredAt)`
       - `Microsoft.Communication.CallParticipantRemoved` → `RecordCallParticipantLeftCommand(AcsRoomId, AcsParticipantRawId, OccurredAt)`
       - anything else → ignore (log at debug, do not throw)
  - [ ] `Application/Features/BoardCalls/Commands/RecordCallParticipantJoinedCommand.cs` — looks up the `BoardCall` by `AcsRoomId` (404/ignore if none — a stale/late event for an already-ended call is expected, not an error), resolves the `User` by reverse lookup on `AcsCommunicationUserId`, creates or reactivates the `BoardCallParticipant` row (mirrors the idempotent-rejoin logic originally scoped for `JoinBoardCallCommand` — that logic **moves here**), publishes `CallParticipantsChanged`.
  - [ ] `Application/Features/BoardCalls/Commands/RecordCallParticipantLeftCommand.cs` — same lookup, marks the matching active `BoardCallParticipant.LeftAt`, then calls `IBoardCallLifecycleService.EndCallIfEmptyAsync` — **this single call is what used to require a dedicated `IBoardCallPresenceTracker` Singleton + a 20-second Hangfire-scheduled job. ACS's own disconnect detection already provides that timing; we just react to its outcome.** Do not reintroduce a custom grace-period mechanism — trust the webhook's timing.
  - [ ] Both commands must be **idempotent** (safe to process the same event more than once — Event Grid's delivery guarantee is at-least-once, not exactly-once): use the same "no-op if already in the target state" pattern as `IBoardCallLifecycleService`.

- [ ] **Task 6 — Frontend: contracts, API client, store** (AC: all)
  - [ ] `Services/Api/IBoardCallsApi.cs` (Refit) — `StartAsync`, `JoinAsync`, `LeaveAsync`, `EndAsync`, `GetActiveAsync`, mirroring `ITasksApi`'s shape. `Start`/`Join` return `IApiResponse<StartOrJoinBoardCallResponse>` where `StartOrJoinBoardCallResponse` wraps `(BoardCallDto Call, AcsCallCredentialsDto Credentials)` — no `GetIceServersAsync` (removed with Task 4's query).
  - [ ] `Services/BoardCalls/BoardCallApiService.cs` + `IBoardCallApiService` — mirror `TaskApiService`'s `HandleResponseAsync()` pattern, unchanged in shape from the original design.
  - [ ] `Services.Abstractions/BoardCalls/IBoardCallStore.cs` + `Services/BoardCalls/Stores/BoardCallStore.cs` — **unchanged from the original design**: holds `BoardCallDto? ActiveCall`, `ApplyAction(BoardActionNotification, Guid)` switching on `CallStarted`/`CallParticipantsChanged`/`CallEnded`. This part of the pivot required no changes at all, since the indicator still rides `BoardActionNotification`.
  - [ ] Wire into `BoardActionsHubService.ConnectAsync`'s existing dispatch (third `.ApplyAction(...)` call) — unchanged from the original design.
  - [ ] Register everything in `Services/DI/ServiceCollectionExtensions.cs` per the existing paired-registration pattern.

- [ ] **Task 7 — Frontend: esbuild bundling + ACS Calling SDK JS interop** (AC: 2, 3, 4)
  - [ ] Add `src/Frontend/WebApp/package.json` — **this is a new build-tooling dependency this repo has never had** (the only existing JS, `fileDownload.js`, is hand-written vanilla with zero dependencies). Dependencies: `@azure/communication-calling` (^1.43.1), `esbuild` (devDependency, latest). Scripts:
    ```json
    {
      "scripts": {
        "build": "esbuild js-src/acsCallInterop.js --bundle --format=esm --outfile=wwwroot/js/acsCallInterop.bundle.js"
      }
    }
    ```
  - [ ] `src/Frontend/WebApp/js-src/acsCallInterop.js` — the bundler **entry point** (source lives outside `wwwroot`; only the bundled output is served). Imports `CallClient`, `LocalVideoStream`, etc. from `@azure/communication-calling`, exposes an ES module surface Blazor imports via `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/acsCallInterop.bundle.js")` — same dynamic-import mechanics as `fileDownload.js`, but **this module is stateful and must be cached, not re-imported per call** (same caveat the original `webrtcInterop.js` plan had, still true here even though the underlying transport is now ACS instead of raw WebRTC). Functions to expose:
    - `initCallAgent(userToken, displayName)` → `CallClient` → `createCallAgent`
    - `joinRoom(roomId)` → `callAgent.join({ roomId })`, wires `call.on('remoteParticipantsUpdated', ...)` back to a `DotNetObjectReference` callback so the Blazor component learns about remote video streams without polling
    - `toggleMic(enabled)` / `toggleCamera(enabled)` — ACS `Call.mute()`/`unmute()` and local video stream start/stop
    - `startScreenShare()` / `stopScreenShare()` — ACS's own `startScreenSharingAsync()`/`stopScreenSharingAsync()`. **ACS handles the underlying track replacement/renegotiation internally — do not hand-roll `replaceTrack` logic; that concern only existed in the raw-WebRTC design and no longer applies.**
    - `attachRenderer(streamId, videoElementId)` — ACS's `VideoStreamRenderer`, same element-must-exist-first ordering caveat as before (render the `<video>`/container element in Blazor first, then call this from `OnAfterRenderAsync`)
    - `leaveCall()` → `call.hangUp()`
  - [ ] `Services/BoardCalls/AcsCallInteropService.cs` (C#, replaces the old `WebRtcCallService`) — orchestrates `IBoardCallApiService` (fetch token via Start/Join) + the JS module; owns the local call session, exposes C# events the UI components subscribe to.
  - [ ] Update `src/Frontend/WebApp/Dockerfile` to run the bundler before `dotnet publish` (Node isn't present in the `mcr.microsoft.com/dotnet/sdk:10.0` build stage by default):
    ```dockerfile
    COPY ["src/Frontend/WebApp/package.json", "src/Frontend/WebApp/"]
    ...
    COPY . .
    WORKDIR "/src/src/Frontend/WebApp"
    RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - && apt-get install -y nodejs
    RUN npm ci && npm run build
    RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish
    ```
  - [ ] Document the local (non-Docker) dev step in the project's frontend README/CLAUDE.md-adjacent notes: `npm install && npm run build` must run at least once (and after any interop change) before `dotnet run`, since the bundle is a build artifact, not checked-in source.

- [ ] **Task 8 — Frontend: UI components** (AC: 1, 2, 3, 4, 6)
  - [ ] `BoardCallIndicator.razor` — **unchanged in shape** from the original design: Store-only injection (`IBoardCallStore` + `IBoardDetailsStore` for role check), mirrors `BoardExportControls.razor`'s live-status button pattern, must not repeat the AD-9 Store-bypass violation.
  - [ ] `BoardCallPanel.razor` — in-call surface: local video/audio controls, one tile per remote participant (rendered from `AcsCallInteropService`'s remote-participants event, not a raw stream map), mic/camera/screen-share toggle buttons, "Leave" button, (ScrumMaster/Admin only) "End call for everyone" button. Injects `IBoardCallStore` for who's-in-the-call state and a scoped `AcsCallInteropService` for the live session — same Store-vs-imperative-session split as originally designed, just with ACS underneath instead of raw WebRTC.

### Review Findings

_Code review of Task 1 & Task 2 implementation, 2026-07-25. Blind Hunter + Edge Case Hunter + Acceptance Auditor (vs. this spec + `project-context.md` + `ARCHITECTURE-SPINE.md`)._

- [x] [Review][Decision] `DeleteBehavior.Restrict` vs `Cascade` for `BoardCall`→`Board` and `BoardCallParticipant`→`BoardCall` — **Resolved: match sibling convention, `Cascade`.** `ColumnConfiguration`/`TaskItemConfiguration`'s `Cascade` pattern now applies here too; both FKs (EF config + `0020` SQL) changed from `Restrict` to `Cascade`. (The `→User` FKs on both entities, e.g. `StartedByUserId`, remain `Restrict` — unaffected by this decision.)
- [x] [Review][Decision] Thin real-time payloads (`CallStartedPayload`, `CallParticipantsChangedPayload`) — **Resolved: embed the full DTO**, matching `TaskCreatedPayload`/`CommentAddedPayload`. Avoids the N+1 where a client receives an ID and must immediately fire a follow-up request just to render `DisplayName`/`AvatarUrl`. `CallStartedPayload` now carries `BoardCallDto Call`; `CallParticipantsChangedPayload` now carries `IReadOnlyList<BoardCallParticipantDto> Participants` instead of bare `Guid`s. `CallEndedPayload` stays ID-only — there's nothing to render for an ended call, matching the sibling "deleted" payload shape where no richer data applies.

- [x] [Review][Patch] Hardcoded max-length literals instead of `Domain.Constants` — **Fixed.** Added `Domain/Constants/BoardCallConstants.cs` (`MaxAcsRoomIdLength = 128`, `MaxAcsCommunicationUserIdLength = 255`), referenced from both configs, matching `SubscriptionConstants`/`ColumnConstants`/`TaskItemConstants`. [`src/Backend/Persistence/Configurations/BoardCallConfiguration.cs`, `src/Backend/Persistence/Configurations/UserConfiguration.cs`]
- [x] [Review][Patch] Unique-index naming convention broken — **Fixed.** All 4 new indexes renamed `UX_...` → `IX_...` in `0020`/`0021`, matching every prior migration's exceptionless convention (unique or not, indexes are named `IX_`). [`src/Backend/Database/Scripts/0020_CreateBoardCallTables.sql`, `src/Backend/Database/Scripts/0021_AddAcsCommunicationUserIdToUsers.sql`]
- [x] [Review][Patch] Missing invariant `CHECK` constraints — **Fixed.** Added `CK_BoardCalls_EndedAfterStarted`, `CK_BoardCalls_AcsRoomId_NotEmpty`, `CK_BoardCallParticipants_LeftAfterJoined`, `CK_Users_AcsCommunicationUserId_NotEmpty` — inline in `0020`'s `CREATE TABLE`s (never applied, safe to amend) and via `ALTER TABLE ... ADD CONSTRAINT` in `0021` (matches `0015_AddArchivationFields.sql`'s pattern for an existing table), mirrored in each EF config via `HasCheckConstraint` (matching `ColumnConfiguration`/`TaskItemConfiguration`). [`src/Backend/Database/Scripts/0020_CreateBoardCallTables.sql`, `src/Backend/Database/Scripts/0021_AddAcsCommunicationUserIdToUsers.sql`, `src/Backend/Persistence/Configurations/BoardCallConfiguration.cs`, `BoardCallParticipantConfiguration.cs`, `UserConfiguration.cs`]
- [x] [Review][Patch] `BoardRepository.SoftDeleteCascadeAsync` not extended for the new tables — **Fixed.** Added `BoardCallParticipants` (joined via `p.BoardCall!.BoardId`) and `BoardCalls` cascade soft-delete steps, inserted after `BoardMembers` and before the final `Boards` update — same `ExecuteUpdateAsync` pattern as every sibling child table. [`src/Backend/Persistence/Repositories/BoardRepository.cs`]

- [x] [Review][Defer] Global per-user single-active-call-across-boards constraint isn't enforced (a user could be "active" in calls on two different boards at once) — deferred, speculative and not covered by any stated AC.
- [x] [Review][Defer] No compensating cleanup for a partial ACS-Room-created/DB-insert-failed (or vice versa) failure in the start/join flow — deferred, this is Task 3/4 (command handler) territory; nothing to implement yet at the Task 1/2 (schema-only) layer.
- [x] [Review][Defer] `src/Frontend/Services/Boards/BoardActionSyncKey.cs` doesn't map the 3 new `BoardActionNotificationType` values (falls through to the generic default case, which is not fatal) — deferred to Task 6, where the frontend Store consuming these types is actually built.
- [x] [Review][Defer] `User.AcsCommunicationUserId` has no issuance/rotation/revocation metadata, so a row can silently point at an ACS identity deleted independently on Azure's side — deferred, schema-only hardening for a later pass once Task 3's `IAcsCallService.EnsureUserIdentityAsync` exists to actually own this lifecycle.

### Findings dismissed as noise or already-decided
- Stale-active-row/heartbeat/TTL concern for crashed clients — superseded by design: the story's Task 5 (Event Grid `CallParticipantRemoved` webhook) replaces the need for this entirely; not a gap in Task 1/2.
- No workspace-entitlement/plan-gating wiring — already an explicit, documented product decision (Dev Notes §5/§7: calls are ungated for now).
- `CountActiveParticipantsAsync` has no caller yet — intentional scaffolding for Task 4's capacity check, working as designed.
- Migration comment citing "AC #1" — traceable to this story file's own AC numbering, not a real issue.
- SQL comment density in `0020` (Acceptance Auditor, informational only) — auditor itself concluded this is not a spec violation.
- Domain entity setters don't self-enforce `EndedAt >= StartedAt` invariants in code — inconsistent with this codebase's established anemic-entity convention (no sibling entity does this); the new DB `CHECK` constraint (patch above) is the actual authority per AD-5.
- Null-guard on `CallParticipantsChangedPayload.ParticipantUserIds` — inconsistent with the guard-free convention used by all 15+ sibling payload records.

### Review Findings — Task 3

_Code review of Task 3 (ACS infrastructure service) implementation, 2026-07-25. Blind Hunter + Edge Case Hunter + Acceptance Auditor (vs. this spec + `project-context.md` + `ARCHITECTURE-SPINE.md`)._

- [ ] [Review][Patch] `EnsureUserIdentityAsync` commits the Unit of Work itself — every other `SaveChangesAsync` call in this codebase is owned by the top-level command handler, never by a reusable Infrastructure service that's meant to be one step inside a larger flow. Once Task 4 calls this from within `JoinBoardCallCommand`, that handler would end up with two separate commits instead of one atomic one. [`src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`]
- [ ] [Review][Patch] `CallParticipantRole` placed in a brand-new `Application/Common/Enums/` folder instead of the established sibling convention — `BoardRole`/`WorkspaceRole` (the same kind of role enum) both live in `Domain/Enums/`. The story explicitly said to check for this before creating a new folder; that check was missed. [`src/Backend/Application/Common/Enums/CallParticipantRole.cs`]
- [ ] [Review][Patch] New top-level `Infrastructure/BoardCalls/` folder duplicates rather than extends the existing convention — board-related infrastructure already groups under `Infrastructure/Boards/{Export,Jobs,Notifiers}`; this should be `Infrastructure/Boards/Calls/`. [`src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`]
- [ ] [Review][Patch] No `RequestFailedException` translation anywhere in `AcsCallService` — every Azure SDK call (`CreateUserAsync`, `CreateRoomAsync`, `AddOrUpdateParticipantsAsync`, `RemoveParticipantsAsync`, `DeleteRoomAsync`, `GetTokenAsync`) can throw `Azure.RequestFailedException`, none of it is mapped to this project's `AppException` subtypes, so it falls through `GlobalExceptionHandler` to a generic 500, losing real status semantics. `RemoveParticipantAsync`/`DeleteRoomAsync` should additionally treat a 404 as an idempotent no-op, matching the "leave"/"end call" semantics Task 4 will build on top of. [`src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`]
- [ ] [Review][Patch] No input validation on any public method — `roomId`/`acsUserId` string parameters have zero guard clauses, unlike `StripeSubscriptionsService`'s established `ArgumentException.ThrowIfNullOrWhiteSpace(...)` convention. [`src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`]
- [ ] [Review][Patch] `AcsModule`'s connection-string check (`?? throw`) doesn't catch an empty/whitespace value — `docker-compose` substitutes `""` when `AZURE_COMMUNICATION_SERVICES_CONNECTION` is unset, which would surface as an opaque SDK parsing error instead of the intended clear startup error. [`src/Backend/Infrastructure/DI/Modules/AcsModule.cs`]
- [ ] [Review][Patch] No logging anywhere in `AcsCallService` — every method makes an external network call to Azure; a production failure (auth expiry, throttling, transient fault) leaves no trace beyond an unhandled exception at the global handler. [`src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`]
- [ ] [Review][Patch] `.env.example` still has no trailing newline after this diff added a second line — violates `.editorconfig`'s top-level `insert_final_newline = true`, and this diff touched the file anyway. [`.env.example`]

- [x] [Review][Defer] Race condition in `EnsureUserIdentityAsync`'s check-then-act (two concurrent requests for the same user both see no `AcsCommunicationUserId` and both call `CreateUserAsync`) — real but narrow (double-click/multi-tab); the DB unique index (`IX_Users_AcsCommunicationUserId`) at least prevents silent duplicate persistence. Proper handling (catch-and-retry on unique violation) needs Task 4's actual command handler to exist.
- [x] [Review][Defer] Broadened the existing `deferred-work.md` item on ACS/DB partial-failure cleanup to explicitly also cover `EnsureUserIdentityAsync` (an ACS identity created but the surrounding save fails) — same class of problem as the already-deferred Room-creation case, not just Room-specific.
- [x] [Review][Defer] `CreateRoomAsync` always passes `validFrom: null, validUntil: null`, deferring to ACS's default room TTL with no reconciliation against `BoardCall.EndedAt`'s lifecycle — Task 4's call once an actual call-duration business rule exists.
- [x] [Review][Defer] No ACS emulator exists (unlike Azurite for Blob Storage) — local dev requires a real (or shared dev) ACS resource to boot the API at all once the connection-string check is strict. Genuine DX gap, folded into the existing dev-tunnel note in Dev Notes.

### Findings dismissed as noise, already-decided, or a misread of the ACS security model
- `IssueTokenAsync` not verifying room membership before issuing a token — misreads the ACS model: the token is a general VoIP-scope credential, not room-scoped; Room membership is enforced by ACS itself at join time, not by token issuance.
- "`AcsModule` should use an `Options` class instead of inline `?? throw`" — `DatabaseModule.cs` already establishes an equally valid inline-connection-string-check precedent for exactly this kind of single-value config; `Options` classes are for multi-field structured config (`BoardExportSchedulerOptions`), not warranted here.
- "`AcsModule`'s `InvalidOperationException` should be an `AppException`" — wrong exception category; this fires at DI-composition/startup time, before any HTTP request exists to render a `ProblemDetails` response for, and matches `DatabaseModule.cs`'s identical pattern.
- `IAcsCallService` has zero consumers in this diff — working as designed; Task 4 is where it gets called.
- Package version pinning "has no stated rationale" — both versions were verified directly against the live NuGet API before being pinned (see Task 3's Completion Notes); just not visible from the diff alone.
- `ToAcsRole`'s `ArgumentOutOfRangeException` default arm called "dead code" — standard idiomatic exhaustive-switch defensive pattern, not a real gap.
- Pre-existing `Application.csproj` → `Azure.Storage.Blobs` AD-1 tension — explicitly not introduced by this diff, out of scope.

## Dev Notes

### §1 — Entity shape (as implemented + required additions)

```csharp
// Domain/Entities/BoardCall.cs — ADD AcsRoomId
public class BoardCall : BaseEntity<Guid>
{
    public required Guid BoardId { get; set; }
    public required Guid StartedByUserId { get; set; }
    public required string AcsRoomId { get; set; }   // NEW
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public Board? Board { get; set; }
    public ICollection<BoardCallParticipant> Participants { get; set; } = [];
}

// Domain/Entities/User.cs — ADD AcsCommunicationUserId
public class User : BaseEntity<Guid>
{
    public required Guid AzureAdObjectId { get; init; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AcsCommunicationUserId { get; set; }   // NEW — provisioned lazily by IAcsCallService.EnsureUserIdentityAsync
}
```
`BoardCallParticipant` is unchanged from the original implementation.

### §2 — Migrations

`0020_CreateBoardCallTables.sql` gains the `AcsRoomId` column + its unique index (amend in place — safe, since it has not been applied anywhere yet). `0021_AddAcsCommunicationUserIdToUsers.sql` is new (shown in Task 1) — `Users` already exists and has prior migrations, so this must be a fresh script, never folded backward into an earlier one.

### §3 — Why the entire Hangfire/presence-tracker/dedicated-hub mechanism is gone

The original Task 5 existed to solve one problem: *how do we know when the last person has left a call, tolerating a brief disconnect?* That required us to build presence detection ourselves (`IBoardCallPresenceTracker` Singleton + `OnDisconnectedAsync` + a 20-second Hangfire-scheduled idempotent check) because a hand-rolled SignalR hub has no built-in notion of "call membership," only "connection membership." **ACS already solves this** — it tracks real call/participant state itself and emits `CallParticipantRemoved` via Event Grid on its own timing. We do not control or need to reimplement that timing; `RecordCallParticipantLeftCommand` → `IBoardCallLifecycleService.EndCallIfEmptyAsync` is the entire replacement, and it is simpler specifically *because* it's reactive to an authoritative external signal instead of something we had to detect ourselves.

### §4 — Access control (unchanged)

`EnsureCanStartCallAsync` (ScrumMaster/Admin, added to `IBoardAccessService`) and reuse of `EnsureCanViewBoardAsync` (any member, for join/leave/get) are unaffected by the pivot — see the original architecture-spine references. `BoardRolePermissions.CanStartCall` still needs adding.

### §5 — No plan gating for *access*; the participant *cap* is a different axis

Per the standing decision, the call feature itself is **not** gated by subscription plan (unlike `board.export`). The 4-participant **cap**, however, is explicitly designed to become a per-plan `Limits` entry later (mirroring `MaxBoardsPerWorkspace`/`MaxMembersPerWorkspace` in `PlanOptions`/`appsettings.json` — a numeric `Limits` field, not a `Features` list entry, since it's a capacity constraint, not a feature switch). `BoardCallConstants.MaxParticipants` is deliberately the *only* place the number `4` appears, so that future work is: add `MaxCallParticipants` to `PlanLimitsDto`, replace the constant reference in `JoinBoardCallCommandHandler` with `entitlementService`'s resolved plan limit. Nothing else in this story should hardcode participant-count logic anywhere else.

### §6 — Event Grid subscription setup is an Azure-portal/CLI step, not code in this repo

The Event Grid → webhook wiring requires an **Event Grid System Topic on the ACS resource with a Webhook subscription pointing at `/api/internal/board-call-events`** — this is Azure infrastructure configuration (portal, CLI, or a future IaC pass), not something DbUp/EF/docker-compose can express today. The architecture spine already flags "no deployment topology/IaC exists" as a standing open item; this story adds one more thing that will eventually need to live there. For now, document the manual `az eventgrid` CLI command (or Portal steps) needed to create the subscription in the story's implementation notes once Task 5 is built.

### §7 — Local dev/testing: exposing the webhook endpoint

`docker-compose`/`dotnet run` alone cannot receive real Azure Event Grid deliveries, since there's no public HTTPS endpoint for a laptop. To test this end-to-end locally:
1. Run the API locally (`dotnet run` in `Presentation`, or via `docker-compose`).
2. Expose it with a tunnel — either `ngrok http 8080` or Visual Studio/`devtunnel` (`devtunnel host -p 8080 --allow-anonymous`) — and note the resulting public HTTPS URL.
3. Point the Event Grid subscription's webhook endpoint at `https://<tunnel-host>/api/internal/board-call-events` (include the `X-Internal-Api-Key` header in the subscription's delivery configuration — Event Grid supports custom delivery headers).
4. Re-run the `SubscriptionValidationEvent` handshake (Event Grid triggers this automatically on subscription creation) — confirm the controller responds correctly before relying on real event delivery.
5. Remember to update the tunnel URL if it changes between sessions (free ngrok URLs are not stable across restarts; `devtunnel` persistent tunnels avoid this).

### §8 — ACS role mapping

| `BoardRoleDto` | ACS `ParticipantRole` |
| --- | --- |
| `ScrumMaster`, `Admin` | `Presenter` |
| `User` | `Attendee` |

This is enforced at both layers deliberately: our own CQRS checks decide *whether* someone can start/join at all; the ACS role additionally constrains what they can do *inside* the room (e.g. only a `Presenter` can remove another participant at the ACS layer) — defense in depth, not redundant.

### §9 — Token lifetime (explicitly deferred, not forgotten)

`IssueTokenAsync` issues a single ACS access token at join time with no refresh mechanism. This is intentional for this story's scope (bounded Daily-Scrum-length calls) — **but implementing token refresh is a strong candidate for a near-future story**, since ACS tokens do expire and a sufficiently long call (or a call left open in a background tab) will eventually lose its token. Flagging this explicitly so it isn't lost: **next-story candidate — ACS token refresh before expiry.**

### §10 — Testing

No test project exists in this solution (`project-context.md`) — do not add test files or invent a framework. `dotnet build` is the verification gate, consistent with how Tasks 1–2 were validated.

### Project Structure Notes

- `Infrastructure/BoardCalls/AcsCallService.cs` and `Infrastructure/DI/Modules/AcsModule.cs` follow the exact same Infrastructure-implements/Application-declares split as every other external-facing service (Blob, Stripe, Service Bus) — no new pattern introduced here, just a new instance of the existing one (AD-1).
- `src/Frontend/WebApp/js-src/` is a **new** top-level source folder, distinct from `wwwroot/js/` (build *output*) — do not put the esbuild entry point inside `wwwroot`, and do not check the generated `wwwroot/js/acsCallInterop.bundle.js` into source control expectations without also documenting the build step (add `wwwroot/js/*.bundle.js` to `.gitignore` if the team decides build artifacts shouldn't be committed — confirm this convention when Task 7 is implemented, it wasn't decided in this pass).
- `Application/Common/Enums/CallParticipantRole.cs` exists specifically to keep the Azure SDK's own role type out of the Application layer — check whether `Application/Common/Enums/` already exists as a folder before creating it; if the codebase's existing convention puts small enums elsewhere (e.g. `Domain/Enums/`), follow that instead and note the deviation.

### References

- [Source: `_bmad-output/project-context.md`], [Source: `_bmad-output/planning-artifacts/architecture/architecture-TaskTracker-2026-07-25/ARCHITECTURE-SPINE.md`] — AD-1 (dependency direction — `IAcsCallService` interface in Application, SDK usage confined to Infrastructure), AD-3 (Shared/Contracts wire authority — `AcsCallCredentialsDto` wraps ACS SDK types, never leaks them), AD-5 (DbUp is schema authority — both migrations follow numbered-script convention), AD-6 (error contract — the new webhook controller is a plain HTTP controller and gets full `GlobalExceptionHandler` coverage, unlike a SignalR hub; this pivot introduces no new instance of that known gap), AD-7 (explicit resource-scoped authorization, extended with `EnsureCanStartCallAsync` — unchanged by the pivot), AD-9 (Store-only frontend call chain — `BoardCallIndicator`/`BoardCallPanel` still must not bypass the Store)
- [Source: `src/Backend/Infrastructure/DI/Modules/BlobModule.cs`] — pattern to mirror for `AcsModule.cs`'s client registration
- [Source: `src/Backend/Presentation/Middlewares/InternalApiKeyMiddleware.cs`] — the `/api/internal` guard the new webhook controller reuses
- [Source: `src/Frontend/WebApp/wwwroot/js/fileDownload.js`] — existing JS interop precedent (dynamic-import mechanics only; this feature's bundling/stateful-module concerns are new)
- [Source: `src/Frontend/WebApp/Dockerfile`] — base image confirmed to have no Node.js; the npm build step must install it explicitly
- Web (current at time of writing, verify again before implementation — Azure SDKs ship frequently): `Azure.Communication.Identity` 1.3.1, `Azure.Communication.Rooms` 1.2.0, `Azure.Messaging.EventGrid` 5.0.0, `@azure/communication-calling` 1.43.1 (npm). ACS Rooms + Event Grid: `CallParticipantAdded`/`CallParticipantRemoved` confirmed as real, current event types delivered through Event Grid's standard voice/video calling event mechanism (not Call-Automation-only). ACS group calls/Rooms support up to 350 participants and a 30-hour max call duration.

## Change Log

- **2026-07-25 — Architectural pivot (specification):** Replaced the custom P2P WebRTC mesh design (dedicated `BoardCallHub`, hand-rolled SDP/ICE signaling over SignalR, STUN/TURN via Open Relay) with **Azure Communication Services** (Identity + Rooms + `@azure/communication-calling`). Participant presence is now driven by Azure Event Grid webhooks instead of a custom Hangfire grace-period job. Tasks 3–8 rewritten; Tasks 1–2 retained with two required schema additions (`BoardCall.AcsRoomId`, `User.AcsCommunicationUserId`). Introduces a new frontend build-tooling dependency (esbuild + `package.json`) that did not exist in this repo before.
- **2026-07-25 — Task 1–2 ACS additions implemented:** `BoardCall.AcsRoomId` added and `0020_CreateBoardCallTables.sql` amended in place (never applied to any database). `User.AcsCommunicationUserId` added via new `0021_AddAcsCommunicationUserIdToUsers.sql`. `AcsCallCredentialsDto` added to `Shared/Contracts`. `dotnet build` verified 0 errors. Tasks 3–8 remain unimplemented.
- **2026-07-25 — Code review of Task 1/2, findings resolved:** 2 decisions made (`BoardCall`/`BoardCallParticipant` FKs now `Cascade` on Board/BoardCall delete, matching `Column`/`Task`'s sibling convention; `CallStartedPayload`/`CallParticipantsChangedPayload` now embed full DTOs instead of bare IDs, matching `TaskCreatedPayload`/`CommentAddedPayload`), 4 patches applied (`BoardCallConstants` extracted, index naming `UX_`→`IX_`, 4 new `CHECK` constraints added, `BoardRepository.SoftDeleteCascadeAsync` extended to cascade to `BoardCalls`/`BoardCallParticipants`). 4 items deferred to `deferred-work.md` (cross-board active-call constraint, ACS/DB partial-failure cleanup, frontend `BoardActionSyncKey` mapping, ACS identity lifecycle metadata) — all out of Task 1/2's scope. `dotnet build` verified 0 errors after all changes.
- **2026-07-25 — Task 3 implemented:** ACS infrastructure service (`IAcsCallService`/`AcsCallService`) built against the real `Azure.Communication.Identity` 1.3.1 / `Azure.Communication.Rooms` 1.2.0 SDK surface (verified via Microsoft Learn API docs before writing the code, not assumed). One signature refinement from the original spec: `IssueTokenAsync` gained a `roomId` parameter so it can return a complete `AcsCallCredentialsDto`. `dotnet build` verified 0 errors, no new warnings.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 5 (claude-sonnet-5)

### Debug Log References

- `dotnet build TaskTracker.sln` — succeeded, 0 errors, 17 pre-existing warnings (initial Task 1–2 implementation, pre-pivot).
- `dotnet build TaskTracker.sln` — succeeded, 0 errors, same 17 pre-existing warnings, none new (ACS pivot additions to Task 1–2).
- `dotnet build TaskTracker.sln` — succeeded, 0 errors, same 17 pre-existing warnings, none new (post code-review fixes).
- `dotnet build TaskTracker.sln` — succeeded, 0 errors, same 17 pre-existing warnings, none new (this session — Task 3, ACS infrastructure service).

### Completion Notes List

- Tasks 1–2 implemented under the original (pre-pivot) design in an earlier session.
- This session: implemented the two ACS-pivot schema additions flagged in the story revision, and the one new Contracts DTO — nothing else. Tasks 3–8 remain untouched, as scoped by the user's request.
  - `BoardCall.AcsRoomId` (required `string`, max 128) added to the entity, `BoardCallConfiguration` (required, unique index), and amended directly into `0020_CreateBoardCallTables.sql` (safe to amend in place — never applied to any database).
  - `User.AcsCommunicationUserId` (nullable `string`, max 255) added to the entity and `UserConfiguration` (partial unique index, `WHERE "AcsCommunicationUserId" IS NOT NULL`, so multiple users without one don't collide). Landed in a **new** `0021_AddAcsCommunicationUserIdToUsers.sql` rather than amending `0001` — `Users` already has prior applied migrations, so it cannot be retroactively amended (only `0020` could be, since it was never shipped).
  - `Contracts/DTOs/BoardCalls/AcsCallCredentialsDto.cs` added — a plain wrapper record, no `Azure.Communication.*` SDK types referenced (AD-3 compliance; the SDK isn't even a dependency yet, since Task 3 hasn't started).
- Verification: `dotnet build TaskTracker.sln` (0 errors) — no test project exists in this solution (`project-context.md`), so build success remains the gate, consistent with the original Task 1–2 pass.
- Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) ran against the full Task 1/2 diff. 2 `decision-needed` findings resolved by the user (FK cascade behavior, payload richness — see Review Findings above), 4 `patch` findings applied, 4 deferred (logged in `deferred-work.md`, all genuinely out of Task 1/2 scope), 7 dismissed (already-decided, working-as-designed, or inconsistent with established codebase conventions). Status kept at `in-progress` rather than `done` — the review was explicitly scoped to Task 1/2 only; Tasks 3–8 remain unimplemented, so the story as a whole is not complete.
- Task 3 implemented: `IAcsCallService`/`AcsCallService` (Infrastructure) + `CallParticipantRole` enum (Application) + `AcsModule` DI wiring + connection-string plumbing. Every Azure SDK call was checked against the actual Microsoft Learn API reference before writing it (constructor overloads, method signatures, the fact that `ParticipantRole`/`CommunicationTokenScope` are extensible `readonly struct`s with static properties, not plain enums) — this avoided several plausible-but-wrong guesses (e.g. `RoomParticipant` takes a `CommunicationIdentifier` in its constructor with `Role` set via object initializer, not a constructor overload with a role parameter). `IssueTokenAsync` gained a `roomId` parameter beyond the original story spec — a necessary correction since the original signature couldn't have populated `AcsCallCredentialsDto.RoomId`. No entitlement/plan-gating check was added (still an explicit non-goal per Dev Notes §5/§7). `dotnet build` — 0 errors, no new warnings.

### File List

**Added (Task 3, this session):**
- `src/Backend/Application/Common/Enums/CallParticipantRole.cs`
- `src/Backend/Application/Interfaces/Services/IAcsCallService.cs`
- `src/Backend/Infrastructure/BoardCalls/AcsCallService.cs`
- `src/Backend/Infrastructure/DI/Modules/AcsModule.cs`

**Modified (Task 3, this session):**
- `src/Backend/Infrastructure/Infrastructure.csproj` (added `Azure.Communication.Identity` 1.3.1, `Azure.Communication.Rooms` 1.2.0)
- `src/Backend/Domain/Constants/ConnectionStrings.cs` (added `AzureCommunicationServices`)
- `src/Backend/Infrastructure/DI/InfrastructureServiceCollectionExtensions.cs` (wired `AddAcsModule`)
- `src/Backend/Presentation/appsettings.json` (added `AzureCommunicationServices` connection string)
- `docker-compose.yml` (added `ConnectionStrings__AzureCommunicationServices` to the `api` service)
- `.env.example` (added `AZURE_COMMUNICATION_SERVICES_CONNECTION` placeholder)

**Added (code review fixes, this session):**
- `src/Backend/Domain/Constants/BoardCallConstants.cs`
- `_bmad-output/implementation-artifacts/deferred-work.md`

**Modified (code review fixes, this session):**
- `src/Backend/Persistence/Configurations/BoardCallConfiguration.cs` (constant reference, `Cascade`, 2 `CHECK` constraints)
- `src/Backend/Persistence/Configurations/BoardCallParticipantConfiguration.cs` (`Cascade`, 1 `CHECK` constraint)
- `src/Backend/Persistence/Configurations/UserConfiguration.cs` (constant reference, 1 `CHECK` constraint)
- `src/Backend/Database/Scripts/0020_CreateBoardCallTables.sql` (amended again — `CASCADE` FKs, 3 `CHECK` constraints, `UX_`→`IX_` renames)
- `src/Backend/Database/Scripts/0021_AddAcsCommunicationUserIdToUsers.sql` (1 `CHECK` constraint, `UX_`→`IX_` rename)
- `src/Backend/Persistence/Repositories/BoardRepository.cs` (`SoftDeleteCascadeAsync` extended for `BoardCalls`/`BoardCallParticipants`)
- `src/Shared/Contracts/Notifications/BoardActions/Payloads/CallStartedPayload.cs` (now carries `BoardCallDto`)
- `src/Shared/Contracts/Notifications/BoardActions/Payloads/CallParticipantsChangedPayload.cs` (now carries `IReadOnlyList<BoardCallParticipantDto>`)

**Added (ACS pivot, prior session):**
- `src/Backend/Database/Scripts/0021_AddAcsCommunicationUserIdToUsers.sql`
- `src/Shared/Contracts/DTOs/BoardCalls/AcsCallCredentialsDto.cs`

**Modified (ACS pivot, prior session):**
- `src/Backend/Domain/Entities/BoardCall.cs` (added `AcsRoomId`)
- `src/Backend/Domain/Entities/User.cs` (added `AcsCommunicationUserId`)
- `src/Backend/Persistence/Configurations/BoardCallConfiguration.cs` (`AcsRoomId` mapping + unique index)
- `src/Backend/Persistence/Configurations/UserConfiguration.cs` (`AcsCommunicationUserId` mapping + partial unique index)
- `src/Backend/Database/Scripts/0020_CreateBoardCallTables.sql` (amended in place — added `AcsRoomId` column + unique index)

**Added (prior session, Tasks 1–2 initial implementation, unaffected by this session):**
- `src/Backend/Domain/Entities/BoardCall.cs` (base shape)
- `src/Backend/Domain/Entities/BoardCallParticipant.cs`
- `src/Backend/Persistence/Configurations/BoardCallConfiguration.cs` (base shape)
- `src/Backend/Persistence/Configurations/BoardCallParticipantConfiguration.cs`
- `src/Backend/Database/Scripts/0020_CreateBoardCallTables.sql` (base shape)
- `src/Backend/Application/Interfaces/Repositories/IBoardCallRepository.cs`
- `src/Backend/Persistence/Repositories/BoardCallRepository.cs`
- `src/Shared/Contracts/DTOs/BoardCalls/BoardCallDto.cs`
- `src/Shared/Contracts/DTOs/BoardCalls/BoardCallParticipantDto.cs`
- `src/Shared/Contracts/Notifications/BoardActions/Payloads/CallStartedPayload.cs`
- `src/Shared/Contracts/Notifications/BoardActions/Payloads/CallParticipantsChangedPayload.cs`
- `src/Shared/Contracts/Notifications/BoardActions/Payloads/CallEndedPayload.cs`

**Modified (prior session):**
- `src/Backend/Persistence/Contexts/TaskTrackerDbContext.cs`
- `src/Backend/Persistence/DI/Modules/RepositoriesModule.cs`
- `src/Shared/Contracts/Notifications/BoardActions/BoardActionNotificationType.cs`
