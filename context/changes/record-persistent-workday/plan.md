# Record Persistent Workday Implementation Plan

## Overview

Implement roadmap S-01: select a date, record separate work entries, edit or delete them, and see the total, remaining time or excess against 7 h 12 min. Persist committed changes locally across process restarts and application-folder changes.

The user approved MEDIUM complexity, seven design decisions and these four phases on 2026-11-11. This plan authorizes no CI experiments or deployment.

## Current State Analysis

The Windows-only .NET 10 MAUI Blazor application is still a template. There is no work-entry model, domain service or persistent store. Home, Counter and Weather are sample pages. The portable UnitTests project is empty. UiTests launches the real Release EXE and connects to its WebView2, but checks only Counter.

Roadmap v3 permits S-01 implementation and local acceptance before completion of F-01. F-01 remains the existing deferred desktop CI item. No lessons.md was present during discovery. The working tree was clean before creating these planning artifacts.

## Desired End State

Opening the app displays a workday screen defaulting to the machine's local current date. Users can select today or any earlier date, including weekends; future dates are rejected. Each saved entry has its own identity, date, Jira ID, title, optional description and positive whole-minute duration. Repeated ticket IDs remain separate entries. Entries display in original creation order, preserved by edits and restarts.

Save is explicit. The UI reports success and changes the visible saved list/totals only after durable commit. Errors retain the form and previously committed records. Changing the selected date or editing another entry with a dirty form asks whether to discard; cancelling preserves the form and context. A changed form's Cancel action also confirms discard. Native window-close recovery of unsaved drafts is outside S-01; committed entries remain durable.

Duration accepts ASCII non-negative integers with h/m units, case-insensitively: `1h`, `30m`, `6h 12m`, `6h12m`, and surrounding whitespace. Allow spaces between the hour and minute components; reject signs, fractions, bare numbers, colons, duplicate/reversed units and other text. A standalone minute value may exceed 59 (`90m`); the minute component in mixed input is 0–59. Total duration must be 1–1440 minutes inclusive: `24h` and `1440m` pass; `24h 1m`, `0m`, `6h 60m` and overflow fail. Format saved durations consistently as hours/minutes, omitting zero components except `0m` for an empty total.

Trim outer whitespace from Jira ID, title and description; ID and title must remain nonempty. Do not impose a Jira project-key pattern. Empty descriptions normalize to empty text. Render stored text as text, never raw markup. Do not invent additional business field-length restrictions in this slice.

The daily target is 432 minutes. Empty day: total `0m`, remaining `7h 12m`; 60 minutes: remaining `6h 12m`; 432: remaining `0m`; 433: excess `1m`. Above-target saves remain allowed. Totals use a sufficiently wide checked numeric representation and never wrap silently. Final summaries are absent until S-02, which will enforce exact completion.

SQLite through Microsoft.Data.Sqlite stores data in `%LOCALAPPDATA%\TimesheetAssistant\timesheet.db`, outside the app folder. Only one active process may edit a given canonical data directory. Another process shows an English explanatory message and offers no editing. A failed or unsupported store is never silently recreated or replaced with an empty one.

### Key Discoveries:

- `src/TimesheetAssistant/MauiProgram.cs:17`: existing composition point registers only BlazorWebView.
- `src/TimesheetAssistant/Components/Pages/Home.razor:1`: template root route to replace with the workday screen.
- `src/TimesheetAssistant/Components/Layout/NavMenu.razor:16`: sample Counter/Weather navigation.
- `src/TimesheetAssistant/TimesheetAssistant.csproj:4`: retain Windows-only TargetFrameworks.
- `src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj:4`: portable net10.0 tests need portable production logic rather than a direct MAUI reference.
- `src/TimesheetAssistant.UiTests/DesktopAppFixture.cs:76`: profile/session lifetime is currently one-shot; cleanup deletes its directory.
- `src/TimesheetAssistant.UiTests/DesktopAppFixture.cs:131`: child-only environment is the isolation insertion point.
- `src/TimesheetAssistant.UiTests/DesktopConnectionTests.cs:20`: temporary Counter test must be replaced, not cited as persistence evidence.
- `scripts/Package-Windows.ps1:37`: generated delivery text still says storage is absent and must reflect S-01 after implementation.

## What We're NOT Doing

- History landing page, copying, templates, outstanding days, holidays/leave, summaries and transfer confirmations: S-07/S-08/S-02–S-06 retain their roadmap scope.
- Future-dated entries, fractions of minutes, bare-number duration input or `6:12` syntax.
- Multiple simultaneous editors for one store, automatic backups/export, cloud sync, authentication or Jira/MyWork integration.
- Persisting unsaved drafts or intercepting native window close to protect them; users must Save before closing.
- EF Core, an IntegrationTests project, broad UI redesign, non-Windows application targets.
- Restoring the F-01 workflow candidate, hosted diagnostic runs, pushes, releases, clean-device acceptance or marking the whole MVP complete.

## Implementation Approach

Apply the feature-oriented convention in `src/AGENTS.md`, explicitly requested by the user on 2026-11-12. For S-01 use `Features/Workdays/` within Core, the application and both test projects. This is a product responsibility name, not a required name for later features. Keep feature-owned types, contracts, services, SQLite adapters and Razor components together within their respective project boundaries; do not create project-wide folders by technical type. Shared app composition, routing/layout and desktop-test infrastructure retain their conventional locations. Extract shared code only when concrete reuse exists.

Add a small `TimesheetAssistant.Core` net10.0 library for entry types, duration parsing, validation, day calculations and the store-facing application service. Reference it from the MAUI app and UnitTests. Keep SQLite, filesystem paths and process ownership in the Windows application, behind a Core store contract. Unit tests use fakes and never real storage.

Use a single SQLite store with schema version 1 and transactions for mutations. Persist stable entry IDs and a database-assigned monotonically increasing creation order independent of editable fields and timestamps. Preserve both on edits, including after restart; they prepare S-02's already specified tie rule without implementing summaries. An entry's date is fixed during editing in S-01; select another date before creating a different entry.

Use an app-lifetime exclusive handle on a lock file inside the canonical data directory, acquired before opening/initializing the database. Retain it until application shutdown; OS handle release must permit reopening after normal close or termination. Do not steal/delete an active lock or change machine settings. Directory aliases/reparse points must not permit opening the same database through two independent lock identities; keeping the lock at the database's actual directory and using filesystem exclusivity is preferred to a path-hashed mutex.

Define `TIMESHEET_ASSISTANT_DATA_DIRECTORY` as an absolute-path override for the child process. Invalid or inaccessible configured paths fail visibly instead of falling back to the user's real store. UiTests always supplies this override, independently of WEBVIEW2_USER_DATA_FOLDER. Normal launches use the fixed LocalApplicationData path.

## Critical Implementation Details

### State sequencing

Commit before presenting success or clearing a dirty form. A failed mutation must not leave optimistic list/totals changes. On failed date reads, do not label old records as belonging to the newly selected date; retain the previous context or show an explicit unavailable state.

### Timing & lifecycle

Restart tests must retain the domain directory while fully stopping the first process and using a fresh WebView2 profile for the next launch. Current fixture cleanup deletes its session directory, so domain storage must have a separate scenario-owned lifetime. Acquire the store lock before schema initialization; a rejected second process must not touch the database.

## Phase 1: Entry Model and Day Rules

### Overview

Establish portable production rules and testable contracts without MAUI or disk access.

### Changes Required:

#### 1. Portable domain and service

**Files**: new `src/TimesheetAssistant.Core/TimesheetAssistant.Core.csproj`; `WorkEntry.cs`, `DurationParser.cs`, `WorkEntryValidator.cs`, `WorkdayTotals.cs`, `IWorkEntryStore.cs`, `WorkdayService.cs` under `src/TimesheetAssistant.Core/Features/Workdays/`.

**Intent**: Centralize accepted input rules, date validation and daily calculations so UI and persistence cannot disagree.

**Contract**: net10.0 with nullable enabled; WorkEntry carries ID, DateOnly work date, Jira ID, title, description, integer duration minutes and immutable creation order. Store supports ordered reads by date and atomic add/update/delete by identity; creation order is assigned by the store. Service validates against an injectable local-date source and calls the store only for valid mutations. Editing cannot move entries to another date. Missing IDs and storage failures return explicit failures without modifying the successful snapshot. No storage/provider/MAUI dependencies in Core.

#### 2. Project wiring and pure tests

**Files**: `TimesheetAssistant.sln`, `src/TimesheetAssistant/TimesheetAssistant.csproj`, `src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`, new tests under `src/TimesheetAssistant.UnitTests/Features/Workdays/` and UnitTests README.

**Intent**: Exercise actual shared production logic without launching an EXE or opening a database.

**Contract**: Both projects reference Core. Preserve the app TargetFrameworks property. Tests cover the complete accepted/rejected duration grammar, boundaries/overflow, trimmed fields, optional description, future-date rejection with a controlled clock, weekend/past acceptance, repeated ticket IDs, edit identity/order, date partitioning, totals 0/60/431/432/433 and above 24h aggregate, and failure behavior using fake stores.

### Success Criteria:

#### Automated Verification:

- Core rule and fake-store tests pass: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` with at least one executed test and no real storage.
- Solution builds with Core references and the retained Windows-only app target: `dotnet build TimesheetAssistant.sln`.

## Phase 2: Durable Local Store

### Overview

Add the production SQLite adapter and safe application-lifetime storage composition.

### Changes Required:

#### 1. SQLite adapter and schema

**Files**: new `SqliteWorkEntryStore.cs`, `StorageInitialization.cs` under `src/TimesheetAssistant/Features/Workdays/`; app csproj.

**Intent**: Persist independent work entries transactionally and support future slices without implementing their features.

**Contract**: Microsoft.Data.Sqlite compatible with .NET 10, exact stable package version pinned at implementation and audited. Schema v1 contains a unique stable entry ID, ISO local work date, required trimmed ID/title, optional description, minutes constrained to 1–1440, and unique database-generated creation order (not regenerated on edit). Index date plus order; parameterize SQL. Initialize a new store transactionally and record schema version with SQLite user_version. Existing unsupported versions or corrupt databases produce a visible failure and no destructive reset. Update/delete affect only the requested ID and verify expected affected rows. Close/dispose resources cleanly; bound lock/busy waits.

#### 2. Data location, ownership and composition

**Files**: new `DataDirectory.cs`, `DataDirectoryLease.cs` under `src/TimesheetAssistant/Features/Workdays/`; `src/TimesheetAssistant/MauiProgram.cs`, application lifecycle wiring where needed.

**Intent**: Keep user data independent of binaries and prevent concurrent editors or test access to the real store.

**Contract**: Fixed normal directory and child override as defined above; canonical absolute resolution, no fallback after an invalid override. Acquire exclusive filesystem lease before DB access and retain for app lifetime. Expose initialization/ownership errors to a renderable English unavailable screen with Retry where meaningful, rather than an unhandled blank window. No editing while unavailable. A second process cannot alter the first process's store. No production records in diagnostic output.

### Success Criteria:

#### Automated Verification:

- Solution build and pure unit tests pass after adding the provider: `dotnet build TimesheetAssistant.sln` and the UnitTests project command.
- Dependency audit completes and findings are recorded: `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`.

Persistence/ownership acceptance is deliberately exercised through the real UI in phase 4, after phase 3 exposes the workflow. Do not claim adapter acceptance from compilation or add real-storage tests to UnitTests.

## Phase 3: Workday Recording Screen

### Overview

Deliver the real user workflow using the shared rules and durable service.

### Changes Required:

#### 1. Date, entry list and editor

**Files**: new `src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor`, adjacent components/form model/scoped CSS as needed; remove superseded `src/TimesheetAssistant/Components/Pages/Home.razor`; update Razor imports/namespaces as needed.

**Intent**: Replace template Home with a focused day editor using existing Bootstrap styling.

**Contract**: Root route defaults to local today, supports past dates/weekends and rejects future dates in service and UI. Show accessible labels for date, Jira ID, Item title, Work description and Duration, examples of valid duration input, explicit Save/Cancel, ordered separate rows with Edit/Delete, loading/empty/error states and daily total/remaining/excess. Disable conflicting operations while loading/saving. Capture mutation context so a late response cannot modify another date's screen. Invalid submissions show actionable English field errors and preserve input. Successful adds clear the form; edits preserve record identity/order; cancel does not mutate data. No summary button or fake transfer state.

#### 2. Confirmations and navigation cleanup

**Files**: `Features/Workdays/` page/editor components, `Components/Layout/NavMenu.razor`, `Components/Layout/MainLayout.razor`, template `Components/Pages/Counter.razor` and `Components/Pages/Weather.razor` within the application.

**Intent**: Apply approved protections and remove sample navigation from the product workflow.

**Contract**: Confirm dirty-form discard before date/entry changes and Cancel; cancelling leaves context/input unchanged. Confirm deletion with ticket and duration plus Delete/Cancel; cancelling leaves records/totals unchanged. Handle deleting a currently edited entry without leaving a stale editor. Remove unused sample pages/links and template About link; retain only useful workday navigation, with dirty-state protection if it can change context. App UI stays entirely English. Native-close draft recovery remains excluded.

### Success Criteria:

#### Automated Verification:

- Solution build and all pure tests pass after the workday UI replaces the starter.

#### Manual Verification:

- Author confirms readable English date/form/list UI, keyboard operation, field errors, remaining/excess states and both discard/delete cancellation paths on the Windows app.

After automated checks, obtain human confirmation of this manual criterion before progressing past phase 3. Record blocked checks independently; do not mark the manual row from an agent assertion alone.

## Phase 4: Real Desktop Verification and Delivery Evidence

### Overview

Prove that the published app saves actual data and retains it across launches and application directories.

### Changes Required:

#### 1. Scenario-owned storage and sessions

**Files**: `src/TimesheetAssistant.UiTests/DesktopAppFixture.cs`, replacement for `DesktopConnectionTests.cs`, new scenario/test helpers, UiTests README.

**Intent**: Replace Counter evidence with isolated product workflows while retaining owned-process cleanup and diagnostics.

**Contract**: Each scenario owns a unique domain directory outside each launch's profile/session directory. Supply its absolute path only to the launched child. Use fresh fixture/session instances per launch or an explicit restart API with equivalent cleanup. Retain the directory across the scenario's launches and remove only that verified test-owned path after all owned processes exit. Do not share mutable class-fixture state between independent test cases. Preserve bounded startup/UI/cleanup, ownership validation, stage logs, failure screenshots/traces where available and original-error reporting. Domain directory identity and launch stages appear in diagnostics without entry content.

#### 2. Product and failure scenarios

**Files**: new `WorkdayRecordingTests.cs` and `WorkdayStorageTests.cs` under `src/TimesheetAssistant.UiTests/Features/Workdays/`.

**Intent**: Exercise persistence through visible behavior rather than direct test writes of normal work records.

**Contract**: Through UI add `T-1 / Scrum Ceremonies / Refinement / 1h` and `X-1 / Order Page / empty description / 6h 12m`; assert 432 minutes and zero remaining. Add a separate T-1 entry of 1m; assert three separate rows and excess 1m. Edit that entry to 2m; assert excess 2m, then verify delete Cancel preserves it and confirmed Delete restores 432. Verify invalid input and dirty-form cancellation, and store a distinct earlier weekend entry. Fully stop and relaunch with the same domain directory and a fresh profile; assert exact surviving contents, order and date totals, including the saved edit in a dedicated retained record and absence of the deleted record. Repeat another restart after a further edit/delete to prove those mutations persist, not just initial adds.

**Contract**: A second independent domain directory starts empty while the first still contains its records. Launch a second process using the first active directory; assert ownership message and no editor, then close the owner and verify a subsequent launch acquires the directory. Launch the same published binaries from another copied/extracted app folder with the same test domain directory and confirm unchanged data. Exercise invalid data-directory configuration, an unreadable/corrupt DB and an unsupported future schema using only test-owned fixture preparation; verify visible failure and original bytes retained (do not require byte identity for valid databases opened normally). Exercise a failed mutation using a bounded test-owned filesystem/database lock or permission condition; assert no false success, no input loss and original committed values after recovery/restart. Select the most reliable local failure setup during implementation; no production-only fault endpoint.

#### 3. Verification record and truthful delivery text

**Files**: new `context/changes/record-persistent-workday/verification.md`; UnitTests/UiTests READMEs; `scripts/Package-Windows.ps1`; `.github/workflows/windows.yml` unit-step wording only; current-state paragraphs in root AGENTS.md and `context/deployment/deploy-plan.md` as needed.

**Intent**: Record actual feature evidence and stop generated packages/instructions from claiming storage/tests are absent.

**Contract**: Keep historical evidence intact; append/update current descriptions with exact source revision, commands, package directory, environment, executed/pass/fail counts, diagnostics and manual outcome. State local S-01 acceptance separately from deferred CI, clean-device compatibility and full MVP acceptance. Preserve current build/package/release behavior and F-01 checkpoint; change only misleading unit-step text in the active workflow. Run the implementation review chain and triage findings before merging.

### Success Criteria:

#### Automated Verification:

- Release build and UnitTests pass before packaging, with nonzero executed pure tests and no failures.
- A fresh package is created and the UiTests project passes against its `artifacts/publish-<version>/TimesheetAssistant.exe`, with nonzero executed desktop tests and successful cleanup.
- Desktop assertions prove CRUD persistence, date isolation, restart with fresh profiles, independent domain directories, second-process exclusion, alternate app-folder reads and safe storage-failure behavior.
- Verification record and delivery text describe actual outcomes; `git diff --check` passes and no data, artifacts, credentials or Visual Studio state are tracked.

#### Manual Verification:

- Author confirms the published Windows app's ordinary Save, close/reopen, edit/delete and launch from another app folder retain the expected records; confirmation and package identity are recorded.

After automated checks, obtain human confirmation of this final manual criterion before claiming local acceptance. A blocked required desktop check blocks S-01 acceptance, not permission to invent a replacement browser-only proof.

## Testing Strategy

### Unit Tests

Use the phase 1 matrix and fake storage to test production Core rules, failures and state calculations. No Playwright, EXE, native SQLite or filesystem in UnitTests. Inject a date source to avoid midnight-dependent tests. Add a pure regression when fixing a discovered rule defect.

Follow `src/AGENTS.md` unit-test readability rules, requested by the user on 2026-11-12: every test has explicit `// Arrange`, `// Act`, `// Assert` sections. Keep only the scenario's essential inputs, explicit operation and expected outcome in the test method. Put incidental object construction, fake setup and repetitive plumbing in intention-revealing feature-local builders, extension methods, private methods or fixtures. Keep scenario-defining values and expectations visible; helper calls must not obscure the behavior or introduce shared mutable state. This applies from phase 1 onward.

### Real Desktop UI Tests

Use the phase 4 scenarios through the real published EXE. This is the accepted component/persistence verification layer; do not introduce an IntegrationTests suite. Keep deterministic fixture data, per-scenario storage and fresh per-launch profiles. A test is successful only if assertions and teardown pass. Inspect TRX counts; discovery alone is insufficient.

### Commands and Evidence

Run from the repository root. Build Release and run `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release` before packaging. Audit dependencies with the phase 2 command. Use `scripts/Package-Windows.ps1` with a unique valid preview version/build number, exact source commit and fresh ignored output directory so the result contains `publish-<version>/TimesheetAssistant.exe`; label any uncommitted working-tree package accurately. Do not publish a release.

Set `TIMESHEET_ASSISTANT_TEST_APP` to that absolute EXE path, then run `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --logger trx`. Restore the previous caller environment afterward. The fixture supplies storage/CDP settings only to its children. Record results in verification.md; keep TRX and session diagnostics in ignored `artifacts/desktop-ui/`.

### Manual Testing Steps

1. Check the date defaults to today and future dates are rejected; select a past weekend.
2. Add valid entries and inspect 0/under/exact/over-target totals; try invalid duration and missing required fields.
3. Edit, cancel dirty-form discard, confirm discard, cancel deletion and confirm deletion; confirm repeated ticket entries remain independent.
4. Save and close normally; reopen and verify records. Close and launch an identical package from another folder, verifying the same normal local data without copying a database into the app folder.
5. Confirm clear unavailable/second-instance messages and no false successful writes. Use only isolated fake data for induced failures.

## Performance Considerations

Query only the selected date and order by persisted creation sequence. Avoid loading/replacing full history per mutation. There is no numerical response-time requirement; use existing bounded UI test timeouts, not invented throughput goals. Keep SQLite operations from freezing the UI and serialize mutations. No speculative caching or database framework is needed for a few entries per day.

## Migration Notes

No domain store exists today, so v1 creates an empty database only when none exists. Store versioning must reject unknown future versions without writes and support later explicit migrations. No automatic retention or cleanup of user entries. Do not store user data in the package or WebView2 profile. Binary rollback must preserve the data directory; it does not downgrade schema. If later schema changes make an old binary incompatible, it must report that incompatibility instead of resetting data. Automatic backup/recovery UI is outside this slice.

## References

- `context/foundation/prd.md`: FR-002, FR-005, FR-007 and persistence/non-retention requirements.
- `context/foundation/roadmap.md`: v3 S-01 and deferred F-01 sequencing.
- `context/foundation/tech-stack.md`: accepted two-project test architecture.
- `src/AGENTS.md`: English source/UI/test content.
- `src/TimesheetAssistant.UiTests/README.md` and `context/changes/verify-desktop-ui-path/resume.md`: local harness and CI limits.
- [Microsoft MAUI local databases](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/database-sqlite): SQLite provider option.
- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions) and [database errors](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors): adapter guidance.

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Entry Model and Day Rules

#### Automated

- [x] 1.1 Core rule and fake-store tests pass: `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` with at least one executed test and no real storage. — bc75d1c
- [x] 1.2 Solution builds with Core references and the retained Windows-only app target: `dotnet build TimesheetAssistant.sln`. — bc75d1c

### Phase 2: Durable Local Store

#### Automated

- [x] 2.1 Solution build and pure unit tests pass after adding the provider: `dotnet build TimesheetAssistant.sln` and the UnitTests project command. — 0459e73
- [x] 2.2 Dependency audit completes and findings are recorded: `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`. — 0459e73

### Phase 3: Workday Recording Screen

#### Automated

- [ ] 3.1 Solution build and all pure tests pass after the workday UI replaces the starter.

#### Manual

- [ ] 3.2 Author confirms readable English date/form/list UI, keyboard operation, field errors, remaining/excess states and both discard/delete cancellation paths on the Windows app.

### Phase 4: Real Desktop Verification and Delivery Evidence

#### Automated

- [ ] 4.1 Release build and UnitTests pass before packaging, with nonzero executed pure tests and no failures.
- [ ] 4.2 A fresh package is created and the UiTests project passes against its `artifacts/publish-<version>/TimesheetAssistant.exe`, with nonzero executed desktop tests and successful cleanup.
- [ ] 4.3 Desktop assertions prove CRUD persistence, date isolation, restart with fresh profiles, independent domain directories, second-process exclusion, alternate app-folder reads and safe storage-failure behavior.
- [ ] 4.4 Verification record and delivery text describe actual outcomes; `git diff --check` passes and no data, artifacts, credentials or Visual Studio state are tracked.

#### Manual

- [ ] 4.5 Author confirms the published Windows app's ordinary Save, close/reopen, edit/delete and launch from another app folder retain the expected records; confirmation and package identity are recorded.
