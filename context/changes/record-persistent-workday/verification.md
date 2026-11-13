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
