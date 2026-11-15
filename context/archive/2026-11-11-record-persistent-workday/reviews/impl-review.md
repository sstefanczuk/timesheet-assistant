<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Record Persistent Workday

- **Plan**: context/changes/record-persistent-workday/plan.md
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3, 4
- **Date**: 2026-11-14
- **Source**: fc9e5b1d3ab2a7459772f0c451edb380a26627fc; implementation commits bc75d1c, 0459e73, cad8254, e47608c and b3a3a30, including the accepted style/test refactor 01eae88.
- **Verdict**: APPROVED
- **Findings**: 0 critical, 1 warning, 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | PASS |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Desktop validation scenario intermittently loses the entered title

- **Severity**: WARNING
- **Impact**: MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Success Criteria
- **Location**: src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayScreen.cs:28; src/TimesheetAssistant.UiTests/Features/Workdays/WorkdayRecordingTests.cs:91
- **Detail**: The fresh full desktop suite executed nine tests against package 0.1.0-preview.111403 from the reviewed commit. Eight passed; Validation_and_cancelled_discard_preserve_draft_date_and_saved_entries failed during its first valid AddAsync, before the intentional validation checks. Trace records FillAsync for Item title with value Saved entry, followed by description/duration input and Save. The resulting form has an empty title, an Enter an item title field error, no saved row and total 0m. The assertion waiting for Entry saved fails. A targeted replay of the same test against the same package passed without source or assertion changes. The suite is therefore intermittent; the replay does not erase the failed full-run evidence. No committed-record loss was observed. Whether the missing input originates in test interaction synchronization or production rendering/focus behavior remains unproven.
- **Fix**: Diagnose the fill-to-render ordering using the retained trace, establish an observable settled editor/date state before input, and verify that entered field values survive rendering before Save. Fix production state/focus sequencing if the same loss is reproducible through normal interaction. Then rerun the unchanged acceptance assertions and the full suite; do not hide the failure with action retries or fixed sleeps.
  - Strength: Addresses the observed lost field and preserves the real workflow assertions rather than treating one successful replay as acceptance.
  - Tradeoff: Requires distinguishing a test timing issue from a product input issue before selecting the code change.
  - Confidence: HIGH for the failure and intermittency; MEDIUM for a rendering/synchronization mechanism because no exact event ordering cause has been isolated.
  - Blind spot: Normal-speed human reproduction and the precise Blazor event/render sequence have not been established.
- **Decision**: FIXED — user approved the fix on 2026-11-14. Date selection now waits for the rendered snapshot date and idle state; Save/Edit wait for their requested editor/focus state and field values are asserted before saving. Added two real-desktop regressions, including a controlled held-date event and rapid input with restart. The held-event regression passes with the fix and fails when only the snapshot-date wait is removed. Final full run passed 11/11 on working-tree package 111405, with all 21 launch/scenario cleanups confirmed. The exact event sequence of the original missing-title occurrence remains unproven; the reproduced readiness defect is fixed and the original scenario passes without action retries or weakened assertions. See verification.md for intermediate failures and final evidence.

### F2 — Table caption uses the system language for its date

- **Severity**: OBSERVATION
- **Impact**: LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: src/TimesheetAssistant/Features/Workdays/WorkdayPage.razor:93
- **Detail**: At the reviewed revision, the app-authored caption used _date.ToString("D") without an explicit culture. The plan and src/AGENTS.md require English UI. Formatting under pl-PL produces Polish weekday/month names inside the English caption; the culture-specific formatting was confirmed locally with .NET. This repeats phase 3 review F1. Native date-picker regional display is separate from this app-authored caption.
- **Fix**: Supply an explicit English culture when formatting this caption, leaving ISO persistence and native date input unchanged.
- **Decision**: FIXED — user approved explicit en-US formatting on 2026-11-14. The caption now passes CultureInfo.GetCultureInfo("en-US") to ToString. Release build passed with 0 warnings/errors, 87 pure tests passed, and the exact date expression returned Saturday, November 14, 2026 under a pl-PL current culture. The matching phase 3 finding is also marked fixed. No storage or native date-picker behavior changed.

## Scope and method

Two parallel review agents were started for plan adherence and safety/quality.
Both returned immediately because their sessions exposed no filesystem or shell
tools. Neither supplied source findings. The parent performed both review scopes
directly; this report does not claim independent parallel source inspection.
No lessons.md exists. The review changes only this report and change.md status;
it does not alter production code, tests, plan checkboxes, commits or archives.

The source range was identified from change-specific commits, rather than all
recent repository activity. The lesson-only root instruction update at 5622cbd
was excluded as unrelated. Feature organization, Shouldly/readability and style
changes match explicit recorded user decisions; they are not unapproved scope.

| Phase | Plan versus source | Assessment |
|-------|--------------------|------------|
| 1 | Portable Core contracts/model, accepted duration grammar, validation, date source, checked wide totals, fake-store service tests and project references | MATCH |
| 2 | Pinned provider/schema v1, transactional parameterized mutations, stable creation order, temporary initialization before publication, canonical directory, lifetime lease and visible Retry screen | MATCH |
| 3 | Root workday editor, service-backed CRUD, totals, dirty/delete confirmations, busy/context guards, encoded text and removed starter navigation | MATCH after approved F2 fix |
| 4 | Scenario-owned domain lifetime, independent fresh profiles, real UI CRUD/restarts, alternate-folder/exclusion/error probes and truthful delivery documentation | MATCH in implemented scope; current execution fails intermittently as F1 |

Additional checks:

- Core and UnitTests have no MAUI/native-storage dependency. SQLite failure
  preparation belongs only to UiTests. No IntegrationTests project was added.
- The app retains the Windows-only TargetFrameworks property. App registration
  gates feature creation on successful storage initialization; shutdown releases
  the lease. SQL uses bound parameters and transactions; read/update/delete
  partition by date and identity. Edits retain persisted creation order.
- Existing unsupported/corrupt files are inspected without destructive reset.
  A new store is initialized at a unique temporary path before a non-overwriting
  move to the final path. Test-induced failures use scenario-owned data only.
- Razor renders entry values as text. Save success and editor reset follow the
  service's committed result; failed reads retain the prior context. Busy guards
  and captured context protect late mutation results.
- Scenario teardown validates owned GUID roots and rejects redirected
  directories before deletion. All fixture launches belong to private process
  jobs; the changed fixture retains child-only CDP/profile/data settings and
  existing failure diagnostics. No normal user store is used by these tests.
- The first-start failure test blocks lease acquisition before database
  initialization; it proves recovery from that condition, not every failure
  inside temporary-schema initialization. verification.md states that limit.
- The active workflow differs only in unit-step name and explanatory wording;
  build/package/release behavior and deferred F-01 remain unchanged. No new
  product features outside S-01, hosted diagnostic runs, pushes or releases
  were introduced by this review.

## Verification executed during this review

Environment: Windows x64 developer workstation, .NET SDK 10.0.403, existing MAUI
Windows workload and WebView2 Evergreen installation.

- dotnet build TimesheetAssistant.sln -c Release: PASS, exit 0, 0 warnings/errors.
- dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj -c Release --no-restore:
  PASS, exit 0, 87 executed/passed, 0 failed/skipped; completed before packaging.
- dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json:
  PASS, exit 0, no vulnerable packages reported for any of the four projects.
- ./scripts/Package-Windows.ps1 -Version 0.1.0-preview.111403 -Commit fc9e5b1d3ab2a7459772f0c451edb380a26627fc -BuildNumber 1114:
  PASS. Fresh package created while the working tree was clean. Publication:
  artifacts/publish-0.1.0-preview.111403/TimesheetAssistant.exe.
  ZIP SHA-256: 78bbeebfd00a113b63faf5af4f5a58a7b93fb5a666d0cc007e498f75b9ae4749.
- With TIMESHEET_ASSISTANT_TEST_APP temporarily pointing to that absolute EXE,
  dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=s01-full-review-111403.trx':
  FAIL, exit 1, total/executed 9, passed 8, failed 1, skipped 0, duration 1m23s.
  TRX: artifacts/desktop-ui/s01-full-review-111403.trx.
- Targeted replay with the same command plus --filter FullyQualifiedName~Validation_and_cancelled_discard_preserve_draft_date_and_saved_entries
  and logger file s01-review-validation-replay.trx: PASS, exit 0, 1 executed/passed,
  0 failed/skipped. No source edits or assertion changes between these runs.
  The caller EXE variable was restored in finally after each run.
- Failure screenshot, trace and stage/cleanup log:
  artifacts/desktop-ui/e8c9bb55f60e490c924c6620ffd4b186/.
  Trace inspection confirmed the title fill call and the empty invalid field.
  All 18 launch logs from the full run contain cleanup completed and scenario
  cleanup completed. Original failure evidence remains available.
- git diff --check: PASS before report generation; no tracked data, generated
  packages, credentials or Visual Studio state were introduced.

Manual criteria 3.2 and 4.5 remain checked with explicit author confirmations in
verification.md and the conversation, associated with packages 111303 and
111402 respectively. The author specifically confirmed loading saved records
after launching from another folder. This review does not invent new manual
confirmation for package 111403 or unset historical plan progress. The new failed
full-run result must be resolved before treating current automated acceptance as
reliable. Earlier successful local results remain historical evidence.

Desktop CI F-01, clean-device compatibility and full MVP acceptance remain
unestablished. No production/test fixes or Git commits were made during the
initial review. The subsequent approved F1 triage changes are recorded below.

## F1 triage verification — 2026-11-14

The initial review verdict was NEEDS ATTENTION because of the 8/9 desktop result.
After the author's explicit approval, the readiness defect was reproduced with
a held browser date-change event and corrected in the worktree. The current
APPROVED verdict reflects the final green suite; F2 was resolved separately below.
The original failed review run and its trace remain historical evidence above.

- Production change: existing Daily totals region exposes the actual snapshot
  date via data-work-date and its aria-busy state. No domain/store behavior or
  visible product text changes. The caption observation was not included.
- Test changes: synchronize date selection with that loaded snapshot, assert
  filled fields, await post-save clearing/focus and post-edit focus; add
  WorkdayInputTests with controlled readiness and twelve-record restart checks.
- Solution Release build: 0 warnings/errors. Pure UnitTests: 87/87 passed.
- Corrected working-tree package: artifacts/publish-0.1.0-preview.111405,
  based on fc9e5b1. ZIP SHA-256:
  ecb337af38403fa8aa060f1dc3f62cc68b8abb9fd16618babdfe2b8648abefb9.
- Targeted regression: PASS 1/1 in s01-date-readiness-fixed.trx. Controlled
  removal of the helper snapshot-date wait: expected FAIL 1/1 in
  s01-date-readiness-mutant.trx; exact helper bytes restored in finally.
- Full desktop run: PASS, runner exit 0, total/executed/passed 11,
  failed/skipped 0, duration 1m42s. TRX:
  artifacts/desktop-ui/s01-f1-final-full.trx.
- All 21 launch logs confirm launch and scenario cleanup; post-run checks found
  no remaining logged profile/domain paths.
- Intermediate diagnostic/test-setup and local packaging failures are recorded
  in verification.md rather than omitted. No fault endpoint, action retry,
  fixed UI sleep, assertion weakening, push or commit was introduced.

## F2 triage verification — 2026-11-14

The author approved the proposed explicit English culture. Updated only the
caption's date expression to ToString("D", CultureInfo.GetCultureInfo("en-US")).
Release build passed with 0 warnings/errors and UnitTests passed 87/87. With the
current culture temporarily set to pl-PL, the same DateOnly expression returned
Saturday, November 14, 2026. The caller culture was restored in finally.
git diff --check passed. No new test or package was needed for this formatting
change. The 11/11 desktop and 3/3 replay evidence above applies to the F1 package
before this caption-only change; no new desktop execution is claimed for F2.

Triage complete: F1 and F2 are FIXED in the working tree; no pending findings
remain in this full report or its matching phase 3 caption finding. All current
dimension verdicts are PASS and the overall verdict is APPROVED. No commit or
push was made as part of this triage.
