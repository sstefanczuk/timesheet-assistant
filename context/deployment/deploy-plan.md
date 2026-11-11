# First Windows technical prerelease

## Approved scope

The user approved GitHub Actions + Releases in the public repository
https://github.com/sstefanczuk/timesheet-assistant, Windows 11 x64, an unsigned ZIP,
and a technical starter preview rather than the functional MVP. The clean-Windows
requirement was replaced, with user agreement, by testing on the existing developer
computer. Windows Sandbox was not available to launch. Clean-device compatibility
remains unverified and must not be claimed in release notes.

No cloud runtime, cloud data store, signing credentials, updater or persistence
implementation is part of this deployment. Existing archives remain unchanged.

## Implementation

- Pin SDK 10.0.402 and workload set 10.0.402.1 in global.json.
- Use a repository NuGet.Config with nuget.org only, independent of private feeds
  configured on the developer computer.
- Retain the Windows-only TargetFrameworks and unpackaged deployment. Map the
  optional RuntimeIdentifierOverride to RuntimeIdentifier for Windows.
- Publish the application project with portable win-x64, SelfContained=true and
  WindowsAppSDKSelfContained=true. WebView2 Evergreen remains a prerequisite.
- Package the whole publish folder using scripts/Package-Windows.ps1. Include
  README.txt, an external SHA-256 file and release notes with the exact commit.
- PRs build and package with read-only permissions. Main builds create a separate
  release job with contents:write and the built artifact from the same run.
- Standard windows-2025 runners; action references pinned to full commit SHAs;
  three-day artifact retention; cancel outdated PR runs, serialize main runs.
- Version v0.1.0-preview.<workflow run number>. Never overwrite an existing tag,
  ZIP or release. Release names identify the runnable ZIP separately from sources.
- AUTO_PUBLISH_PRERELEASES is initially absent/false, so first runs create drafts.
  After the first launch check and publication, set it to true for automatic
  prereleases after successful main builds. Do not mark previews as Latest.

## Execution and human gates

1. Review tracked files and history for secrets/private data before pushing to the
   public remote. Do not include the unrelated untracked .agents directory.
2. Install GitHub CLI if absent. The owner authenticates with `gh auth login`;
   never paste tokens into chat or commit them. Check `gh auth status` and
   `gh repo view sstefanczuk/timesheet-assistant --json visibility,defaultBranchRef`.
3. Configure origin as https://github.com/sstefanczuk/timesheet-assistant.git.
   Inspect remote history before pushing; never force-push unrelated history.
4. Run `dotnet build TimesheetAssistant.sln`,
   `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj`, and
   `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`.
   Record blocked checks independently. The empty pure-logic suite proves discovery
   only; run the desktop suite locally after packaging while its CI gate is
   deferred. Report audit findings instead of hiding them.
5. Run the packaging script with the exact source commit, preview version and
   build number; locally use an ignored output directory unique to that attempt.
   Set `TIMESHEET_ASSISTANT_TEST_APP` to the absolute published
   `artifacts/publish-<version>/TimesheetAssistant.exe` path (or the corresponding
   local output path), then run
   `dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj --logger trx`.
   Windows x64 and WebView2 Evergreen Runtime are required. Full local publication
   commands are in `src/TimesheetAssistant.UiTests/README.md`.
6. Commit only reviewed deployment changes, push main and inspect the Actions run
   with `gh run list` and `gh run view <run-id> --log-failed`.
7. Download the draft's uploaded ZIP with
   `gh release download <tag> --pattern "TimesheetAssistant-*-win-x64.zip*" --dir artifacts/draft-check`.
   Verify its SHA-256, extract to a separate folder, launch, check Home, Counter,
   Weather, close/reopen, then check a second extraction directory. The user
   confirms visible UI behavior on the existing developer machine. Record any
   SmartScreen/device-policy restrictions; do not claim a clean-machine test.
8. On a successful launch check, add its result and environment to release notes,
   then publish with `gh release edit <tag> --draft=false --prerelease --latest=false`.
   Download publicly without authentication and compare the ZIP hash again.
9. Enable steady-state publishing with
   `gh variable set AUTO_PUBLISH_PRERELEASES --body true --repo sstefanczuk/timesheet-assistant`.
   This occurs only after the first successful publication, not during setup.

## Recovery and completion

Build/package failures do not publish. Failed manual checks leave the first draft
unpublished. Re-running a release never overwrites an existing tag; inspect partial
state manually and use a new run number when rebuilding. Preserve previous
releases. Once a previous version exists, download its ZIP into a separate folder;
binary rollback does not reverse any future local-data migrations.

Complete when the draft's ZIP has passed the developer-machine launch check, the
prerelease is publicly downloadable with the expected SHA-256, and results are
recorded here. No first-release rollback target or persistence test exists yet.

## Execution results

Verified locally on 2026-10-29:

- Release build passed: zero warnings and errors, after adding the repository
  NuGet.Config. The first sandboxed attempt could not read the Windows SDK; an
  unrestricted retry exposed an unrelated global private feed (401). Neither
  failure was hidden; the repository-level public source resolved the latter.
- xUnit discovery completed, with no tests found. No functional coverage claimed.
- Dependency audit against nuget.org found no vulnerable direct/transitive packages.
- PowerShell packaging script parsed successfully; actionlint accepted the workflow.
- Local ZIP built from base commit 0c79052 plus the deployment working-tree changes:
  `artifacts/local-check/release-0.1.0-preview.1/TimesheetAssistant-0.1.0-preview.1-win-x64.zip`.
  This is a local packaging check, not a release from a committed CI revision.
  Size: 112187469 bytes. SHA-256:
  `7703d8763f0520da3a4be164a83da3bf55ffa3171e14ce0965be25c53c06f762`.
- ZIP hash matched; extraction contained the EXE, coreclr.dll, Microsoft.UI.Xaml.dll,
  runtime configuration with included .NET 10.0.13, README and wwwroot/index.html.
- Extracted application started on the developer desktop; process remained alive
  and reported window title TimesheetAssistant. The user confirmed Home, Counter,
  Weather and close/reopen behavior with "działa". No clean-machine check occurred.
- GitHub remote had no refs when inspected. Typical private-key/token signatures
  were not found in tracked history; this is not an exhaustive secret audit.
- GitHub CLI 2.103.3 is installed under Program Files and is also available under
  ignored artifacts/tools/gh/bin. The user completed browser authentication as
  sstefanczuk; credentials remain in the system keyring, not source.
- Origin is configured; deployment commit 8d1d7c5 was pushed to public main.
  First Actions run: https://github.com/sstefanczuk/timesheet-assistant/actions/runs/40923578794.
- The first CI run failed before packaging with NETSDK1112: the separate restore
  used default configuration while build used Release. Restore and build now both
  explicitly select Release and RuntimeIdentifierOverride=win-x64. No draft or
  public release was created by that failed run. Checkout was updated to Node 24.

First deployment is complete. The preview is publicly available and its downloaded
checksum was verified. AUTO_PUBLISH_PRERELEASES is now true. No automated CI UI
test or clean-machine compatibility is claimed.

### First remote package

- Corrected run https://github.com/sstefanczuk/timesheet-assistant/actions/runs/40924554238
  passed build, xUnit discovery, dependency audit, packaging, artifact upload and
  draft creation. Build job: 5m51s; release job: 37s.
- Draft tag: v0.1.0-preview.2. Exact built/tagged commit:
  `614a5a2823c12f1205b7fbd86bad859ecc3c455e`.
- Application ZIP: 112187777 bytes; verified downloaded SHA-256:
  `119381a6579a9f4079158e82476c6e016cc42fd02756842c5a0c43954d36cdb2`.
  Checksum matches the sidecar and GitHub asset digest. The ZIP was extracted to
  `artifacts/ci-extracted` and started with window title TimesheetAssistant.
- Initial desktop inspection reported `Computer Use app approval timed out`.
  Recovery through the Computer Use launch/activation APIs succeeded. The exact
  downloaded CI package was verified on the developer workstation: Home rendered,
  navigation opened Counter, Click me changed the visible count from 0 to 1,
  Weather loaded five table rows, normal close removed the window, and reopening
  displayed Home again. The user had independently confirmed the local ZIP.
- Non-blocking CI annotations: upload-artifact v4/download-artifact v4 still
  declare Node 20 and ran under GitHub's forced Node 24 compatibility behavior.
  A future action-version update should verify artifact upload/download together.

### Publication and completion

- Published prerelease (not Latest):
  https://github.com/sstefanczuk/timesheet-assistant/releases/tag/v0.1.0-preview.2.
- Release notes record the actual developer-machine checks and explicitly exclude
  clean-machine/CI UI verification. No binary or checksum was replaced during
  publication; the draft was promoted in place.
- Downloaded the public ZIP using Invoke-WebRequest without authentication.
  SHA-256 matched `119381a6579a9f4079158e82476c6e016cc42fd02756842c5a0c43954d36cdb2`.
- Enabled repository variable AUTO_PUBLISH_PRERELEASES=true only after the public
  download check. Future successful main builds publish previews automatically;
  PRs retain read-only build/package behavior. No additional secrets were added.
- The final documentation-only result commit uses [skip ci] to avoid creating a
  redundant preview for the execution log. Application/workflow code is exactly
  the code verified by the successful CI run and tagged for this release.
- Remaining limits: unsigned starter, WebView2 required, no persistence, empty
  xUnit suite, no clean-device verification, and artifact-action deprecation
  annotations noted above. These do not block the approved technical prerelease.

## Desktop UI gate candidate: F-01 phase 3

Status as of 2026-11-11: hosted desktop CI work is deferred. The exact workflow
candidate is preserved in local branch `wip/f-01-desktop-ui-gate-20261111`, commit
`8980a91`. Main retains its existing build/package/release workflow without this
gate. The workflow behavior below describes the candidate used in experiments;
it is not active on main. A green main build does not establish desktop CI
acceptance. Local desktop verification remains required.

Current implementation as of 2026-11-08 supersedes the historical empty-test
description above. UnitTests remains empty and reserved for pure logic. UiTests
now launches the real unpackaged Release EXE, connects Playwright .NET to its
WebView2, and exercises Home → Counter → count 0 to 1. The author confirmed the
local visible flow and ordinary application/independent WebView2 behavior in
phase 2. Counter is temporary and will be replaced in S-01 with work-entry tests,
isolated domain storage, and explicit restart/read checks. The current test does
not verify persistence or clean-device compatibility.

The candidate `Windows preview` build job retains restore, Release build,
dependency audit and packaging. It runs UnitTests before packaging, verifies a WebView2 Evergreen
runtime executable exists in the runner's machine/user installation directories,
then sets `TIMESHEET_ASSISTANT_TEST_APP` to
`artifacts/publish-<version>/TimesheetAssistant.exe` and runs:

```powershell
dotnet test src/TimesheetAssistant.UiTests/TimesheetAssistant.UiTests.csproj -c Release --no-build --logger 'trx;LogFileName=desktop-ui.trx' --results-directory artifacts/desktop-ui
```

A missing runtime gives a clear failure, not a skip. The gate requires both a
successful runner exit (including fixture teardown) and a completed TRX with at
least one executed test, all passing and zero failures. Only then are release
inputs uploaded. The release job still requires successful completion of build;
a desktop failure must prevent release. Debugging/profiles are configured only
for the launched test process, without changing the normal application/package.

`desktop-ui-diagnostics` is a separate artifact with three-day retention and an
`always()` upload. Its `artifacts/desktop-ui/` contents include `runtime.log`,
`desktop-ui.trx` when produced, and per-session startup/PID/stage/endpoint/cleanup
logs. Screenshots and traces are retained for failures when a connected page is
available; pre-connection failures cannot supply them. Earlier build failures
may leave no desktop diagnostics. The existing `windows-preview` artifact remains
the release input and also retains its three-day lifetime.

CI acceptance is **pending**. Initial runner attempts on 2026-11-08 failed:

- [Normal attempt](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42459710244)
  and [negative attempt](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42459712349)
  both passed build, packaging and runtime detection, then timed out waiting for
  CDP within 60 seconds. Both retained TRX and startup/runtime logs and completed
  cleanup. No page was connected, so no screenshot/trace was available. The
  negative attempt skipped an eligible inert release sentinel with `needs: build`,
  but never reached the deliberately wrong assertion and does not prove that
  acceptance criterion. Neither attempt published a release.

- A minimal [elevation probe](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42460440845)
  confirmed the hosted runner is elevated. WebView2 ignores user-scoped
  environment overrides in elevated hosts. The fixture now launches only the
  application with a verified non-elevated medium-integrity token, preserving
  suspended creation, job ownership and child-private settings. An initial
  `CreateProcessWithTokenW` attempt failed with Access denied on the runner's
  restricted-token path. A narrow console probe confirmed the EXE is readable
  under that token; the corrected path uses `CreateProcessAsUserW` for the
  caller's own restricted token. Linked filtered tokens retain
  `CreateProcessWithTokenW`. No machine registry, policies or ACLs were changed.

Final full attempts after the two bounded corrections also failed:

- [Normal run](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42461643870)
  found a CDP listener owned by the test application's job, then Playwright failed
  the connection with `ECONNREFUSED`.
- [Negative run](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42461646251)
  found an owned listener, then failed with `ECONNRESET`. It never reached the
  deliberately wrong Counter assertion. Its eligible inert release sentinel was
  skipped because build failed.
- Both retained TRX, runtime and session logs, completed cleanup, skipped release
  inputs and release, and published nothing. No page connection means no page
  screenshot/trace was available. The separate diagnostic artifact retained its
  three-day expiry; downloaded copies are under ignored `artifacts/phase3-ci/`.
- Local Release build and the unchanged desktop interaction test passed after
  the final correction. This does not establish hosted-runner acceptance. The
  two-correction limit is exhausted; further diagnosis needs an explicit decision.

### Renewed investigation

The user authorized a fresh limit of two corrections on `windows-2025`.
Proxy-disabled, ownership-checked HTTP discovery retrieved a valid websocket URL,
but the browser listener disappeared before attachment. The normal
[discovery run](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42476450437)
and its [negative counterpart](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42476453143)
both failed before UI assertions. The local interaction test passed even with an
unreachable HTTP proxy configured only for that test subprocess.

A narrow [context probe](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478074065)
confirmed the parent and own child shared `WinSta0/Default` and the same session;
the exact standard token could write/delete markers in the test profile and
artifact directories. No desktop, ACL or machine-policy changes were needed.
Own-instance runtime logs were inconclusive. A private-job
[lifecycle probe](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478365625)
then established WebView2 exits with `0x80000003` before cleanup. It observes only
job-owned process handles, never unrelated processes or command lines.

The last correction removed the extra `DISABLE_MAX_PRIVILEGE` flag while retaining
the standard LUA token and explicit non-elevated/medium-integrity verification.
Local Release build and desktop interaction passed. The full
[current-publication run](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478901525)
still failed: owned WebView2 PIDs 2916 and 5788 exited with `0x80000003` before
cleanup, while the application remained alive. It retained TRX/runtime/lifecycle
diagnostics with three-day expiry, completed cleanup, and skipped release-input
upload and release. It never reached Home/Counter assertions. The matching
[negative run](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42478904280)
also failed: owned WebView2 PIDs 1512 and 7360 exited with `0x80000003` before
cleanup. Its eligible inert release sentinel was skipped; diagnostic upload and
cleanup succeeded, and release-input upload was skipped. Neither a passing
desktop run nor the required controlled Counter assertion failure was established.

Both renewed corrections are exhausted. The native failing module/check is not
identified; `0x80000003` alone does not establish it. Further work should diagnose
the WebView2 native failure before choosing another workaround. At the end of
that investigation the workflow remained an uncommitted candidate, not an
accepted release gate; it is now preserved at `8980a91`. No main push or release
was performed during this investigation; F-01 remains incomplete.

Required acceptance proofs remain pending:

- Success proof: pending a `windows-2025` run on the changed workflow that executes
  at least one desktop test against the published Release EXE and supplies its TRX.
- Controlled-failure proof: pending a run with the final Counter assertion
  temporarily set to 2, showing failed build, blocked release, and retained
  available TRX/log/screenshot/trace diagnostics. Restore the assertion afterward;
  never merge the deliberately failing test.

F-01 remains incomplete until both real run links and outcomes are recorded.
The successful first-deployment runs above verify the previous workflow and are
not evidence of this desktop gate. If runner execution fails after diagnosis and
reasonable corrections, record the failure and agree the next step with the
user; no switch to a self-hosted runner or browser-only substitute is approved.

### Native diagnosis handoff — 2026-11-09

The [supporting native diagnosis report](../changes/diagnose-webview2-native-crash/diagnosis.md)
records all five diagnostic run IDs/exact SHAs and stable evidence before the
three-day artifacts expire. Passive baseline
[42572422837](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42572422837)
reproduced natural own-WebView2 `0x80000003` exits without a debugger, with no
matching Windows events or own-profile Crashpad reports. Final active run
[42578053182](https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42578053182)
attempted two own PIDs and ended offline analysis with `budget-exhausted` and
`Analyses=[]`. Helpers and private files were cleaned up; raw dumps/debugger
output were not exported. The exact exhausted budget was not recorded.

The result is **inconclusive**: no fault module/offset, stack or root cause was
established, and a first-chance debugger breakpoint remains possible. The report
recommends a separately agreed change to record the exhausted budget and bounded
counters before deciding whether another run is warranted. It authorizes no
new experiment, increased limits or release. The prior correction/run approvals
are consumed.

F-01 remains `implementing`, with Progress 3.1–3.3 unchecked and both desktop CI
acceptance proofs still pending. The successful diagnostic collection/cleanup
does not accept the Windows preview gate. No persistence or clean-device proof
was added. Supporting diagnostic work is recorded in commits `29a3054` and
`49d90c5`; earlier F-01/deployment history remains unchanged.

### Deferral and preserved work — 2026-11-11

The user approved keeping the candidate off main and committing the diagnostic
history and current test documentation. The checkpoint on
`wip/f-01-desktop-ui-gate-20261111` preserves only the pending preview workflow;
the already committed test fixture and native diagnostic tooling remain on main.
F-01 and its hosted success/controlled-assertion-failure proofs remain incomplete.

This cleanup does not push commits, run CI, publish a release or change repository
publication settings. Existing main publishing behavior therefore remains in
place; any future push must account for automatic technical prereleases without
a desktop gate. Those previews must not be described as desktop-CI-verified or
as an accepted MVP. Product work continues to require local logic and real
desktop checks; S-01 must cover isolated storage and save/restart/read behavior.
