# Duplicate Work Entries Implementation Plan

## Overview

Let the user duplicate an individual work entry into an editable form, with a useful suggested work date. Saving creates a separate entry and leaves the original untouched.

## Current State Analysis

The home page displays today and earlier nonempty days in a single table, with edit and delete actions and an existing add/edit dialog (`src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`). History initially loads three earlier nonempty days and can append older days (`src/TimesheetAssistant.Core/Features/Workdays/WorkEntryHistoryService.cs`). The history window is not the complete interval needed to calculate a suggested date. The store already offers consistent range reads and assigns new identities and creation order to added entries (`src/TimesheetAssistant.Core/Features/Workdays/IWorkEntryStore.cs`, `src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs`).

## Desired End State

Each displayed entry has an accessible Duplicate action. It opens the existing form with copied Jira ID, title, description and duration, and an editable suggested date. Nothing is saved until the user submits; cancellation leaves all entries unchanged. Saving adds a new independent entry, even on the source date or when the target already contains the same Jira ID.

The suggested date is the earliest Monday–Friday strictly after the source date and no later than local today whose total is less than 432 minutes, including empty days. Totals of 432 minutes or more are skipped. If no candidate exists, including when the source is today, use today. Holidays and leave are not automatically recognized. The user may choose any date up to today, including the source date or a weekend, using existing validation.

### Key Discoveries:

- `WorkEntryHistoryService` pages nonempty dates; it cannot alone establish that a missing date has no entries.
- `IWorkEntryStore.ReadHistoryRangeAsync` can read the complete nonempty interval in a consistent snapshot; its bounds must be valid, so handle source-today and date boundaries explicitly.
- `WorkdayService.AddAsync` validates and adds a new identity; `WorkdayPage.razor` already refreshes history and reveals saved dates.
- `src/AGENTS.md` requires English UI, feature organization and the UI playbook; desktop acceptance uses the real published executable.

## What We're NOT Doing

- Bulk selection or copying multiple entries in one action; this supersedes the original S-08 interaction in the roadmap.
- Immediate saving on click, future dates, holiday calendars, leave types, transfer confirmations or summary generation.
- Changes to the existing database schema or archived changes.

## Implementation Approach

Keep date selection in portable workday logic, reading the required interval from storage rather than inferring completeness from the paged display. Reuse the add path and dialog with a separate draft, never the source identity or shared add draft. Preserve current validation, error handling, paging and focus behavior.

## Phase 1: Working duplication

### Overview

Deliver the complete visible workflow and meaningful unit coverage for the date rule.

### Changes Required:

#### 1. Suggested date and storage reads

**File**: `src/TimesheetAssistant.Core/Features/Workdays/WorkEntryHistoryService.cs` and related feature types; `src/TimesheetAssistant.UnitTests/Features/Workdays/`

**Intent**: Determine the suggested date from actual persisted totals, including empty weekdays and dates outside the loaded history window. Surface storage failure without presenting an invented result.

**Contract**: Search weekdays strictly after source through a single captured local today, in ascending order, for total < 432 minutes; otherwise return today. Read the complete interval via the existing consistent store range contract where applicable; avoid invalid or overflowing date bounds, and do not modify the history snapshot on failure. Cover sparse dates, weekends, exact and excess totals, source today, fallback and storage failure in pure unit tests.

#### 2. Duplicate action and editable draft

**File**: `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`, related feature styles and `src/TimesheetAssistant/UI-PLAYBOOK.md` if the icon-only convention changes.

**Intent**: Make duplication available from each displayed entry through the exact same add-entry control and form opened by “Add entry”, without changing the original or the ordinary add draft.

**Contract**: Provide a keyboard-accessible icon button with an English accessible name and tooltip. Load the suggested date before opening the existing Add entry control and exact same add form, in new-entry mode with a separate draft containing all source fields; allow editing all fields and dates no later than today. Submit through the add operation with a fresh identity; cancel or failed lookup saves nothing. Handle loading and storage errors visibly, prevent duplicate submissions, retain input on save failure, refresh/reveal the destination after success, and preserve dialog focus and both themes.

### Success Criteria:

#### Automated Verification:

- Pure unit tests pass: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`.
- Solution builds: `dotnet build TimesheetAssistant.sln`.

#### Manual Verification:

- Author confirms the visible Duplicate action opens an editable form with the agreed suggested date and saves a separate entry; cancellation does not save.

**Implementation Note**: Complete automated checks and obtain the author's manual confirmation before proceeding to phase 2.

## Phase 2: Desktop verification and acceptance

### Overview

Verify the real published application on isolated data and record evidence and author acceptance.

### Changes Required:

#### 1. Desktop scenarios and evidence

**File**: `src/TimesheetAssistant.UiTests/Features/Workdays/`, `src/TimesheetAssistant.UiTests/README.md`, `context/changes/copy-earlier-day-entries/verification.md`

**Intent**: Establish the real user workflow and persistence, with diagnostics and a reviewable record.

**Contract**: Exercise duplication from initially visible and loaded older entries; weekday suggestion across empty, partial, exact and excess days and weekends; fallback to today; editing and selecting another permitted date; cancel; duplicate Jira IDs and same-date copies; independent modification of source and copy; and save/restart/read with isolated data. Test a storage failure without a partial write or misleading date when feasible with existing fixture. Record package version, source/worktree provenance, commands, TRX counts, failures and cleanup, plus author confirmation; separately identify blocked checks. Preserve existing upgrade and other regression scenarios.

### Success Criteria:

#### Automated Verification:

- Release build and unit tests pass before packaging; package a fresh version using `scripts/Package-Windows.ps1` as described in `src/TimesheetAssistant.UiTests/README.md`.
- Full desktop UI suite passes against the freshly published executable with isolated scenarios and TRX diagnostics, including the configured prior executable for upgrade coverage.

#### Manual Verification:

- Author verifies the duplication workflow and date suggestion in the published application, including keyboard access and both themes, and confirms acceptance in `verification.md`.

## Testing Strategy

### Unit Tests:

- Cover date boundaries, sparse weekday totals, weekends, exact/excess totals, fallback and store failure with the project's fake store and Shouldly conventions.

### Integration Tests:

- Use the existing UiTests project and real Release application; do not create an IntegrationTests project. Verify persistence across owned process restarts and regression of existing workday/history workflows.

### Manual Testing Steps:

1. Duplicate an earlier entry, inspect the suggested date and copied fields, modify and save; verify the original is unchanged.
2. Cancel a second duplication and verify no entry appears; choose the source date for another copy and verify both rows remain independent.
3. Check keyboard focus, icon label and appearance in light and dark themes on the published package.

## Performance Considerations

Do not issue one storage request per calendar day. Read the relevant persisted interval in a bounded operation and compute from its totals; avoid coupling the result to the currently loaded history page.

## Migration Notes

No schema migration is planned. Existing entries remain unchanged; new entries use the current store and validation contracts.

## References

- `context/foundation/roadmap.md` — S-08 and local verification requirements.
- `context/foundation/prd.md` — FR-006.
- `src/TimesheetAssistant.Core/Features/Workdays/WorkEntryHistoryService.cs` — history paging.
- `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor` — table, dialog and save flow.
- `src/TimesheetAssistant.UiTests/README.md` — packaging and real desktop test procedure.

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append a commit SHA when a step lands.

### Phase 1: Working duplication

#### Automated

- [x] 1.1 Pure unit tests pass: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` — 689e709
- [x] 1.2 Solution builds: `dotnet build TimesheetAssistant.sln` — 689e709

#### Manual

- [x] 1.3 Author confirms the visible Duplicate action opens an editable form with the agreed suggested date and saves a separate entry; cancellation does not save. — 689e709

### Phase 2: Desktop verification and acceptance

#### Automated

- [ ] 2.1 Release build and unit tests pass before packaging; package a fresh version using `scripts/Package-Windows.ps1` as described in `src/TimesheetAssistant.UiTests/README.md`.
- [ ] 2.2 Full desktop UI suite passes against the freshly published executable with isolated scenarios and TRX diagnostics, including the configured prior executable for upgrade coverage.

#### Manual

- [ ] 2.3 Author verifies the duplication workflow and date suggestion in the published application, including keyboard access and both themes, and confirms acceptance in `verification.md`.
