# Review Fixes

## Phase 2 — F1: First initialization and Retry

- Decision: user approved the proposed temporary-database fix on 2026-11-13.
- Status: implemented in the working tree; verification results are recorded in verification.md.
- Code: src/TimesheetAssistant/Features/Workdays/StorageInitialization.cs.
- Change: create a unique temporary database, commit schema v1 and close all provider
  handles before moving it to timesheet.db without overwrite. Cleanup only files
  owned by that attempt, under the directory lease. Cleanup failure can leave an
  unused uniquely named temporary file; it cannot cause a later attempt to treat
  that file as the final store. Pre-existing databases retain their protections.
- Remaining evidence: real desktop first-initialization failure/retry scenario in
  phase 4, using an isolated test-owned domain directory. No real-storage tests
  were added to UnitTests.

## Phase 2 — F2: Desktop domain isolation

- Decision: user approved the isolated test-data directory fix on 2026-11-13.
- Status: implemented in the working tree; verification results are recorded in verification.md.
- Code: src/TimesheetAssistant.UiTests/DesktopAppFixture.cs; adjacent README.
- Change: always override the child's TIMESHEET_ASSISTANT_DATA_DIRECTORY with a
  unique absolute session/data path, separate from its webview2 profile. Log the
  data-directory identity without records and validate the GUID session root
  before recursive cleanup, after owned processes have exited.
- Remaining scope: phase 4 must retain scenario-owned domain storage across fresh
  launch sessions; the current smoke's domain directory has a single-session lifetime.

## Full review — F1: Input readiness

- Decision: user approved diagnosis and a fix on 2026-11-14.
- Status: fixed in the working tree; final full desktop verification passed 11/11.
- Code: src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor;
  src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayScreen.cs;
  src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayInputTests.cs.
- Change: acknowledge the rendered snapshot date and idle state before typing;
  verify field values and wait for post-save/post-edit focus. Added a held-event
  readiness regression and rapid twelve-entry save/restart/read regression.
- Evidence: the held-event regression passes with the fix and fails after
  removing only the new snapshot-date wait. Original helper bytes were restored.
  Package 111405 passed all eleven desktop tests and all 21 sessions cleaned up.
  Release build and 87 pure tests passed. See verification.md for commands,
  intermediate failures, package provenance and diagnostic limitations.
- Remaining decision: full-review F2 (culture-dependent caption), separately
  recorded as phase 3 review F1, has not been changed by this fix.

## Full review — F2 / Phase 3 — F1: English caption date

- Decision: user approved explicit en-US formatting on 2026-11-14.
- Status: fixed in the working tree; both reports now record FIXED.
- Code: src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor.
- Change: pass CultureInfo.GetCultureInfo("en-US") to the caption date's
  ToString("D") call. Persistence and the native date picker are unchanged.
- Verification: Release build passed with 0 warnings/errors; 87 pure tests
  passed; the exact DateOnly expression produced English text with current
  culture pl-PL. git diff --check passed. No new desktop run is claimed for this
  caption-only change. Full-review triage is complete with F1/F2 fixed.
