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
  and reported window title TimesheetAssistant. Visible UI behavior, restart and
  a second extraction still need user confirmation; no clean-machine check occurred.
- GitHub remote had no refs when inspected. Typical private-key/token signatures
  were not found in tracked history; this is not an exhaustive secret audit.
- GitHub CLI portable 2.103.3 is available under ignored artifacts/tools/gh/bin.
  CLI authentication is pending; no token was requested or stored in source.
  The attempted system MSI installation did not report completion during checks.

Pending: authenticated GitHub access, remote CI/draft verification, user UI check,
first publication and public unauthenticated checksum check. No published release
or completed functional test is claimed. AUTO_PUBLISH_PRERELEASES remains disabled.
