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
