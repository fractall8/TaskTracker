---
name: 'TaskTracker Architecture Spine — Adversarial Review'
type: review
reviewed-doc: '../ARCHITECTURE-SPINE.md'
created: '2026-07-25'
---

# Adversarial Review — ARCHITECTURE-SPINE.md (TaskTracker)

Method: for every AD and Consistency Convention row, construct two units (teams/agents/features) that each
follow the stated Rule to the letter yet still ship something incompatible. Where possible, findings are
grounded in the actual codebase (file:line evidence) rather than left as pure hypotheticals — several of the
constructed "two teams diverge" scenarios turned out to already be true of the current code, not just risks.

**Headline finding:** AD-4 as written describes a mechanism (`WorkspaceFeatureGuardBehavior` /
`IRequireWorkspaceFeature`) that **no command in the codebase actually uses**. Every real feature-gated command
(`ArchiveAndExportCommand`, `ReExportArchivedBoardCommand`, `GetBoardArchiveDownloadQuery`) hand-rolls the same
`entitlementService.HasFeatureAsync(...)` check inline in the handler — precisely the pattern AD-4 says it
"prevents." The spine is documenting an aspiration, not the load-bearing reality, on the exact AD it calls
"ADOPTED." This is not a future risk; it is a present divergence between three already-written handlers, each a
separate hand-copy of the same check with no shared enforcement point.

---

## AD-1 — Backend dependency direction

**Rule as written** only constrains Presentation → Infrastructure/Persistence → Application → Domain. It says
nothing about whether **Infrastructure and Persistence may depend on each other** — the mermaid diagram shows
both pointing at Application but never at each other, yet the Rule text never forbids it.

**Divergence scenario:** Team A builds `BoardExportJobModule` (Infrastructure) and, needing a quick paged scan
over export requests, references `Persistence`'s `TaskTrackerDbContext`/repositories directly instead of routing
through an Application query — justified as "just a read, not worth a CQRS round-trip," and indeed
`BoardExportSchedulerJob` already does in-process DB scanning + status mutation + SignalR notification, not a
trivial enqueue (see AD-8 below). Team B, building a different Infrastructure module, keeps strict separation
and never touches Persistence directly. Both satisfy AD-1's literal text (neither violates the stated arrows).
The result: business logic (paging, status transition rules) now lives partly in Infrastructure for one feature
and entirely in Application for another, with no single AD forbidding the Infra→Persistence shortcut. **Fix:**
AD-1 should state explicitly whether Infrastructure↔Persistence may depend on each other, not just leave it
implied by an incomplete diagram.

---

## AD-2 — CQRS shape and pipeline order

**Rule as written** fixes pipeline order (`ValidationBehavior` → `LoggingBehavior` →
`WorkspaceFeatureGuardBehavior`) but says nothing about where the AD-7 membership check sits relative to that
pipeline — and that pipeline behavior turns out to be dead code (see AD-4 finding above), so in practice **every
gating check today runs inside the handler, after whatever membership check the handler does, in whatever order
each author picks.**

**Divergence scenario:** Command A is scoped directly by `WorkspaceId` (e.g. `UpdateWorkspaceCommand`) — its
author checks membership first, then (if it were gated) entitlement, cheaply, since both keys are already in
hand. Command B is scoped by a nested resource (`BoardId`, as in `ArchiveAndExportCommand`) — its author must
first resolve `board.WorkspaceId` from the DB before either check is even possible, and then calls
`entitlementService.HasFeatureAsync(board.WorkspaceId, ...)` before any membership/board-access check runs.
Nothing in AD-2 or AD-4 mandates "resolve membership before entitlement" — an author can legally write the
entitlement check first, meaning a non-member of a workspace could receive a `SubscriptionFeatureRequiredException`
(403, "Upgrade Required") for a workspace whose plan they have no business knowing about, before ever being told
they aren't a member. Two features picking opposite check orders both "follow" AD-2/AD-4 to the letter while
leaking different information to unauthorized callers. **Fix:** AD-2 (or AD-7) should pin check ORDER
— membership/board-access must run and fail closed before any entitlement check — not just pin the MediatR
behavior order, which is moot since the behavior is unused.

---

## AD-3 — Shared/Contracts is the sole wire-format authority

Grounding check found no actual shape drift today — `BoardActionNotification`/payloads under
`Shared/Contracts/Notifications/BoardActions/Payloads/*` are consumed as-is by
`BoardActionsHubService`/`BoardDetailsStore`/`TaskDetailsStore`. AD-3 is holding in practice. The remaining hole
is structural, not yet observed:

**Divergence scenario:** Contract DTOs are `record` types with **primary constructors** (per the Consistency
Conventions row), which in C# means positional parameter order is part of the wire contract for any caller using
positional (not `with`/named) construction. Team A extends `BoardExportRequestDto` for the scheduled-export path
by adding a new optional field at the end of the primary constructor. Team B, working in parallel on the
on-demand/manual export feature, also extends the same record, inserting their new field in the *middle* for
readability (grouping related fields together) — both changes individually compile, both are "defined once in
Shared/Contracts and referenced by both sides" per AD-3's letter. Merged together, any positional-construction
call site (easy to write by accident with primary-constructor records, e.g. `new BoardExportRequestDto(id, name,
true)`) now silently binds the wrong field to the wrong parameter with no compiler error, because the types
still line up. AD-3 says to "check both sides before merging" but never says construction must be by named
arguments only, or that contract records need reserved trailing-only extension. **Fix:** add a rule that
Contracts records are only ever constructed with named arguments (or object initializers), or that new fields
are strictly append-only.

---

## AD-4 — Entitlement gating is enforced symmetrically

**This AD is currently describing code that doesn't exist as a live mechanism.** `WorkspaceFeatureGuardBehavior`
and `IRequireWorkspaceFeature` exist and compile, but grep across the Application layer shows **zero commands
implement `IRequireWorkspaceFeature`**. The three real gated operations
(`ArchiveAndExportCommand.cs`, `ReExportArchivedBoardCommand.cs`, `GetBoardArchiveDownloadQuery.cs`) each call
`entitlementService.HasFeatureAsync(board.WorkspaceId, FeatureConstants.X, ct)` and throw
`SubscriptionFeatureRequiredException` **by hand, inline in the handler** — exactly what AD-4's Rule text says
is prevented ("never hand-checked in the handler").

**Divergence scenario (already real, not hypothetical):** three separate authors each wrote their own copy of
"fetch the resource, get its WorkspaceId, call HasFeatureAsync with a FeatureConstants string, throw
SubscriptionFeatureRequiredException." Nothing enforces these three copies stay in sync. A fourth feature added
later could easily: (a) forget the check entirely — nothing fails at compile time or via a pipeline behavior,
since there is no pipeline behavior actually wired to catch it; (b) check entitlement against the wrong
workspace (e.g. a command that takes both a `WorkspaceId` field on the DTO *and* a nested `BoardId` — if these
ever disagree, e.g. a client-supplied `WorkspaceId` that doesn't match the board's actual workspace, whichever
one the handler happens to check is the one enforced); or (c) use a different `FeatureConstants` string casing/
naming than `PlanOptions.Features[]` expects, silently always-allowing or always-denying since
`FeatureConstants.IsValid` validation is a separate, easy-to-forget step. **Fix:** either wire
`WorkspaceFeatureGuardBehavior` in for real (retrofit the three existing commands to implement
`IRequireWorkspaceFeature`) or delete AD-4's claim about the pipeline and document the actual pattern (manual
per-handler check helper) — as written, the spine is asserting a guarantee ("never hand-checked") that the
current codebase already violates three times over.

---

## AD-5 — Database schema authority is DbUp, not EF migrations

**Rule as written** requires "a `Persistence/Configurations/*` update AND a new numbered
`Database/Scripts/000N_*.sql`" but specifies no coordination mechanism for the number `N` itself.

**Divergence scenario:** Team A branches off main and adds `0007_add_workspace_invites.sql` +
matching `IEntityTypeConfiguration`. Team B branches off the same main commit and adds
`0007_add_task_labels.sql` + its own configuration. Both individually satisfy AD-5's Rule to the letter — each
added exactly one EF config and one new numbered script. On merge: either a literal filename collision (one
branch must rename, discovered only at merge time, not by any rule), or — worse — both get merged with
DbUp's filesystem/lexicographic ordering silently deciding which "0007" wins first in an environment that
already ran one of them, while a fresh environment runs them in a different relative order than a partially
migrated one. Nothing in AD-5 mandates a reservation ledger, a CI check for duplicate/gapped numbers, or that
script numbers be assigned at merge time rather than branch time. **Fix:** add a numbering-collision rule (a
CI check on script filenames, or "renumber before merge, never in parallel" convention).

---

## AD-6 — Error contract chain

Grounding confirms the "legacy-only" framing holds **within Application** — `UnauthorizedAccessException` isn't
thrown there. But AD-6's Rule text doesn't scope itself to HTTP/Presentation at all, and the codebase already
has a case where the "single place" promise doesn't hold: `Infrastructure/Boards/Hubs/BoardExportStatusHub.cs:47`
catches `UnauthorizedAccessException` and rethrows it as a `HubException` — a **SignalR hub method**, which
never passes through `GlobalExceptionHandler`'s `IExceptionHandler` middleware at all (that middleware only
wraps the ASP.NET Core HTTP pipeline, not Hub invocations).

**Divergence scenario:** Feature A is a REST command that throws `WorkspaceLimitExceededException` (an
`AppException` subtype) — correctly caught by `GlobalExceptionHandler`, correctly rendered as `ProblemDetails`,
correctly unwrapped by the frontend's `ApiResponseExtensions.HandleResponseAsync()`. Feature B is a SignalR hub
method that also throws an `AppException` subtype for a business-rule violation — by AD-6's letter this is
compliant ("business-rule violations throw an AppException subtype"). But it never reaches
`GlobalExceptionHandler`; the client gets a raw `HubException`/connection fault instead of `ProblemDetails`, and
the frontend code path built entirely around `ProblemDetails` shapes (`Errors`→`Detail`→`Title`) has nothing to
parse. Both features "did what AD-6 says," yet only one actually gets the single-error-contract guarantee the
AD promises. **Fix:** AD-6 should either explicitly scope itself to HTTP-invoked handlers, or define the
equivalent single-catch contract for Hub-invoked handlers (a Hub filter mapping `AppException` → a structured
Hub error payload), since real code already needs it.

---

## AD-7 — Workspace-membership authorization is explicit, not automatic

This is where the grounding turned up the sharpest confirmed divergence. `IWorkspaceAccessService` is
role-agnostic at the membership layer (`EnsureIsMemberAsync` takes no role) with separate named methods for
privileged actions (`EnsureCanManageWorkspaceAsync`, `EnsureCanDeleteWorkspaceAsync`, etc. — no generic
`EnsureHasRoleAsync`). AD-7's Rule says a handler must call `EnsureIsMemberAsync` "**or the equivalent role
check**" — which admits two contradictory readings:

1. *Safe reading:* always call at least `EnsureIsMemberAsync`; escalate to a named `EnsureCanX` method when the
   action needs more than membership.
2. *Unsafe reading:* "or" is exclusive — calling *any one* access-service method, including the loosest
   membership-only check, discharges AD-7 even for a privileged action, because the Rule never says "and use
   the strictest check the action requires."

**Confirmed real divergence, not hypothetical:** `CreateTaskCommand.cs` and `CreateColumnCommand.cs` — both
operating on resources that are transitively workspace-scoped — call `IBoardAccessService.EnsureCanManageTasksAsync`
/ `EnsureCanManageColumnsAsync` (a **board**-level gate keyed on `BoardId`) and **never call
`IWorkspaceAccessService`/`EnsureIsMemberAsync` at all**. Meanwhile `GetWorkspaceByIdQuery`,
`UpdateWorkspaceCommand`, `ChangeWorkspaceRoleCommand`, `RemoveWorkspaceMemberCommand`, `CreateBoardCommand`,
and `AddBoardMemberCommand` go through `IWorkspaceAccessService` directly. These are **two independent,
separately-implemented authorization gates** (`IWorkspaceAccessService` vs `IBoardAccessService`) for the same
underlying workspace-membership question, each satisfying AD-7's literal text via its own reading of "or the
equivalent role check." Nothing keeps them in sync: if a user's workspace role changes but a board-level
member/role cache or a board-membership row lags behind (e.g. board membership added before a workspace
role downgrade propagates), a task/column command could allow an action the workspace-level gate would have
blocked, or vice versa. Since AD-7 explicitly disclaims any structural enforcement ("a reviewer must check for
the call"), a reviewer checking `CreateTaskCommand` for "the call" would correctly see *a* call
(`EnsureCanManageTasksAsync`) and approve it — never noticing it's a different authorization system than the
one AD-7's own example (`GetWorkspaceByIdQueryHandler`) uses. **Fix:** AD-7 needs to either name
`IBoardAccessService` as an explicitly sanctioned equivalent (with a stated invariant that it and
`IWorkspaceAccessService` are kept consistent), or mandate that workspace-scoped resources funnel through one
canonical entry point regardless of which nested ID they carry.

---

## AD-8 — Async work split: Hangfire triggers, Functions processes

**Rule as written** binds "any new scheduled/recurring or heavy async workload" to the Hangfire-triggers/
Functions-processes split, and says Hangfire owns "recurring/scheduled triggers only." Two holes, one confirmed:

**Confirmed hole — "triggers only" is already not true.** `BoardExportSchedulerJob.RunAsync` doesn't just
enqueue: it runs a **paged DB scan** (`ScanForRequestedExportStatusesAsync`), mutates status to Pending per item,
enqueues to Service Bus, *and* sends a SignalR notification — all in-process, inside Hangfire, before Functions
ever gets involved. This is real processing, not a trigger. AD-8 gives no threshold for "heavy," so this already
sits in a gray zone the AD claims doesn't exist.

**Divergence scenario building on that:** Team A, adding a new recurring job (e.g. "stale workspace-invite
cleanup" that loops over up to thousands of rows and calls an email API per row — network I/O, rate-limited,
can run minutes), points to `BoardExportSchedulerJob` as established precedent for "recurring jobs can do
real per-row work in-process" and builds it entirely inside Hangfire, relying on Hangfire's own retry semantics
(no idempotency: Functions grounding shows **no explicit idempotency/dedup for duplicate Service Bus deliveries
either** — a retried Hangfire job restarts the whole loop from scratch, e.g. re-sending emails already sent).
Team B, building a similarly-shaped "digest email" recurring job, judges *their* workload "heavy" and dispatches
it through Service Bus to a new Functions handler, mirroring the export pipeline. Both cite AD-8 in support of
opposite architectures for equivalent-weight problems, because "heavy" has no defined threshold and the
existing precedent (`BoardExportSchedulerJob`) already contradicts the Rule's "triggers only" framing.

**Second, sharper hole — AD-8's Binds clause has a scope gap.** AD-8 binds "scheduled/recurring **or** heavy
async" work. A one-off, non-recurring, not-obviously-heavy task (e.g. "recompute this one board's thumbnail
right now" triggered synchronously by a user action) is *neither* recurring nor clearly heavy — it falls outside
AD-8's stated scope entirely. Team A could reasonably add a raw Service Bus enqueue directly from a command
handler (bypassing Hangfire, since it's not recurring). Team B, for an equivalent one-off async need, could spin
up an `IHostedService`/`BackgroundService` inside the API process instead (also not recurring, also arguably not
"heavy"). Both are outside AD-8's binding, both compliant, and now there are three coexisting async mechanisms
(Hangfire→Functions, ad hoc direct Service Bus, in-process `IHostedService`) for a system whose stated goal was
"no ad hoc new async mechanisms per feature." **Fix:** define "heavy" with a concrete proxy (e.g. "touches
external I/O beyond a single row" or "expected duration > N seconds"), retrofit `BoardExportSchedulerJob`'s scan
loop to match the stated split or explicitly carve out an exception for it, add idempotency/dedup as a stated
requirement for both Hangfire retries and Functions message processing, and close the Binds-clause gap by
covering *all* new async work, not just recurring-or-heavy.

---

## AD-9 — Frontend call chain is fixed

**Rule as written** only names the Refit→ApiService→Store→component chain; it says nothing about where SignalR
Hub services fit, even though the broader doc mentions "SignalR hub services (`Services/Hubs`, mirroring the
backend's hubs)" as a first-class part of Frontend architecture.

**Divergence scenario:** Feature A's Hub service (mirroring `BoardActionsHubService`) pushes incoming
notifications by calling directly into the relevant Store's internal update methods, so real-time updates and
API-driven updates converge on one state owner — consistent with AD-9's spirit even though AD-9's text doesn't
actually say Hub services must go through Store. Feature B, built by someone reading AD-9 literally ("Components
never skip a layer" — read as "never skip the *API* layer," which a Hub push isn't), has its component inject
the Hub service directly and hold its own local state for hub-pushed events, since AD-9's Rule only restricts
"components bypassing the Store to call a Refit interface or `*ApiService` directly" — a Hub service is neither.
Both are literally compliant. Result: two different features now have two different sources of truth for
"current board state" — one flowing through `BoardStore`, one living in local component state fed straight
from the hub — so a component using Feature B's pattern can show stale or divergent data relative to one using
Feature A's pattern after the same broadcast event. **Fix:** AD-9 should explicitly state Hub services publish
into the same Store, never directly into components.

---

## Consistency Conventions table

**Sensitive-field redaction ("known gap" already flagged in the doc itself):** the redaction list
(`Password`, `Token`, `RefreshToken`, `ClientSecret`, `AccessToken`) is exact-name matching. Team A adds a DTO
field named `ApiSecret` or `SigningKey` for a new integration; Team B adds a field named `AccessToken`. Both
"follow the convention" (redaction is a platform-level policy, not something either feature owner is on the hook
to extend) yet one leaks a secret into Serilog output and one doesn't, and nothing in the convention obligates
either author to touch the redaction policy when introducing a new secret-shaped field. **Fix:** redact by
suffix/substring heuristic (`*Secret`, `*Key`, `*Token`) or require new secret-bearing fields to be reviewed
against the redaction list explicitly, since exact-name matching guarantees this gap recurs per new field name.

**Internal-key auth for Functions→API (`/api/internal`, static `X-Internal-Api-Key` header):** the convention
states the mechanism but not its topology assumptions — see AD-8/Deferred overlap below; a static shared header
is only as safe as the network it travels over, which is exactly what's deferred.

---

## Deferred section — should either item actually be deferred?

### WebRTC calls — should NOT be fully deferred; it's live risk on the current branch

The repo's current branch is **`feature/webrtc-calls`**, meaning work in this exact space may already be
starting without spine guidance on the one question the spine itself calls "a real fork this spine should
otherwise pin down." Concretely:

**Divergence scenario:** One developer/agent assumes calls are ephemeral — pure SignalR broadcast, session
state held in an in-memory singleton (`Dictionary<CallId, Participants>`) inside a hub-adjacent service, no
Postgres entity, no AD-5 schema work, no AD-7 hook point (there's no workspace-scoped *resource* to authorize
against — it's a live broadcast, not a CQRS command against a stored aggregate). A second developer/agent,
reading "session-state ownership" as implying a real resource, adds a `CallSession` entity with an
`IEntityTypeConfiguration` and a new DbUp script (following AD-5 to the letter), wires a `WorkspaceId`-scoped
CQRS command for joining/leaving a call (following AD-1/AD-2/AD-7 to the letter, including an
`IWorkspaceAccessService.EnsureIsMemberAsync` check), and expects call history to be queryable. If frontend work
and backend work split along these two assumptions — e.g. a frontend engineer builds a `CallStore` expecting
`ICallsApi`/Refit/persisted call history per AD-9's chain, while the backend implements pure ephemeral hub
broadcast with no persisted `CallId` ever exposed via REST — the two halves are individually spine-compliant
and mutually non-integrable. Given the branch name, this isn't a someday risk; it's the most likely next thing
built. **Recommendation:** pin the ephemeral-vs-persisted decision now, before any more code lands on this
branch, rather than truly deferring it.

### Production deployment topology — deferring the topology is fine; deferring its security implication is not

Deferring "which cloud, which IaC, which pipeline" is reasonable — no evidence forces that decision today.
But one consequence of deferring it is dangerous enough to pin now: AD-8's `/api/internal` boundary is
guarded *only* by a static `X-Internal-Api-Key` header (confirmed: `InternalApiKeyMiddleware.cs`), with no
mention of network-level restriction. **Divergence scenario:** whoever sets up a staging environment first
puts API and Functions in the same private VNet/network and treats the static key as defense-in-depth on top
of network isolation (safe). Whoever sets up a second environment (a demo environment, a different cloud
region, a quick public-facing staging slot) — with no spine rule constraining this — puts the API behind a
public ingress without ensuring `/api/internal` is unreachable from outside the private network, because
nothing says it must be. Both are consistent with "no topology is defined yet." One is a static-header-guarded
endpoint reachable only internally (acceptable); one is a static-header-guarded endpoint reachable from the
public internet (a real vulnerability — a leaked or brute-forced header is the only thing standing between the
internet and internal export-status callbacks). **Recommendation:** even while deferring the full topology, pin
one constraint now: `/api/internal` must never be reachable outside the deployment's private network, regardless
of which topology is eventually chosen.
