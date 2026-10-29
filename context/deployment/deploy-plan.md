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
   `dotnet test src/TimesheetAssistant.Tests/TimesheetAssistant.Tests.csproj`, and
   `dotnet list TimesheetAssistant.sln package --vulnerable --include-transitive --source https://api.nuget.org/v3/index.json`.
   Record blocked checks independently. The current empty tests prove discovery
   only, not functionality. Report audit findings instead of hiding them.
5. Run the packaging script with the exact source commit, preview version and
   build number; locally use an ignored output directory unique to that attempt.
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
