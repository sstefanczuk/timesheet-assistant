# Record Persistent Workday — Verification

## Phase 2: Durable Local Store

Date: 2026-11-12. Source: working-tree phase 2 changes based on `01eae88`.
Environment: Windows x64, .NET SDK 10.0.403; pinned workload set 10.0.402.1,
installed maui-windows 10.0.110/10.0.100. No package or release was created.

- `dotnet build TimesheetAssistant.sln`: succeeded, 0 warnings, 0 errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`:
  87 passed, 0 failed, 0 skipped. The first sandboxed invocation was blocked
  before test execution by denied access to the user-level NuGet.Config;
  the authorized invocation with the required access passed.
- `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`:
  completed successfully; no vulnerable packages reported for the application,
  Core, UnitTests or UiTests. This records the feed result on this date.
- `git diff --check`: passed.

The provider is pinned to [Microsoft.Data.Sqlite 10.0.11](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.11),
a stable .NET Standard 2.0 package compatible with .NET 10. The package supplies
SQLitePCLRaw native dependencies. Parameterized commands and transactional commits
follow [Microsoft's transaction guidance](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).
Each operation owns its connection with pooling disabled; busy waits are bounded
to five seconds using DefaultTimeout, including implicit transaction commands
([provider error guidance](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors)).
Synchronous provider work runs on a worker thread because the provider's
[async APIs execute synchronously](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async).

Phase 2 adds no tests: the existing pure suite remains free of real storage.
The deliberate-break gate is inapplicable. No normal user database was opened
for verification. Persistence, process exclusion, Retry, corrupt/future schema
preservation, native package loading and restart behavior still require the
phase 4 real desktop scenarios after phase 3 exposes the workday workflow.
No desktop CI, clean-device or S-01 local acceptance is established here.

Implementation mode adaptation: the user selected delegation, but the subagent
had no filesystem tools and returned without changes. The parent implemented
the phase without altering its product scope or architecture.

## Phase 2 review triage — F1

2026-11-13: User approved the temporary-database approach. Working-tree changes
based on 0459e73 create and initialize a uniquely named temporary file while the
directory lease is held, dispose SQLite handles and publish it with
`File.Move(temporaryPath, databasePath, overwrite: false)`. Cleanup targets only
the attempt-owned temporary database and rollback journal. Existing final files
are never initialized as new, deleted or replaced.

- `dotnet build TimesheetAssistant.sln`: passed; 0 warnings, 0 errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`:
  87 passed, 0 failed, 0 skipped; executed after the build completed.
- `git diff --check`: passed.

These checks establish compilation and existing pure-rule regression results.
No new tests or real database operations were added to UnitTests. An induced
first-initialization failure followed by successful Retry still needs the
phase 4 isolated desktop scenario. No package was created or desktop app launched.

## Phase 2 review triage — F2 and local smoke

2026-11-13: User approved child-only domain isolation in the existing desktop
fixture. Source: 0459e73 plus uncommitted F1/F2 fixes; this is a working-tree
verification output, not a release. No CI run, ZIP or release was created.

- `dotnet build TimesheetAssistant.sln -c Release`: passed, 0 warnings, 0 errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release`:
  87 passed, 0 failed, 0 skipped; completed before publication.
- Fresh local publication:
  `dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -p:Version=0.1.0-preview.111301 -p:ApplicationVersion=1113 -o artifacts/publish-0.1.0-preview.111301`:
  succeeded. EXE: `artifacts/publish-0.1.0-preview.111301/TimesheetAssistant.exe`.
- `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=f2-isolation-20261113.trx'`:
  exit 0, TRX total/executed/passed 1, failed 0, skipped 0. The existing
  Home → Counter → 0 to 1 workflow passed and fixture cleanup succeeded.
- During that run the caller's data override was deliberately set to the invalid
  relative value `f2-invalid-inherited-directory`. The fixture replaced it in the
  child's environment with its absolute test-owned session/data path. Both caller
  environment variables (test EXE and data override) were restored in finally.
  Native diagnostic capture was not enabled.
- TRX: `artifacts/desktop-ui/f2-isolation-20261113.trx`.
- Session log: `artifacts/desktop-ui/8685fb68c85e4a08b927f718413a1d62/desktop.log`.
  It records separate webview2/data paths under session
  `208c3c2d3d7340a7a69088accc902917`, interaction completion, owned-process exits
  and `cleanup completed`. A post-run existence check confirmed the logged
  domain directory was absent. Diagnostics contain no entry records.
- `git diff --check`: passed.

The smoke establishes successful new-store startup with the packaged native
provider and the approved child override/cleanup path. It does not exercise CRUD,
restart retention, induced first-initialization failure/Retry, existing-store
protection or second-process exclusion. Those remain phase 4 desktop scenarios.
The single-launch data directory is removed with its session; phase 4 still needs
an independent scenario lifetime. Both findings are fixed in code; no S-01 local
acceptance, clean-device or desktop CI acceptance is claimed.

## Phase 3: Workday Recording Screen

Date: 2026-11-13. Source: uncommitted phase 3 changes based on `cad8254`.
Environment: Windows x64, .NET SDK 10.0.403, workload set 10.0.402.1,
maui-windows 10.0.110/10.0.100. Implementation was delegated as selected by the user.

- `dotnet build TimesheetAssistant.sln -c Release`: passed, 0 warnings/errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release`:
  blocked before execution by sandbox denial reading the user NuGet.Config.
  Reused the successful build's restored dependencies with the same command plus
  `--no-restore`: 87 passed, 0 failed, 0 skipped.
- No tests were added or modified in phase 3; deliberate-break verification is
  inapplicable. Pure tests cover existing Core rules, not Razor interaction.
- `git diff --cached --check`: identified an extra blank line at the end of the
  new confirmation component; removed that whitespace before the final check.
- Fresh local publication:
  `dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -p:Version=0.1.0-preview.111303 -p:ApplicationVersion=1113 -o artifacts/publish-0.1.0-preview.111303`:
  passed. The initial sandboxed invocation could not access the user SDK directory;
  the invocation with the required access succeeded. This is a working-tree
  publication for manual phase 3 verification, without a release or ZIP.

Manual criterion 3.2 is pending author confirmation: readable English date,
form and list UI; keyboard operation; field errors; remaining/excess states;
discard and delete cancellation paths. No desktop interaction or persistence
acceptance is claimed from compilation/publication. The old Counter desktop
test is intentionally superseded and was not run against this UI; phase 4
must replace it with the planned isolated product scenarios. No phase 4 or CI
work was performed, and no phase 3 commit was created before manual confirmation.

2026-11-14: Author confirmed that everything worked as expected after the requested
manual verification using `artifacts/publish-0.1.0-preview.111303/TimesheetAssistant.exe`.
This closes manual criterion 3.2 (English UI, keyboard use, field errors,
remaining/excess and discard/delete cancellation). The earlier sandboxed launch
did not show a visible window; relaunch outside the sandbox produced a titled
application window, and the author subsequently completed verification.
The final staged whitespace check passed after removing the extra EOF blank line.
Phase 4 automated persistence/failure scenarios and final acceptance remain pending.

## Phase 4: Real Desktop Verification and Delivery Evidence

Date: 2026-11-14. Source: uncommitted phase 4 changes based on
`5622cbd00bd258b509353493eac5c57356708c4a`. The package is a working-tree
verification build, not an exact committed revision or a release. Production
behavior is unchanged in this phase. Environment: Windows x64 developer machine,
.NET SDK 10.0.403 and the existing MAUI Windows/WebView2 installation.

Implementation mode: the selected subagent returned without edits because its
session exposed no filesystem/shell tools. The parent completed the same scope.
The roadmap already listed S-01 as in-progress. No lessons.md exists.

### Build, package and audit

- `dotnet build TimesheetAssistant.sln -c Release`: exit 0, 0 warnings/errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-restore`:
  87 executed/passed, 0 failed/skipped, before packaging. UnitTests remains pure.
- Initial package attempt `0.1.0-preview.111401` failed before publication because
  the sandbox denied access to the user Windows SDK directory. Its unused output
  directories were left in ignored artifacts; no successful package is claimed.
- `./scripts/Package-Windows.ps1 -Version 0.1.0-preview.111402 -Commit 5622cbd00bd258b509353493eac5c57356708c4a -BuildNumber 1114`:
  succeeded with required SDK access. Fresh publication:
  `artifacts/publish-0.1.0-preview.111402/TimesheetAssistant.exe`.
  ZIP: `artifacts/release-0.1.0-preview.111402/TimesheetAssistant-0.1.0-preview.111402-win-x64.zip`.
  SHA-256: `7e2762a3cd1d8bd32604e2845401b6957257fe0b41bea1a85412b289462a3b65`.
  The generated Commit field names the base revision; this record explicitly
  identifies its uncommitted working-tree provenance. No release was published.
- `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`:
  exit 0, no vulnerable packages reported across all four projects. The initial
  sandbox invocation was denied SDK access; the authorized invocation passed.
  UiTests uses the same pinned Microsoft.Data.Sqlite 10.0.11 solely for isolated
  failure preparation, not normal entry creation or UnitTests.

### Real desktop results

The runner temporarily set TIMESHEET_ASSISTANT_TEST_APP to the absolute EXE above
and TIMESHEET_ASSISTANT_DATA_DIRECTORY to deliberately-invalid-inherited-override.
Each launched child received its own scenario directory instead. Both caller
variables were restored in finally. Native diagnostic capture was not enabled.

- Initial `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=s01-phase4-111402.trx'`:
  9 executed, 8 passed, 1 failed, 0 skipped. The failed-update assertion used
  Playwright's default five-second expectation, racing the provider's five-second
  busy wait. Corrected only this expectation to the existing 15-second UI budget;
  retained all failure/data/input assertions.
- Corrected `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-restore --logger 'trx;LogFileName=s01-phase4-111402-retry1.trx'`:
  exit 0; TRX total/executed/passed 9, failed/skipped 0; duration 1m24s.
  TRX: `artifacts/desktop-ui/s01-phase4-111402-retry1.trx`.
  Recorded run interval: 2026-11-14 13:26:02–13:27:28 Europe/Warsaw.
- Inspected all 18 launch logs created in that interval: every log contains both
  cleanup completed and scenario cleanup completed. Each logged profile and
  domain path was absent after completion. Example session logs:
  `artifacts/desktop-ui/0b9031656b2e459dab2f3e4758875abc/desktop.log` and
  `artifacts/desktop-ui/ea166922cc6a41a082302bb8d76e13d1/desktop.log`.

The nine cases (including two theory rows) prove through the published EXE:

- Exact separate ticket rows, 0/60/432/433/434-minute totals, edited duration,
  delete Cancel and confirmed Delete, validation and cancelled dirty-form context
  changes. Normal work records are created only through UI.
- Three launches with fresh profiles retain exact contents, identity/order, a
  saved edit, absence of deleted entries and a separate earlier Saturday. A second
  restart preserves a further edit/delete; no reliance on the starter smoke.
- Independent domain directory starts empty while the original retains records;
  concurrent access has a visible ownership message without an editor; a later
  launch reacquires the released directory. A complete copy of the published app
  launches from a different directory and reads the same scenario data.
- Relative override fails visibly; corrupt and future-schema databases retain
  their original bytes; a temporarily unreadable DB recovers via Retry after its
  exclusive test handle is released. A blocked first-start lease creates no final
  database; releasing it and Retry permits save/restart/read. This lease probe
  exercises safe first-start recovery, not every possible mid-schema I/O fault.
- A test-owned SQLite write transaction forces an update to fail within the UI
  budget: no success status, input retained, committed row/totals unchanged;
  restart shows the old values and a subsequent normal update succeeds.

### Deliberate-break and final gates

After staging the intended paths, temporarily changed the production SQL read
from ORDER BY creation_order to ORDER BY creation_order DESC in the worktree only.
Published this mutant separately to ignored artifacts/break-check-111499. The
single targeted persistence test failed on its first post-restart expected row:
expected T-1, actual KEEP-1. TRX executed 1, failed 1. The finally block restored
SqliteWorkEntryStore.cs from staging and restored the caller EXE variable.
`git diff --exit-code -- src/TimesheetAssistant/Features/Workdays/SqliteWorkEntryStore.cs`
confirmed no mutant remained. No production source change is included in phase 4.

- Negative TRX: `artifacts/desktop-ui/s01-break-order.trx`.
- Failure screenshot/trace/log:
  `artifacts/desktop-ui/f9e0cccbfe784f299a194feda81a819c/`.
  Both mutant launch sessions completed cleanup. No intentional mutation is staged.
- Positive replay against the original correct package passed 1/1 with exit 0:
  artifacts/desktop-ui/s01-order-restored.trx. This targeted replay followed the
  negative ordering test and source restoration.
- Final restored `dotnet build TimesheetAssistant.sln -c Release --no-restore`
  passed with 0 warnings/errors, and UnitTests --no-build --no-restore passed 87/87.

- Final git diff --check and git diff --cached --check passed. Reviewed the
  staged path list: no databases, artifacts, credentials or Visual Studio state
  are included. The ZIP hash was recalculated and matches its sidecar.

### Acceptance limits

2026-11-14: The author confirmed that package 0.1.0-preview.111402 works as
expected after the requested manual checks. They explicitly confirmed that
launching from a different application location loads the database correctly and
displays the previously saved entries. This closes manual criterion 4.5 and,
together with the automated evidence above, establishes local phase 4 acceptance.
Implementation review/triage remains required before merging; the earlier phase 3
caption observation remains separately pending.

Desktop CI F-01, clean-device compatibility, the remaining slices and full MVP
acceptance are unchanged and incomplete. No push, hosted run, tag, release or
publication-setting change was performed. Delivery text now describes persistent
workday recording; the active workflow changes unit-test wording only.
