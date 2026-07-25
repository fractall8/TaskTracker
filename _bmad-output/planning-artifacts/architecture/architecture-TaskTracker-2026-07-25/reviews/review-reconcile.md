# Reconciliation Review: project-context.md → ARCHITECTURE-SPINE.md

Date: 2026-07-25
Scope: does every load-bearing rule/constraint in `_bmad-output/project-context.md` land somewhere in the spine's
Invariants & Rules (AD-1..AD-9), Consistency Conventions table, or Deferred section? Typos/wording differences are
out of scope — only substantive drops, simplifications, or contradictions matter.

## Verdict: minor gaps (leaning toward real gaps on comment/test discipline)

Most structural/CQRS/error-handling/entitlement rules made it across faithfully, several nearly verbatim (Serilog
redaction gap note, internal API key gating, error contract chain, frontend call chain). The gaps that exist are
concentrated in **code-generation "tone" rules** — quiet constraints on what an agent must *not* produce — which the
spine's AD structure (organized around cross-layer wiring invariants) had no natural slot for and simply dropped.

## Section-by-section comparison

### Language-Specific Rules (C#) — partial drop
- Covered: file-scoped namespaces, `_camelCase`/`camelCase` build-error enforcement, DTOs-as-`record`-with-primary-constructors (Consistency Conventions, naming row).
- **Dropped:** "usings placed outside the namespace (build error otherwise)" — spine only says "file-scoped namespaces," not the using-placement rule.
- **Dropped:** "Braces required on all control flow statements" — no mention anywhere in the spine.
- **Dropped (quiet "don't"):** "no expression-bodied constructors/operators/local functions elsewhere" — the spine kept the positive half of this rule (prefer primary constructors/records for DTOs) but silently cut the negative constraint that governs everything *else* in the codebase.
- **Dropped:** "Nullable reference types + ImplicitUsings enabled everywhere — don't opt out per file."
- **Dropped:** the Husky caveat that pre-commit formatting won't fix naming/brace build errors (see Workflow section below — related to the same gap).

### Domain Exceptions & Error Handling — fully covered
AD-6 reproduces this rule set closely, including the legacy-exception fallback note and the frontend's untyped-`Exception` behavior. No gaps.

### Framework-Specific Rules — fully covered
AD-1 through AD-9 collectively cover: dependency direction, CQRS file shape + pipeline order, plan-gating via `IRequireWorkspaceFeature`, schema-change dual requirement (EF config + DbUp script), frontend call chain, `HandleResponseAsync` unwrapping behavior, SignalR hub mirroring, and the Shared/Contracts single-source-of-truth rule. No substantive loss here.

### Testing Rules — dropped entirely (real gap)
project-context.md states plainly: *"No test project exists in this solution — do not invent test commands, assume
a test framework (xUnit/NUnit/bUnit/etc.), or add test files unless explicitly asked to set one up first."*
This is exactly the kind of quiet "don't do X" rule the task description warns about, and it has **no equivalent
anywhere in the spine** — not in Invariants & Rules, not in Consistency Conventions, not in Deferred. Since the
spine is the "build substrate" that downstream code-generation work will be projected from, an agent relying only
on the spine has no signal against inventing a test project or test commands. This is the single clearest
substantive drop.

### Code Quality & Style Rules — two quiet rules dropped (real gap)
- **Dropped:** `GenerateDocumentationFile=true` but `CS1591` is suppressed — XML doc comments are *not* required,
  and agents should not add them just to silence an already-silenced warning. Nothing in the spine mentions this;
  an agent unaware of it could start adding XML doc comments across the codebase, directly against project intent.
- **Dropped:** "Match existing comment density: sparse, only for genuinely non-obvious behavior — no
  narrative/explanatory comments." This is a classic quiet tone rule LLM agents systematically violate
  (over-commenting) if not told otherwise, and it is not reflected anywhere in the spine.
- Covered: one-file-per-logical-unit / feature-foldered organization (Consistency Conventions naming row).

### Development Workflow Rules — dropped, likely acceptable scope cut
- Branch naming (`feature/<name>` → `main` via PR), conventional-commit prefixes, and the explicit "never bypass
  Husky with `--no-verify`" instruction are absent from the spine. The hybrid-vs-full Docker Compose dev modes
  *are* preserved (Structural Seed / environment envelope note), and `.env` prerequisite is dropped but is a setup
  triviality already stated in CLAUDE.md.
  This looks like an intentional/reasonable scope boundary (spine = architecture substrate, not day-to-day git
  workflow, and CLAUDE.md already carries this) rather than a true loss, but flagging since the task asked to check
  workflow-adjacent "don't" rules too — the `--no-verify` prohibition is arguably load-bearing enough to want a
  one-line callout, especially paired with the dropped "Husky won't fix naming/brace build errors" caveat above.

### Critical Don't-Miss Rules — fully covered
- Serilog redaction exact-match + known gap (`ApiKey`/`Secret`/`Pin` uncovered) — reproduced near-verbatim in
  Consistency Conventions.
- `InternalApiKeyMiddleware` / `/api/internal` prefix — reproduced in AD-8 and Consistency Conventions, though the
  spine states it descriptively ("internal calls go through /api/internal") rather than as the source's forward-looking
  imperative ("new internal/service-to-service endpoints *must* live under that prefix to get the check"). Minor
  softening of an otherwise-preserved rule, not a drop.

## Not a gap, but worth noting
AD-7 (workspace-membership authorization is explicit, not automatic — `IWorkspaceAccessService.EnsureIsMemberAsync`)
does not appear anywhere in project-context.md. This is net-new content in the spine, not a contradiction or a
drop — presumably sourced from a direct codebase scan beyond project-context.md, which is consistent with the
spine's `sources: ['project-context.md']` frontmatter still being a partial input. Flagging only for awareness, not
as a review finding.

## Summary of real gaps (ranked)
1. Testing-rules prohibition ("do not invent test commands / test files") — fully absent.
2. Comment-density rule ("no narrative/explanatory comments") — fully absent.
3. XML-doc-comment prohibition (CS1591 suppressed, don't add docs to silence it) — fully absent.
4. "No expression-bodied constructors/operators/local functions elsewhere" — half-dropped (positive half kept).
5. Braces-required-on-all-control-flow and using-directive-placement-is-a-build-error — fully absent.
