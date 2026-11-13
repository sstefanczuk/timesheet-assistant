<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Record Persistent Workday

- **Plan**: context/changes/record-persistent-workday/plan.md
- **Scope**: Phase 2 of 4
- **Reviewed phases**: 2
- **Date**: 2026-11-12
- **Revision**: 0459e73; the pre-existing working-tree plan change only appends that SHA to completed phase 2 Progress rows.
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 2 warnings, 0 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Failed first initialization leaves a database that Retry rejects

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: src/TimesheetAssistant/Features/Workdays/StorageInitialization.cs:57
- **Detail**: The initializer exclusively creates the final database file before opening SQLite and committing the schema. If opening the provider or creating/committing the schema fails, cleanup at lines 78–82 disposes resources but leaves this attempt's empty or rolled-back database. Retry and subsequent launches see an existing file (`Path.Exists`, line 47), call `Initialize(isNew: false)` and reject its `user_version = 0` in SqliteWorkEntryStore.cs:40. Removing the original transient cause does not recover the store, despite the Retry prompt. Existing unversioned user files must remain protected; treating every version-0 file as a new database would violate the plan. This is a source-level failure-path finding, not a reproduced desktop scenario.
- **Fix**: Create and initialize a uniquely named temporary database while holding the directory lease; close all SQLite handles and publish it to the final path with a same-directory move that refuses to overwrite. Clean up only this attempt's temporary files on failure. Leave every pre-existing final database untouched.
  - Strength: Retry can start a fresh initialization after a transient failure; existing and unsupported databases keep their current protection.
  - Tradeoff: Adds a small initialization/publish lifecycle and cleanup for attempt-owned temporary files; interruption before publication may leave an unused temporary file.
  - Confidence: HIGH — file creation, failure cleanup and version rejection are explicit in the reviewed code.
  - Blind spot: The real desktop failure/recovery scenario has not run; validate it with isolated storage in phase 4.
- **Decision**: FIXED — 2026-11-13, user selected the proposed temporary-database approach. Initialization now creates a unique attempt-owned file under the directory lease, commits its schema, closes provider handles and moves it to the final path with overwrite disabled. Failure cleanup targets only the temporary database and its rollback journal before releasing the lease. Existing final databases are still inspected and never reset. Desktop failure/retry acceptance remains pending in phase 4; the original review verdict above is retained as historical evidence.

### F2 — Current desktop harness can open the normal user store

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: src/TimesheetAssistant.UiTests/DesktopAppFixture.cs:136
- **Detail**: Phase 2 now initializes storage before rendering an ordinary route. The existing fixture overrides only WEBVIEW2_USER_DATA_FOLDER and WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS before launching its child at line 139. It never supplies TIMESHEET_ASSISTANT_DATA_DIRECTORY and inherits a caller value if present. With no caller override, the published phase 2 app resolves the normal LocalApplicationData directory, may create its database and holds its lease while the smoke runs. This is a new interaction with an unchanged fixture, not a demand to implement phase 4's restart suite early. The phase 2 composition intent explicitly includes preventing test access to the real store, and the plan says UiTests always supplies the child override. No desktop test was run during this review.
- **Fix**: Before any child launch, explicitly override TIMESHEET_ASSISTANT_DATA_DIRECTORY with a unique absolute test-owned domain path, separate from the WebView2 profile, and retain owned-process cleanup before deleting that path. Keep phase 4's multi-launch scenario lifetime work deferred.
- **Decision**: FIXED — 2026-11-13, user approved explicit child-only test storage isolation. The fixture now overrides TIMESHEET_ASSISTANT_DATA_DIRECTORY with its unique absolute session/data path, separately from the profile, logs that identity without record contents and checks the GUID session root before cleanup after owned-process exit. Phase 4 still owns the independent multi-launch scenario lifetime.

## Scope and Evidence

The primary reviewer read the full plan and all phase 2 production sources,
composition/lifecycle/routing changes, project manifest and verification text,
plus Core store/service contracts and the existing desktop fixture. The review
covers all 12 files in 0459e73 and their interactions with those unchanged callers.
No lessons.md exists.

Two focused review agents were started in parallel as required by the review
workflow. They lacked filesystem tools and did not receive usable source history.
The primary reviewer supplied focused code excerpts directly. Their conclusions
are limited to those excerpts; they do not represent independent full-repository
inspection. The primary reviewer verified the final findings against actual files
and resolved the line numbers. One agent treated the fixture issue as planned
phase 4 work; the primary reviewer retained F2 because the existing runnable harness
already selects the normal store after this commit, while full restart testing
can still remain deferred.

The adapter otherwise follows the intended Core boundary: parameterized values,
date-filtered ordered reads, stable GUID identities, AUTOINCREMENT creation order,
transactional schema/version creation and mutations, identity/date predicates and
expected update/delete cardinality. SQL values are not concatenated. Unsupported
versions are inspected read-only and never reset. Connections are operation-owned,
pooling is disabled, DefaultTimeout is five seconds, and provider work runs on a
worker thread. The canonical directory lease precedes DB access; success retains
the lease until disposal, failures release local resources. Unavailable routes
render English text/Retry before constructing their content. Windows-only targets,
feature organization, English source content and the pure-test boundary remain.

StorageGate and routing/lifecycle changes implement the phase 2 unavailable-screen
contract, not an unplanned phase 3 editor. verification.md records the audit required
in phase 2, although the full delivery record belongs to phase 4. No CI, release,
new feature workflow, real-storage UnitTests or new test project was introduced.

## Automated Verification

Commands rerun from the repository root on Windows with SDK 10.0.403 and the
installed maui-windows workload. Authorized execution used access to installed
SDK/NuGet configuration/cache and the NuGet audit feed.

| Command | Result | Observed output |
|---------|--------|-----------------|
| `dotnet build TimesheetAssistant.sln` | PASS, exit 0 on isolated rerun | Build succeeded; 0 warnings, 0 errors |
| `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` | PASS, exit 0 | 87 passed, 0 failed, 0 skipped |
| `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` | PASS, exit 0 | No vulnerable packages reported for application, Core, UnitTests and UiTests |
| `git diff --check` | PASS, exit 0 | No whitespace errors |

The first review build ran concurrently with the test command, which also builds
Core, and failed with CS2012 because both wrote the same generated Core DLL.
After both processes exited, the build was rerun alone and passed. This is a review
command scheduling error, not an asserted product defect. No code or assertion was
changed to make a gate pass.

## Manual Verification and Limits

Phase 2 has no manual criterion. Its Progress rows 2.1 and 2.2 are complete;
phases 3–4 remain pending and their manual rows remain unchecked. No actual
application launch or database mutation was performed for this review. SQLite
durability, packaged native loading, process exclusion and the failure/recovery
scenarios still require the planned isolated real desktop evidence. Passing these
automated criteria does not resolve the two functional warnings or establish S-01,
desktop CI, clean-device or MVP acceptance. Product code remains unchanged; triage
decisions are pending.

## Triage Outcome — 2026-11-13

Fixed: F1, F2. No pending decisions, skipped findings or accepted risks.
Changes are uncommitted. The original review verdict and evidence above describe
0459e73 before these fixes; they are retained as review history.

Verification after both fixes: Release solution build passed with 0 warnings and
0 errors; Release pure tests passed 87/87. A fresh working-tree publication at
`artifacts/publish-0.1.0-preview.111301/TimesheetAssistant.exe` passed the existing
real desktop smoke (TRX executed/passed 1, failures 0) while the caller supplied
an intentionally invalid data override. The child's unique data directory was
logged and removed after owned-process exit; cleanup completed. Full commands,
TRX and log identity are in verification.md. The initial failure/Retry path and
product persistence scenarios still require phase 4 evidence.
