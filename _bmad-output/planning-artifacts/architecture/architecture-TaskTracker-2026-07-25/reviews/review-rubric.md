---
name: 'review-rubric-TaskTracker-2026-07-25'
type: architecture-review
reviewed_artifact: '../ARCHITECTURE-SPINE.md'
date: '2026-07-25'
status: complete
---

# Architecture Spine Review — TaskTracker

## Verdict

Structurally a good spine — terse, correctly scoped, and almost entirely code-verified — but it is not yet a fully
accurate ratification of the brownfield: AD-9 is marked `[ADOPTED]` while being actively violated in three places,
and AD-7 names only half of the actual two-service authorization convention it exists to police. Both are fixable
edits, not rewrites.

## Findings (ordered by severity)

### 1. [HIGH] AD-9 (frontend call chain) is marked ADOPTED but is already violated in three places

Rule: "Components/pages inject the Store only." Verified against code — three components inject and directly call
an `*ApiService`, bypassing the Store entirely:

- `src/Frontend/WebApp/Pages/Workspaces/Components/WorkspaceSubscription.razor:7` — `@inject ISubscriptionApiService SubscriptionApiService`, called at lines 128/145 (`CreateCheckoutSessionAsync`, `CreatePortalSessionAsync`)
- `src/Frontend/WebApp/Pages/Workspaces/Components/WorkspaceActiveBoards.razor:3` — `@inject IBoardApiService BoardApiService`, called at lines 259/280/310/340 (`CreateBoardAsync`, `UpdateBoardAsync`, `DeleteBoardAsync`, `LeaveBoardAsync`)
- `src/Frontend/WebApp/Components/BoardExportControls/BoardExportControls.razor:10` — `@inject IBoardApiService BoardApi`

`[ADOPTED]` status asserts this is current, enforced reality; it isn't. Either fix the three call sites (route
through the corresponding Store) or reword the AD as a target invariant with named, tracked exceptions — as written
an agent will trust the rule and miss that the pattern is already broken in exactly the files it would most likely
copy from (Board/Subscription features).

### 2. [HIGH] AD-7 names only one of two parallel authorization services — the rule under-covers its own subject

AD-7's rule requires calling `IWorkspaceAccessService.EnsureIsMemberAsync` (or "the equivalent role check") as a
handler's first action. In practice there are **two** parallel access-control interfaces in
`Application/Interfaces/Services`, not one:

- `IWorkspaceAccessService` (`EnsureIsMemberAsync`, `EnsureCanManageWorkspaceAsync`, `EnsureCanChangeMemberRoleAsync`, `EnsureCanDeleteWorkspaceAsync`, `EnsureCanManageInvitesAsync`, `EnsureCanManageBoardMembersAsync`, `EnsureCanManageSubscriptionsAsync`) — used by Workspace-scoped handlers.
- `IBoardAccessService` (`EnsureCanViewBoardAsync`, `EnsureCanManageColumnsAsync`, and others) — used by Board/Column/Task/Comment/Attachment-scoped handlers (`GetBoardByIdQuery`, `CreateColumnCommand`, `LeaveBoardCommand`, `ArchiveAndExportCommand`, `GetTaskByIdQuery`, `CreateCommentCommand`, `UploadAttachmentCommand`, etc.) — a **larger** surface (~40+ files) than the Workspace-only one AD-7 names.

AD-7 exists specifically because this is "the one access-control point in the system that is not enforced
structurally" — its entire value is that a reviewer/agent can grep for the named call and know a handler is
covered. As written, the rule only names half the actual convention: an agent auditing a new Board/Column/Task
handler against AD-7 literally ("does it call `IWorkspaceAccessService`?") gets a false negative for the more common
path. The "(or the equivalent role check)" hedge is too vague to substitute for naming `IBoardAccessService`
explicitly. Fix: name both services and state which resource types route to which.

### 3. [MEDIUM] Observability/tracing strategy is a silent cross-service dimension, not decided or deferred

The Stack table and AD-8 correctly note Functions is intentionally decoupled from the API's DI graph, but the spine
never addresses observability as a whole-system concern even though the two runtimes already diverge on it:
`TaskTracker.Functions.csproj` includes `Microsoft.Azure.Functions.Worker.OpenTelemetry` (1.2.0) — a real dependency,
also listed in `project-context.md`'s stack table — while the Backend API has only Serilog structured logging, no
OpenTelemetry/tracing. This is exactly the kind of "operational envelope" dimension the checklist calls out: it's
fine if the answer is "intentionally divergent, no unified tracing yet" or "deferred," but right now it's simply
absent from both the Stack table and the Deferred section, so a future agent has no way to know whether adding
tracing to the API should mirror the Functions approach or whether they are meant to diverge permanently.

### 4. [LOW] Minor identifier drift — spine wasn't grep-verified against every filename

The actual file is `BoardExportRecoveryShedulerJob.cs` (typo — missing 'c' in "Sheduler"). Purely cosmetic and
doesn't affect the rule's enforceability, but it's a signal the spine's specific identifiers weren't checked
against the tree file-by-file.

## Checklist-by-checklist assessment

1. **Real divergence points fixed, none missing?** — Mostly yes. The nine ADs cover the load-bearing forks
   (dependency direction, CQRS/pipeline shape, wire-format ownership, entitlement gating, schema authority, error
   contract, workspace auth, async-work split, frontend call chain). The one gap worth naming is observability
   (finding 3) — everything else a new feature would plausibly fork on is covered.
2. **Is every AD's Rule enforceable and does it actually prevent its stated divergence?** — Eight of nine hold up
   under direct inspection (AD-1 through AD-6, AD-8 confirmed by reading the actual files/registration order). AD-7
   is enforceable but under-specifies its own subject (finding 2); AD-9 is enforceable as written but already false
   as a statement of current reality (finding 1).
3. **Could anything under Deferred have been an AD instead?** — No. WebRTC calls: confirmed zero diff between
   `main` and `feature/webrtc-calls` — genuinely no code exists to pin down yet, correctly deferred. Production
   deployment topology: confirmed no `.github`, no Bicep/Terraform in the repo — correctly deferred rather than
   invented.
4. **Is named tech plausible as current?** — Yes, cross-checked every version in the Stack table against the
   actual `.csproj` files (`Presentation.csproj`, `Application.csproj`, `Infrastructure.csproj`, `Persistence.csproj`,
   `WebApp.csproj`, `TaskTracker.Functions.csproj`) — all exact matches, no mismatched version families. One omission
   (finding 3, OpenTelemetry) rather than a wrong entry.
5. **Does it ratify the brownfield rather than contradict/reinvent it?** — Mostly, but not fully: AD-9 doesn't
   ratify current reality, it contradicts it (finding 1), and AD-7 ratifies an incomplete picture of the real
   convention (finding 2). AD-1 through AD-6, AD-8 are clean ratifications, each verified line-for-line against the
   actual DI registration, exception hierarchy, and middleware.
6. **Is every structural dimension at this altitude decided/deferred/flagged, including the operational envelope?**
   — The environment envelope itself is handled well: "local development only... no staging/production topology
   exists yet" is stated plainly and cross-referenced into Deferred, matching the confirmed absence of CI/CD or IaC.
   Observability (finding 3) is the one dimension left genuinely silent rather than decided/deferred.
7. **Is the spine terse, or has rationale crept in?** — Terse. Each AD's "Prevents"/"Rule" pair stays to 1-3 lines;
   no narrative justification, retrospective framing, or multi-paragraph rationale crept into the document itself.

## Verification method

Every AD and Stack entry was checked directly against source, not inferred: `Domain.csproj`/`Application.csproj`
project references (AD-1), `ApplicationServiceCollectionExtensions.cs` behavior registration order (AD-2),
`IRequireWorkspaceFeature`/`WorkspaceFeatureGuardBehavior`/`SubscriptionFeatureRequiredException` (AD-4),
`AppException`/`NotFoundException`/`GlobalExceptionHandler`/`ApiResponseExtensions` (AD-6),
`IWorkspaceAccessService`/`IBoardAccessService` usage across `Application/Features` (AD-7),
`BoardExportSchedulerJob`/`InternalApiKeyMiddleware` (AD-8), `.razor` `@inject` directives across `WebApp` (AD-9),
`SensitiveDataDestructuringPolicy.cs` (redaction list), `docker-compose.yml` (environment envelope), and
`git log`/`git diff main..feature/webrtc-calls` plus a repo-wide search for `.github`/`*.bicep`/`*.tf` (Deferred
section accuracy).
