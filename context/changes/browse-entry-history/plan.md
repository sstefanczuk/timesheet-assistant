# Browse Entry History Implementation Plan

## Overview

Deliver S-07: show today separately and the three most recent earlier dates containing entries on application startup. Let the user load one additional older nonempty day at a time, inspect full saved entries, edit a historical entry directly in the existing form, and add new entries from scratch. History has no retention limit.

Complexity: MEDIUM. The user approved four design questions and the three phases below on 2026-11-15. History and the editor share one screen; compact form redesign is deferred.

## Current State Analysis

S-01 provides durable workday CRUD, date selection, totals, validation and discard/delete confirmation. The root page currently opens today's editor. There is no multi-day read contract or history view. SQLite already indexes work date and immutable creation order; no schema migration is needed for this feature.

The portable Core service owns a single selected-day snapshot. History needs independent state so loading older days cannot replace the editor's date or draft. Existing UI tests assume a single table and globally locate rows; adding history requires scoping those locators without weakening existing assertions.

S-01 is locally accepted and archived. F-01 desktop CI remains deferred and does not block this slice. No lessons.md, existing S-07 research/frame, or S-07 plan exists at planning time. Archived evidence is reference material only.

## Desired End State

- The root screen contains a separate Today section, the existing selected-day editor and saved-entry list, and an Earlier days section. Today remains visible when another date is selected for editing.
- Today shows its saved entries and total, or an explicit empty state, and offers Add entry for today. Avoid rendering the same today's rows twice when the selected-day list already supplies them; use one shared presentation where appropriate.
- Earlier days initially shows up to three distinct dates strictly before the local current date, newest first, skipping empty dates and including weekends. Every group shows date, Jira ID, title, work description, duration and daily total. Entries retain creation order, identities and separate repeated tickets.
- Load older day appends exactly one older nonempty date. Exhaustion is explicit and disables or removes that action. No history cutoff, age-based cleanup or transfer-status filter is introduced.
- Each history entry has Edit, which selects its date and populates the existing editor with that exact saved identity. Add entry for today selects today with a blank form; the existing date picker continues to allow adding to an earlier date. These context changes honor discard confirmation.
- Successful mutations refresh the visible history and Today section. The oldest loaded date boundary survives editing: refresh the entire retained interval rather than resetting to three days. Remove groups that become empty, include newly populated dates within the interval, and retain at least the initial three nonempty days when available. Loading further history continues strictly before the retained boundary, even if its last group was deleted.
- Restart returns to the initial three-day window; retaining scroll position, drafts or pagination across process restarts is not required. Explicit refresh/re-entry evaluates local today again. An operation uses one captured date boundary; a detected date rollover rebuilds the initial history window without discarding a draft. No background midnight timer is required.
- Failed initial reads have an error and Retry, distinct from an empty history. Failed paging or refresh retains the last successful groups and cursor, labels the view as not refreshed, and permits retry. A committed save followed by a failed history refresh remains a successful save and must not be repeated.

### Key Discoveries:

- `src/TimesheetAssistant.Core/Features/Workdays/IWorkEntryStore.cs:6`: only single-date reads exist.
- `src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs:76`: `(work_date, creation_order)` index supports date selection; existing schema is version 1.
- `src/TimesheetAssistant.Core/Features/Workdays/WorkdayService.cs:46`: selected-day state belongs to the editor service.
- `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor:159`: successful loads reset the editor; `:263` guards dirty context changes and `:412` preserves input on failure.
- `src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayScreen.cs:10`: global table-row locator will become ambiguous; `:55` already waits for acknowledged date loading.
- `src/TimesheetAssistant.UiTests/DesktopScenario.cs:13`: scenario-owned data survives launches; `:26` supports copying the full application directory.
- `context/archive/2026-11-11-record-persistent-workday/verification.md`: accepted restart/folder-independence evidence exists, but the same-binary folder copy alone is not old-version-to-new-version upgrade evidence.

## What We're NOT Doing

- S-08 copying or selecting entries for copying; summaries, outstanding-day rules, transfer confirmations, holiday/leave types and templates.
- Compact form redesign, a general navigation redesign, persistent draft recovery or exact scroll restoration.
- Retention cleanup, cloud services, authentication, new operating-system targets, new dependencies or an IntegrationTests project.
- Desktop CI changes/runs, F-01 diagnosis, hosted releases, clean-device acceptance or edits to archived changes.

## Implementation Approach

Extend the existing Workdays feature across Core, application and both test projects. Use bounded distinct-date SQLite reads with an exclusive date cursor, not calendar-day iteration or loading every historical entry. Keep the schema and existing data directory unchanged. Add a separate scoped history service with immutable snapshots and the existing injectable local-date pattern. The root page composes history with the current editor and funnels entry selection through one guarded context transition.

The store returns complete day groups with a HasOlder flag computed using a bounded extra date, never a partial day. Initial requests select three dates; subsequent requests select one. Refresh uses the retained oldest-date boundary, so deleting the last entry of that day cannot reset or skip the remaining history. Group ordering is date descending, entry ordering creation ascending.

## Critical Implementation Details

### State sequencing

Cross-day Edit must confirm draft discard once, load the destination successfully, resolve the selected identity in that loaded day, and only then populate the form. A failed destination read retains the prior editor; a missing identity reports that it is unavailable without opening another entry. History refresh is a separate read after mutation commit and cannot turn a committed mutation into a retryable save failure.

### Test observability

Existing desktop helpers must wait for the rendered selected date and idle state, not browser input echo. Scope history/editor statuses, totals and rows to their named regions; history completion must expose an equally observable loading state. Do not add fixed sleeps or action retries to hide races.

## Phase 1: History Reads and Core State

### Overview

Provide bounded history reads and pure, independently testable history state while leaving current CRUD behavior intact.

### Changes Required:

#### 1. History contracts and SQLite reads

**Files**: `src/TimesheetAssistant.Core/Features/Workdays/IWorkEntryStore.cs`, new `WorkEntryHistoryPage.cs` in the same Core folder, `src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs`.

**Intent**: Discover actual nonempty dates efficiently and return their complete saved contents without imposing a retention limit.

**Contract**: Add `ReadHistoryAsync(DateOnly beforeExclusive, int dayLimit, CancellationToken cancellationToken = default)` returning ordered day snapshots and HasOlder. Add `ReadHistoryRangeAsync(DateOnly fromInclusive, DateOnly beforeExclusive, CancellationToken cancellationToken = default)` returning the complete retained date interval plus whether older nonempty dates exist. Both return the history-page type, use parameterized reads and the existing operation/error boundary. Reject invalid limits/ranges consistently. Keep `ReadAsync`, schema version 1, database path and CRUD invariants unchanged. Date selection, complete entry materialization and HasOlder must represent one consistent read snapshot.

#### 2. Separate history state

**File**: new `src/TimesheetAssistant.Core/Features/Workdays/WorkEntryHistoryService.cs`.

**Intent**: Own initial loading, incremental paging and refresh independently of the selected workday.

**Contract**: Expose initial load, load-older and refresh operations with immutable successful snapshots, captured local today, oldest loaded boundary and exhaustion. Initial size is three dates and append size one. Serialize operations; publish groups/cursors only after successful reads, avoid duplicate dates and retain state on storage failure/cancellation. Refresh keeps the inclusive oldest boundary and fills back to at least three dates if deletions leave fewer; the boundary may extend older but never shrink forward during that session. Return explicit operation failure using the existing result conventions. Today is read separately from earlier history and remains excluded from it. A refresh never mutates `WorkdayService.Snapshot`.

#### 3. Pure history verification

**Files**: `src/TimesheetAssistant.UnitTests/Features/Workdays/FakeWorkEntryStore.cs`, new `WorkEntryHistoryServiceTests.cs` in the same folder.

**Intent**: Verify orchestration and observable state without real storage.

**Contract**: Extend the fake for both reads and controlled failure/delayed operations. Cover 0/1/2/3/more dates, sparse dates/weekends, today/future exclusion, one-date append/exhaustion, duplicate ticket preservation, storage failure/retry without cursor movement, retained-range refresh after edit/add/last-entry deletion, minimum-three refill, independent editor snapshot and local-day rollover. Use Shouldly and explicit Arrange/Act/Assert; query correctness against real SQLite remains a desktop responsibility.

### Success Criteria:

#### Automated Verification:

- Release solution build succeeds: `dotnet build TimesheetAssistant.sln -c Release`.
- Pure tests pass, including history state and existing workday regressions: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release`.
- `git diff --check` passes; schema version, Windows target and pure-test dependency boundaries are preserved.

No manual gate is required for this phase; database query behavior is accepted through phase 3 real desktop scenarios.

## Phase 2: History and Editor on One Screen

### Overview

Expose the complete browsing and editing workflow on the root page without redesigning the form.

### Changes Required:

#### 1. Root page and feature components

**Files**: `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`, `WorkdayPage.razor.css`, new feature-local `WorkEntryHistory.razor` and optional matching stylesheet.

**Intent**: Show Today, earlier day groups and the existing editor together; allow direct editing and creating entries from scratch.

**Contract**: Retain `/`, all current fields/date rules and editor behavior. Add named Today and Earlier days regions, full fields/totals, accessible Edit actions, Add entry for today, Load older day and refresh/retry actions. Label loading/error/empty/exhausted states distinctly. Use English date formatting independent of machine culture. History actions carry identity and date, never row index. Route context-changing actions through the existing discard guard once; cancellation preserves date, fields and loaded range. Disable overlapping transitions. After successful save/delete refresh history and Today without resetting an unrelated draft. Keep Delete in the selected-day list; do not add a second deletion workflow to history.

#### 2. Composition and navigation label

**Files**: `src/TimesheetAssistant/MauiProgram.cs`, `src/TimesheetAssistant/Components/Layout/NavMenu.razor` if its current label needs clarification.

**Intent**: Register history state and keep the shell aligned with the shared screen.

**Contract**: Add scoped history service using the existing store and local-date delegate. Keep one root screen, existing storage initialization/ownership and Windows-only targets. Avoid new navigation that bypasses dirty-form protection.

#### 3. Existing desktop helper compatibility

**File**: `src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayScreen.cs`, affected existing Workdays test selectors only where needed.

**Intent**: Preserve precise S-01 assertions with multiple visible day groups.

**Contract**: Scope rows, controls, status messages and totals to the selected-day editor region. ReadyAsync still acknowledges the actual root editor; no separate landing navigation is needed for the approved shared screen. Preserve date acknowledgement, focus waits, exact row assertions and failure checks.

### Success Criteria:

#### Automated Verification:

- Release solution build and pure tests pass after UI composition and helper changes.
- `git diff --check` passes and existing desktop tests compile with scoped editor selectors.

#### Manual Verification:

- Author confirms the shared screen shows Today separately, readable complete history and totals, direct cross-day Edit, Add entry for today, keyboard operation and cancelled draft discard without losing input or loaded history.

**Implementation Note**: Publish a fresh local verification build after unit tests if needed for this manual check. Record package provenance and the author's result; pause for the manual confirmation before proceeding to phase 3. Compact form redesign is not an acceptance condition.

## Phase 3: Desktop Verification and Persistence Evidence

### Overview

Verify history through the actual packaged application, preserve S-01 regression coverage and establish old-package-to-new-package data compatibility.

### Changes Required:

#### 1. History desktop scenarios

**Files**: new `src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryScreen.cs` and `WorkEntryHistoryTests.cs`; existing `WorkdayStorageTests.cs` only for reusable scenario extensions.

**Intent**: Prove real SQLite date selection, UI interactions and persistence rather than relying on fake-query behavior.

**Contract**: Use scenario-owned data, fresh launch profiles and ordinary entry creation through UI. Cover empty history with usable add action; fewer than three dates; at least five sparse dates including a weekend and a multi-year-old date; Today excluded from the initial three groups; exact fields/creation order and totals; one extra complete day per click; exhaustion; direct edit/save and confirmed last-entry deletion after loading older days; retained date boundary and correct next page; discard cancellation; restart showing committed changes and the default three-day window. Use app-displayed today for relative dates. Induce a bounded test-owned read failure after successful paging, verify previous groups/cursor and a visible retry, then release the failure and retry successfully. Scope failure setup to synthetic data and preserve existing process/cleanup diagnostics.

#### 2. Upgrade compatibility

**Files**: new `src/TimesheetAssistant.UiTests/Features/Workdays/WorkEntryHistoryUpgradeTests.cs`, `src/TimesheetAssistant.UiTests/DesktopScenario.cs` only if a small launch convenience is necessary.

**Intent**: Distinguish actual application-version compatibility from moving identical binaries.

**Contract**: Configure a test-runner-only absolute `TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP` pointing to the author-accepted S-01 package `artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe` (present at planning time). Launch it with the scenario's isolated data, create sparse history through its UI, close it fully, then launch the new package using the same data and verify exact history contents, identities/order and further paging. Use a small feature-local legacy helper for the prior package when needed: the new scoped editor selectors must not be assumed to exist in S-01. A missing configured prior EXE fails this dedicated compatibility test rather than silently skipping it. No ordinary user database is accessed. Reuse the existing copied-directory scenario for folder independence. Record versions/source provenance separately; copying the new package is not sufficient upgrade evidence.

#### 3. Documentation and delivery evidence

**Files**: `src/TimesheetAssistant.UnitTests/README.md`, `src/TimesheetAssistant.UiTests/README.md`, `scripts/Package-Windows.ps1` generated preview descriptions, new `context/changes/browse-entry-history/verification.md`.

**Intent**: Document new test configuration and the delivered history capability with honest acceptance limits.

**Contract**: Explain history suites, prior-package variable, sequential build/test/package commands and retained artifacts. Update preview descriptions for browsing history without claiming copying, summaries or full MVP completion. Verification records exact commands, exit codes, executed/passed/failed/skipped counts, package/source provenance, old/new version paths, diagnostics, cleanup and author acceptance. Record blocked checks separately. Do not change CI workflow or archived S-01 evidence.

### Success Criteria:

#### Automated Verification:

- Sequential Release build and pure UnitTests succeed before creating a fresh package with `scripts/Package-Windows.ps1`.
- Full UiTests against the fresh `artifacts/publish-<version>/TimesheetAssistant.exe`, with the identified prior package configured, exits zero and TRX reports executed tests with no failures/skips; history, upgrade and existing S-01 scenarios pass and owned process/data cleanup completes.
- Dependency audit and `git diff --check` pass; verification records package provenance and actual results without claiming desktop CI or clean-device acceptance.

#### Manual Verification:

- Author confirms browsing, loading older history, direct editing, adding from scratch and retained committed history after reopening the identified new package; the old-to-new isolated-data demonstration preserves entries.

**Implementation Note**: Keep local packages and TRX/logs in ignored artifacts. Record the author's confirmation before marking local acceptance complete. Do not publish a hosted release or resume F-01 as part of this phase.

## Testing Strategy

### Unit Tests:

Test Core state transitions, bounds, retained date interval, failures and clock changes with an in-memory fake. Do not test the fake as proof of SQLite ordering. Preserve all existing pure rules and dependencies.

### Real Desktop Tests:

Use the existing separate UiTests project and DesktopScenario lifetime. Build, run unit tests, package and run desktop tests sequentially; the S-01 evidence records a broken manifest from overlapping build/publication. Scope every locator to the appropriate day/region and wait on rendered acknowledgements. Failure injection must actually prevent a read: a normal SQLite reserved write transaction may still allow readers, so use a bounded exclusive lock or owned file-access failure and release it in finally. Do not weaken assertions if the intended failure was not induced.

Run from the repository root. Set `TIMESHEET_ASSISTANT_TEST_APP` and `TIMESHEET_ASSISTANT_PREVIOUS_TEST_APP` only around the runner, restore their previous values in finally, then run `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --logger trx`. Use the existing per-launch child isolation; no global CDP/profile settings. Run `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json` for the recorded dependency audit.

### Manual Testing Steps:

1. Open a fresh local verification package and confirm Today, history empty state and adding from scratch.
2. Create several nonconsecutive dates including a weekend and an old date; reopen and verify the initial three earlier dates and full details.
3. Load more than one older day, edit a historical entry, then delete a day's final entry; confirm the retained interval and continued paging.
4. Enter a draft and request a different historical Edit; cancel discard and verify the original input remains. Repeat with confirmed discard and verify the exact destination entry.
5. Observe the isolated old-package/new-package demonstration and confirm saved history remains available. Record author confirmation against the identified package.

## Performance Considerations

Initial reads fetch Today and three earlier complete days; each append fetches one day plus bounded lookahead for exhaustion. Query distinct indexed dates instead of walking empty calendar days. Refresh loads only the user-expanded interval, which can grow through explicit browsing. There is no invented numeric latency limit, permanent cap on history or new virtualization subsystem for the stated 2–5 entries/day scale.

## Migration Notes

No schema or data-path migration is intended. Existing version-1 stores remain readable; keep initialization checks, corruption/future-schema protection and directory ownership intact. Rolling back the executable leaves the same schema readable by S-01. Locate an existing S-01 verification package first; if unavailable, reproduce an identified S-01 revision in an isolated checkout/output without changing the current working tree. A missing baseline is a reported verification blocker until supplied/reproduced, never a waived upgrade check.

## References

- Scope: `context/foundation/roadmap.md` S-07 and S-08; `context/foundation/prd.md` FR-002, FR-006, FR-011 and non-functional retention requirements.
- Architecture and suite boundaries: `context/foundation/tech-stack.md`, `src/AGENTS.md`.
- Current domain/store/UI and test extension anchors are listed under Key Discoveries.
- Prior acceptance: `context/archive/2026-11-11-record-persistent-workday/verification.md` (read-only).
- Desktop configuration/diagnostics: `src/TimesheetAssistant.UiTests/README.md`.
- User decisions and phase approval: planning conversation, 2026-11-15; one shared screen, direct entry edit, full rows with sums, retained loaded interval, compact form redesign later.

## Progress

> Convention: pending checkboxes are execution state; append the closing commit SHA when a step lands.

### Phase 1: History Reads and Core State

#### Automated

- [x] 1.1 Release solution build succeeds: `dotnet build TimesheetAssistant.sln -c Release`.
- [x] 1.2 Pure tests pass, including history state and existing workday regressions: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release`.
- [x] 1.3 `git diff --check` passes; schema version, Windows target and pure-test dependency boundaries are preserved.

### Phase 2: History and Editor on One Screen

#### Automated

- [ ] 2.1 Release solution build and pure tests pass after UI composition and helper changes.
- [ ] 2.2 `git diff --check` passes and existing desktop tests compile with scoped editor selectors.

#### Manual

- [ ] 2.3 Author confirms the shared screen shows Today separately, readable complete history and totals, direct cross-day Edit, Add entry for today, keyboard operation and cancelled draft discard without losing input or loaded history.

### Phase 3: Desktop Verification and Persistence Evidence

#### Automated

- [ ] 3.1 Sequential Release build and pure UnitTests succeed before creating a fresh package with `scripts/Package-Windows.ps1`.
- [ ] 3.2 Full UiTests against the fresh `artifacts/publish-<version>/TimesheetAssistant.exe`, with the identified prior package configured, exits zero and TRX reports executed tests with no failures/skips; history, upgrade and existing S-01 scenarios pass and owned process/data cleanup completes.
- [ ] 3.3 Dependency audit and `git diff --check` pass; verification records package provenance and actual results without claiming desktop CI or clean-device acceptance.

#### Manual

- [ ] 3.4 Author confirms browsing, loading older history, direct editing, adding from scratch and retained committed history after reopening the identified new package; the old-to-new isolated-data demonstration preserves entries.
