# Record Persistent Workday — Plan Brief

> Full plan: [plan.md](plan.md)

## What & Why

S-01 gives Timesheet Assistant its first useful product workflow: record work for a date and retain it after closing the app. Users can add, edit and delete separate entries while seeing the total and remaining time or excess against 7 h 12 min.

## Starting Point

The app is a Windows-only MAUI Blazor starter without domain logic or storage. UnitTests is empty; the real desktop harness currently checks Counter and provides no persistence evidence.

## Desired End State

The root screen opens today, supports any earlier date including weekends, and saves independent ticket entries locally. Explicit Save commits changes; restart and launching from another app folder preserve records. Visible errors never masquerade as successful writes or silently replace existing data.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| Duration input | Text: 1h, 30m, 6h 12m or 6h12m; H/M accepted | Fast keyboard entry with an explicit grammar. |
| Duration validation | 1–1440 whole minutes; mixed minutes 0–59; no fractions, bare numbers or colons | Reject ambiguous input and catch likely mistakes. |
| Fields | Trim outer whitespace; ID/title required, description optional; no Jira-key pattern | Preserve manual entry without unnecessary Jira coupling. |
| Date selection | Local today or any past date, including weekends | Supports backfilling without future work entries. |
| Saving/discard | Explicit Save; confirm discard on changed-form context switches and Cancel | Avoid accidental form loss without persisting incomplete data. |
| Deletion | Confirm ticket and duration, Delete/Cancel | Protect committed entries against accidental clicks. |
| Concurrent launches | One active process per data directory | Avoid conflicting editors in the simple MVP. |
| Storage | SQLite / Microsoft.Data.Sqlite; no EF Core | Transactional record changes and later history queries. |
| Location | %LOCALAPPDATA%\TimesheetAssistant\timesheet.db | Data survives changes to the extracted app folder. |
| Testing | Portable Core + pure UnitTests; real EXE UiTests with separate domain directories | Tests shared rules and actual storage without another test project. |
| Unit test readability | Commented Arrange/Act/Assert; incidental setup in named builders/helpers | User convention added on 2026-11-12; test bodies expose the scenario, operation and expected result. |
| Code organization | Features/Workdays within each applicable project; feature-oriented folders for future work | User convention added on 2026-11-12; keep related code together while preserving project boundaries. |
| Ordering | Stable record ID and creation order retained on edit/restart | Keep duplicate tickets separate and prepare S-02's tie rule. |

## Scope

**In scope:**

- Manual date-based CRUD, validation, daily totals against 432 minutes, durable storage and ownership/error handling.
- Local published-app tests covering restart, independent stores, failed operations and another app folder.
- Truthful delivery text and recorded local acceptance evidence.

**Out of scope:**

- History landing page, copying, templates, summaries, transfers, outstanding days and leave handling.
- Future dates, unsaved-draft recovery/native-close interception, cloud services, multiple editors, backups/export and EF Core.
- Desktop CI experiments, pushes/releases, clean-device or full MVP acceptance.

## Architecture / Approach

A small net10.0 Core library owns rules and a store-facing service; both the app and UnitTests reference it. The Windows app composes SQLite, a directory lease and a Razor workday editor. Schema v1 retains date, text fields, integer minutes, stable identity and creation order. Tests override only the child process's data directory and use fresh WebView2 profiles across launches. Unsupported/corrupt stores remain intact and unavailable for editing.

Feature code and tests live under `Features/Workdays/` in their respective projects, including the root-route Razor page. Shared routing/layout, app composition and desktop-test infrastructure keep their existing locations. Future feature names follow actual responsibilities, without project-wide Models/Interfaces/Services folders.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Entry Model and Day Rules | Core contracts, parser, validation, totals and fake-store tests | Parser boundaries and preserved identity/order. |
| 2. Durable Local Store | SQLite, stable directory, schema version and process lease | No destructive fallback; no test access to real data. |
| 3. Workday Recording Screen | Date/form/list, CRUD, totals and confirmations | Commit before success; preserve dirty context on cancellation. |
| 4. Real Desktop Verification and Delivery Evidence | Published-app scenarios and local acceptance record | Separate domain lifetime from process/profile cleanup. |

**Prerequisites:** Windows, pinned .NET 10 SDK/maui-windows workload and WebView2; existing local UI harness. F-01 completion is not required.
**Estimated effort:** Approximately 4–7 focused development sessions across four phases; a rough planning estimate, not a deadline commitment.

## Open Risks & Assumptions

- SQLite native assets must work in the actual published package; compilation alone is insufficient.
- Storage-error scenarios need bounded, test-owned failure setup. Real user data must never be used for induced failures.
- Unsaved drafts are not recovered after native close. Only successfully saved entries are promised to survive restart.
- Local success does not resolve the deferred CI failure or establish clean-device compatibility.

## Success Criteria (Summary)

- Separate entries, date isolation, edit/delete and 0/under/exact/over-target totals behave as agreed.
- Real EXE tests prove retained data across restarts, independent profiles/stores and another app folder; failures preserve committed data.
- Pure tests, local desktop results and author confirmation are recorded; required blocked checks prevent local acceptance.

## References

- [PRD](../../foundation/prd.md), [roadmap v3](../../foundation/roadmap.md), [test architecture](../../foundation/tech-stack.md).
- [UI test setup](../../../src/TimesheetAssistant.UiTests/README.md), [deferred F-01](../verify-desktop-ui-path/resume.md).
