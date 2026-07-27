# Review: Stack Version Reality-Check

**Reviewed doc:** `../ARCHITECTURE-SPINE.md` — `## Stack` table
**Review date:** 2026-07-25
**Method:** Each pinned version was checked against NuGet.org / Docker Hub / vendor release notes and blog posts via live web search (not asserted from training-data memory). The versions themselves were already confirmed to match the repo's actual `.csproj` files, so internal consistency was not in question — this review checks whether each is a real, current, sensible choice for a .NET 10-era project as of July 2026.

## Verdict

**All 16 stack entries are real, existing packages/versions — nothing hallucinated, renamed, or deprecated.** Every pinned version corresponds to an actual published release, and all sit within 0–2 minor/patch releases of the true mid-2026 latest (normal "pinned a bit behind tip" drift, not staleness). Two items warrant a caveat note in the doc (Postgres major version, MediatR licensing), detailed below.

## Detailed findings

| # | Stack entry | Claimed version | Exists? | Latest verified (as of ~2026-07-25) | Verdict | Notes |
|---|---|---|---|---|---|---|
| 1 | .NET | 10 (`net10.0`) | Yes | GA'd 2025-11-11, LTS through Nov 2028 | **Plausible** | .NET 8/9 both reach EOL 2026-11-10, so targeting .NET 10 in July 2026 is the correct, expected choice, not ahead of reality. |
| 2 | ASP.NET Core Web API | 10 | Yes | Ships in-band with .NET 10 SDK | **Plausible** | No separate versioning concern. |
| 3 | Blazor WebAssembly | 10 | Yes | Ships in-band with .NET 10 SDK | **Plausible** | Same train as #1/#2. |
| 4 | MudBlazor | 9.5.0 | Yes | 9.7.0 (9.5.0 itself shipped 2026-05-26) | **Plausible, ~2 minor behind** | Real published version, not stale enough to flag. |
| 5 | EF Core | 10.0.8 | Yes | 10.0.10 | **Plausible, 2 patches behind** | Normal pin lag. |
| 6 | Npgsql (EFCore.PostgreSQL) | 10.0.2 | Yes | 10.0.3 | **Plausible, 1 patch behind** | Normal pin lag. |
| 7 | MediatR | 14.1.0 | Yes | 14.2.0 | **Plausible, with a real caveat** | MediatR underwent a genuine, well-documented licensing shift: Jimmy Bogard moved MediatR (and AutoMapper) to a commercial dual-license model under Lucky Penny Software, with v14.0.0 (Dec 2025) billed as the ".NET 10 support" release under the new terms. This is not a hallucination — it is accurate. **Recommend the architecture doc add a note flagging the commercial-licensing implication** (cost/compliance for larger orgs) and optionally mention the MIT-licensed source-generator alternative "Mediator" (martinothamar) that emerged in response, in case the team wants an escape hatch. |
| 8 | FluentValidation | 12.1.1 | Yes | 12.1.1 (exact match) | **Plausible — spot on** | Matches latest exactly. |
| 9 | Postgres (Docker) | 17-alpine | Yes | Postgres 18 is GA (18.4), Postgres 19 already in beta; 17's own latest patch is 17.10 | **Plausible but one major version behind** | 17-alpine is real, still maintained/supported, and a defensible "stable" pin, but by July 2026 it is not the current major version. **Flag only if the doc implies "latest"** — as a deliberate stability choice it's fine, just worth a one-line caveat. |
| 10 | Hangfire (core) | 1.8.23 | Yes | 1.8.24 (Hangfire.NetCore/AspNetCore meta-packages) | **Plausible, 1 patch behind** | Normal drift. |
| 11 | Hangfire.PostgreSql | 1.21.1 | Yes | 1.21.1 (exact match, published 2026-02-11) | **Plausible — spot on** | Its version numbering (1.21.x) sitting far above Hangfire core's (1.8.x) is expected — it's an independently-versioned community storage provider, not tied to core's scheme. Not a red flag. |
| 12 | Azure Functions Worker (isolated V4) | 2.52.0 | Yes | 2.52.0 (exact match) | **Plausible — spot on** | |
| 13 | Azure Service Bus SDK | 7.20.1 | Yes | 7.20.2 | **Plausible, 1 patch behind** | Normal drift. |
| 14 | Azure Cosmos DB SDK | 3.61.0 | Yes | 3.62.0 (3.63.0-preview also exists) | **Plausible, 1 minor behind** | Fits the SDK's normal cadence, not an implausible jump. |
| 15 | Azure Blob Storage SDK | 12.27.0–12.29.1 | Yes | 12.29.1 (exact match at range's upper bound) | **Plausible — spot on** | Range upper bound exactly matches current latest stable; internally consistent with real version history. |
| 16 | Microsoft.Identity.Web | 4.10.0 | Yes | 4.13.2 | **Plausible, ~3 minor behind** | Real version, fine to pin. |
| 17 | MSAL WebAssembly (Microsoft.Authentication.WebAssembly.Msal) | 10.0.8 | Yes | 10.0.10 | **Plausible** | Confirms this package's versioning tracks the target .NET major version (8.0.x/9.0.x/10.0.x ↔ .NET 8/9/10) — 10.0.8 is a real prior patch in that line, not a scheme mismatch. |
| 18 | Stripe.net | 52.1.1 | Yes | 52.1.1 (exact match; 52.2.0 only as prerelease) | **Plausible — spot on** | Currently the latest stable. |
| 19 | Serilog (core) | 4.3.0 | Yes | 4.4.0 (with an intermediate 4.3.1 bugfix release) | **Plausible but somewhat stale** | 4.3.0 is over a year old at review time and has been superseded by both a patch (4.3.1) and a minor (4.4.0). Not implausible, but the weakest pin in the table — worth bumping. |
| 20 | Serilog.AspNetCore | 10.0.0 | Yes | 10.0.0 (exact match, released 2025-11-28, no point releases yet) | **Plausible — spot on** | Confirms this package's versioning tracks the target ASP.NET Core major version (7.x/8.x/9.x/10.x ↔ .NET 7/8/9/10) — 10.0.0 is correct and current. |

## Summary of caveats to fold back into the architecture doc

1. **MediatR licensing (real, not fabricated):** v14 ships under Jimmy Bogard's new commercial dual-license terms (Lucky Penny Software). Worth a one-line note on cost/compliance exposure for a paid-subscription product like TaskTracker, and awareness of the MIT-licensed "Mediator" alternative if this becomes a blocker.
2. **Postgres 17-alpine is one major version behind (18 is GA, 19 in beta).** Fine as a deliberate stability pin; just don't describe it as "current."
3. **Serilog core (4.3.0) is the most dated single pin** in the table (superseded by 4.3.1 and 4.4.0) — low risk, but the one worth bumping first if the team does a dependency refresh pass.

No entries were found to be hallucinated, non-existent, renamed, or dangerously outdated.
