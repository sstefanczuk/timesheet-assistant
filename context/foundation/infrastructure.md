---
project: Timesheet Assistant
researched_at: 2026-10-26
recommended_platform: GitHub Releases + GitHub Actions
runner_up: Azure Blob Storage
context_type: mvp
tech_stack:
  language: C#
  framework: .NET MAUI Blazor Hybrid
  runtime: .NET 10 / Windows / WebView2
---

## Recommendation

**Distribute versioned Windows application ZIPs through GitHub Releases, built and tested on Windows in GitHub Actions.** The application executes on the user's Windows 11 device; no cloud application runtime, database, queue, or user-data storage is required. This preserves the decision in [tech-stack.md](tech-stack.md) and the local-only requirements in [prd.md](prd.md).

The user explicitly accepted this recommendation and its risks on 2026-10-26. Azure Blob Storage is the runner-up; Amazon S3 is the third alternative. This document records a decision, not an implemented pipeline or completed deployment.

### Interview constraints

- No persistent server connections, hosted workers, or backend.
- Cost and developer experience have equal weight; no fixed budget was supplied.
- Existing experience: Azure / AWS / GCP; no specific provider preference was given.
- One region is sufficient; the MVP serves its author only.
- Entries remain on the device; no cloud data services. Release binaries are a separate concern.

### Compatibility and scope

The project targets `net10.0-windows10.0.19041.0` through `TargetFrameworks` and already sets `WindowsPackageType=None`. Retain both choices. Build/publish needs a Windows toolchain; native execution needs Windows and WebView2. Serving `wwwroot` does not deploy the MAUI application. There is no selected local persistence engine in the foundation stack; its implementation remains separate from distribution.

The default web-platform shortlist was expanded because none is a native Windows MAUI runtime. The comparison below evaluates release-file distribution independently from execution and Windows compilation. Request-based estimates of 10k–100k monthly API calls do not describe this single-user desktop MVP; relevant usage is Windows build minutes, stored artifact size, release frequency, and downloaded bytes.

## Platform Comparison

Pass = 2, Partial = 1, Fail = 0. Weights: CLI 3, managed service 3, readable documentation 2, scriptable API 3, integration 1; maximum 24. These scores describe operational capabilities, not native-runtime compatibility. Cost/DX and existing stack resolve ties; provider familiarity is a soft preference. No global-CDN or database bonus is warranted by the interview.

| Distribution candidate | CLI-first | Managed | Readable docs | Scriptable API | MCP / CI integration | Weighted total | Fit |
|---|---|---|---|---|---|---|---|
| GitHub Releases + Actions | Pass | Pass | Pass | Pass | Pass | 24 | Selected; Windows build plus release lifecycle |
| Azure Blob Storage | Pass | Pass | Pass | Pass | Pass | 24 | Runner-up; separate Windows builder |
| Amazon S3 | Pass | Pass | Pass | Pass | Partial | 23 | Third; separate Windows builder |
| Cloudflare Workers / Pages | Pass | Pass | Pass | Pass | Pass | 24 | Excluded as native runtime; file-hosting limits |
| Vercel | Pass | Pass | Pass | Pass | Partial | 23 | Excluded as native runtime; extra web platform |
| Netlify | Pass | Pass | Pass | Pass | Pass | 24 | Excluded as native runtime; optional download site |
| Fly.io | Pass | Partial | Pass | Pass | Partial | 20 | Excluded as native runtime; running file server unnecessary |
| Railway | Pass | Partial | Pass | Pass | Pass | 21 | Excluded as native runtime; running file server unnecessary |
| Render | Pass | Pass | Pass | Pass | Pass | 24 | Excluded as native runtime; static distribution possible |

GitHub provides `gh`, documented REST APIs, readable documentation, managed release assets, Windows-hosted Actions runners, and first-party CI integration. It combines version tags, release notes and binaries without a second cloud account. Public standard-runner usage is free; private repositories have plan-dependent allowances and overage costs. Do not assume free Windows builds or confuse temporary Actions artifact storage with release assets. Release assets must each be below 2 GiB; GitHub documents no total release-size or bandwidth quota. Repository readers can access releases. [Releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases), [Actions billing](https://docs.github.com/en/billing/concepts/product-billing/github-actions), [CLI](https://cli.github.com/manual/gh_release_create).

Azure Blob Storage provides managed binary storage, CLI upload/download/list operations, documented APIs and Microsoft Learn documentation. Official Azure MCP storage tools are available; Azure MCP Server 2.0 is labeled generally available in its repository (checked 2026-10-26). Private containers support Entra-authenticated access or expiring user-delegation SAS links. SAS links are bearer credentials, not permanent release URLs. Storage adds subscription, RBAC and release-metadata work; it supplies neither Windows builds nor an updater. Pricing depends on region, redundancy, tier, storage, transactions and egress; no reliable fixed numeric quote was returned by the pricing page, so none is assumed. [CLI](https://learn.microsoft.com/en-us/cli/azure/storage/blob?view=azure-cli-latest), [Authorization](https://learn.microsoft.com/en-us/azure/storage/blobs/authorize-data-operations-cli), [MCP](https://github.com/microsoft/mcp/blob/main/servers/Azure.Mcp.Server/README.md), [Pricing](https://azure.microsoft.com/en-us/pricing/details/storage/blobs/).

Amazon S3 provides managed object storage and documented CLI/API operations for versioned release keys and authenticated downloads. New buckets block public access by default; an owner can download through authenticated CLI or issue a short-lived presigned URL without making the bucket public. It requires IAM, billing and independent release notes/version conventions. Integration is scored Partial because this research verified CLI/API but did not establish a release-specific first-party agent integration; it is not a claim that AWS has no MCP. Cost depends on region, stored bytes, requests and transfer, and requires a measured package/download estimate. [CLI upload](https://docs.aws.amazon.com/cli/latest/reference/s3/cp.html), [Presigned URLs](https://docs.aws.amazon.com/cli/latest/reference/s3/presign.html), [Access](https://docs.aws.amazon.com/AmazonS3/latest/userguide/granting-public-access.html), [Pricing](https://aws.amazon.com/s3/pricing/).

Cloudflare Workers/Pages has strong CLI, managed delivery, readable docs, REST APIs and official MCP. Its runtime does not execute Windows MAUI. Pages and Workers Static Assets limit individual files to 25 MiB; a larger published ZIP would require another service such as R2. Static requests can be free, but this does not remove file-size limits. Pages deployment and Workers deployment use different commands. Official MCP exists; its consulted current catalog did not explicitly declare a lifecycle status, so GA is not inferred. [Languages](https://developers.cloudflare.com/workers/languages/), [Pages limits](https://developers.cloudflare.com/pages/platform/limits/), [Static billing](https://developers.cloudflare.com/workers/static-assets/billing-and-limitations/), [MCP](https://developers.cloudflare.com/agents/model-context-protocol/cloudflare/servers-for-cloudflare/).

Vercel offers managed static hosting, a scriptable CLI/API and readable docs, but its function runtimes do not run native Windows MAUI. CLI source-upload limits are aggregate source limits (100 MB Hobby / 1 GB Pro), not a verified per-ZIP limit. Hobby eligibility is personal/non-commercial; eligibility of a work-related tool must not be assumed. Its official MCP is **beta**, checked 2026-10-26, hence Partial integration credit. [Runtimes](https://vercel.com/docs/functions/runtimes), [Limits](https://vercel.com/docs/limits), [Pricing](https://vercel.com/pricing), [MCP beta](https://vercel.com/docs/ai-tooling/vercel-mcp).

Netlify offers atomic static deployments, CLI draft/production operations, readable docs, APIs and official MCP. It can distribute files, not execute native MAUI. Credit-based quotas charge for production deployments and transferred bytes; large desktop downloads must be estimated rather than called unlimited free hosting. Single-owner private access is possible on credit Free/Personal; shared password protection requires Pro. Official MCP documentation did not explicitly declare GA/beta status. [CLI](https://cli.netlify.com/commands/deploy/), [Credits](https://docs.netlify.com/manage/accounts-and-billing/billing/billing-for-credit-based-plans/how-credits-work/), [Private access](https://docs.netlify.com/manage/security/secure-access-to-sites/password-protection/), [Agent setup](https://docs.netlify.com/build/build-with-ai/agent-setup-guides/agent-setup-overview/).

Fly.io Machines runs Linux workloads. Download hosting would require a separate server such as nginx, creating compute and maintenance needs absent from the MVP. CLI and REST/OpenAPI are documented. Its official MCP is **beta**, checked 2026-10-26; it does not currently replace CLI deployment/app creation. [Runtime](https://docs.fly.io/machines/overview), [Static server](https://fly.io/docs/languages-and-frameworks/static/), [API](https://machines-api-spec.fly.dev/), [MCP beta](https://fly.io/mcp/).

Railway requires Linux-compatible container images, excluding this native runtime. A Caddy container can serve binaries, but adds a running service; managed-operation credit is Partial for this use case. CLI, public API and official hosted MCP are documented; hosted MCP via CLI needs 5.44.0 or newer. No explicit lifecycle label was established for MCP. [Runtime constraint](https://railway.com/changelog/2026-01-09-railway-agent-skill), [Static server](https://railway.com/deploy/caddy), [CLI](https://docs.railway.com/cli), [API](https://docs.railway.com/integrations/api), [MCP](https://docs.railway.com/ai/mcp-server).

Render requires Linux/amd64 container images, excluding native Windows execution. Managed static sites could distribute prebuilt downloads, but provide no Windows builder and remain subject to bandwidth/pipeline limits. CLI noninteractive operations, backward-compatible API and official MCP are documented; no explicit MCP lifecycle label was established. [Runtime](https://render.com/docs/deploying-an-image), [Static sites](https://render.com/docs/static-sites), [CLI](https://render.com/docs/cli), [API](https://render.com/docs/api), [MCP](https://render.com/docs/mcp-server).

### Shortlisted Platforms

#### 1. GitHub Releases + GitHub Actions (Recommended)

Best fit because it preserves the foundation stack and combines Windows builds with versioned binary distribution. Equal cost/DX weighting favors avoiding a second artifact-storage service. Repository visibility and account allowances must be checked during deployment planning.

#### 2. Azure Blob Storage

Credible alternative when independent private binary distribution and Azure RBAC are more useful than integrated release notes. Existing hyperscaler experience lowers learning cost, but does not remove storage-account and permission overhead. Keep a separate Windows build pipeline.

#### 3. Amazon S3

Credible alternative for private object distribution using versioned keys and authenticated downloads. Its core operation scores well, but no requirement justifies adding IAM, storage billing and release metadata management to the existing GitHub workflow. Keep a separate Windows builder.

## Anti-Bias Cross-Check: GitHub Releases + GitHub Actions

### Devil's Advocate — Weaknesses

1. A successful build on an SDK-equipped machine can hide missing runtime dependencies in the distributed ZIP.
2. Releases does not install updates, roll back local schema changes, or restore user entries.
3. Private release access follows repository access; a future audience may require a different distribution boundary.
4. Repeated Windows builds and retained Actions artifacts can exhaust private-plan allowances. Download hosting and CI have different cost models.

### Pre-Mortem — How This Could Fail

The first ZIP built successfully and ran on the author's development machine. The team treated that result as deployment verification and skipped a clean Windows test. After a laptop replacement, the application failed because a runtime prerequisite was missing. The release notes never identified WebView2 requirements, and users could not tell which download contained the compiled application rather than source code.

Automatic publishing continued after merges while the test project still contained no meaningful functional tests. A later change migrated local storage without first creating a recoverable backup. When a defect damaged the time entries, the author downloaded an older release, only to discover that its code could not read the new data format. Keeping old binaries had provided no tested data recovery path.

Meanwhile, frequent Windows rebuilds and accumulated temporary artifacts consumed the private repository's allowance. A needed repair release stalled at the worst moment. GitHub's download service remained available throughout; the failure came from treating artifact publication as proof of installation, update safety, and recovery. The project had automated distribution before proving the desktop lifecycle.

### Unknown Unknowns

- .NET 10 uses portable `win-x64` / `win-x86` RIDs; older `win10-*` examples are invalid. The MAUI publish documentation also describes a `RuntimeIdentifierOverride` workaround, which is absent from the current project.
- .NET runtime inclusion, Windows App SDK self-contained deployment and WebView2 availability are separate verification concerns. A ZIP that works on the development PC proves none of them for a clean device.
- GitHub's automatically generated source ZIP is different from the explicitly uploaded application ZIP.
- Selecting an old binary does not reverse local data migration; data compatibility must be tested independently.

[Version-specific MAUI publish guidance](https://learn.microsoft.com/en-us/dotnet/maui/windows/deployment/publish-unpackaged-cli?view=net-maui-10.0), [MAUI Windows deployment / WebView requirement](https://learn.microsoft.com/en-us/dotnet/maui/windows/deployment/overview), [Release contents](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases).

## Operational Story

These are intended operations; no workflow, hosting resource, release or secret was configured by this research.

- **Preview deploys**: PR Windows builds produce a temporary ZIP artifact for manual download and launch in an isolated Windows profile. There is no preview URL for the native application. Do not give fork PR code release-write permissions or signing secrets. Establish artifact retention and access rules in the deployment plan.
- **Secrets**: Prefer the per-job, repository-scoped `GITHUB_TOKEN`; only the release job needs `contents: write`. Other jobs remain read-only. Human-managed signing credentials, if later needed, belong in restricted secrets/certificate storage, never source files or logs. Rotation is performed by the credential owner; PR jobs cannot receive those credentials. [Token model](https://docs.github.com/en/actions/concepts/security/github_token).
- **Rollback**: Download the previous uploaded ZIP with `gh release download <previous-tag> --pattern "TimesheetAssistant-*-win-x64.zip" --dir <separate-folder>` and launch it only after checking data compatibility and taking a backup. Binary retrieval may take minutes; no recovery-time guarantee is established. Never replace the data directory with the application folder. Preserve prior versions and do not delete releases as rollback.
- **Approval**: The user approved this platform decision. Before first production publication, review a deployment plan and approve execution, as required by repository guidance. Preserve the stack's intended auto-publish-after-merge flow once that plan is approved; routine releases after passing agreed checks do not need another conversational approval. Humans handle project deletion, destructive data actions, primary credential rotation, repository visibility and billing decisions.
- **Logs**: Read pipeline output with `gh run list` and `gh run view <run-id> --log-failed` (or `--log`). Releases has no native application runtime log stream. Desktop runtime diagnostics must be implemented locally and must avoid ticket descriptions/time-entry data by default. No cloud telemetry is selected.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| Missing runtime/WebView2 prerequisites | Devil's advocate | H | H | Extract published ZIP on clean Windows 11 without SDK/VS; verify launch and document prerequisites |
| Untested schema downgrade | Pre-mortem | M | H | Back up local data and verify upgrade/recovery compatibility before releasing schema changes |
| Private release access blocks intended downloader | Devil's advocate | M | M | Confirm repository visibility and test download using the intended account before first publication |
| Private Windows CI allowance exhausted | Devil's advocate | M | M | Inspect account allowance, estimate measured build minutes, set budget controls and artifact retention |
| Invalid .NET 10 RID or missing override mapping | Unknown unknowns | M | H | Use portable RID and verify documented conditional RuntimeIdentifier mapping in deployment implementation |
| Windows App SDK self-contained mistaken for all prerequisites | Unknown unknowns | M | H | Verify .NET runtime, Windows App SDK and WebView2 independently on a clean device |
| Source archive mistaken for runnable application | Unknown unknowns | M | M | Give uploaded application ZIP a version/architecture name and explicitly identify it in release notes |
| Empty tests treated as functional verification | Pre-mortem | H | H | Require a meaningful launch/persistence check; prove Playwright/WebView2 CI feasibility separately |
| Local data stored under replaceable application directory | Research finding | M | H | Select stable per-user storage outside the extraction folder and verify preservation across updates |
| Unsigned download blocked by SmartScreen or device policy | Research finding | M | H | Test on the actual target device; decide signing/prerequisite policy before promising portable installation |
| Release assets exceed provider size limits | Research finding | L | M | Measure final ZIP; ensure each GitHub release asset remains below 2 GiB |
| Optional provider MCP is beta or status unverified | Research finding | L | L | Use documented CLI/API; do not depend on optional MCP for publication or recovery |

Windows signing and distribution implications: [Microsoft distribution guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/choose-distribution-path).

## Getting Started

The following are implementation handoff steps, not commands executed during this research.

1. Create and review `context/deployment/deploy-plan.md` in Plan Mode using this document and `tech-stack.md`. Confirm GitHub repository identity/visibility, account allowances, target CPU architecture (x64 is provisional), installed .NET 10 SDK/workload, and release naming. Pin a reproducible SDK/toolchain in the implementation plan; no `global.json` currently pins the SDK and MAUI packages use `$(MauiVersion)`.
2. Add/verify the documented conditional `RuntimeIdentifierOverride` to `RuntimeIdentifier` mapping before relying on that parameter. Retain the Windows-only `TargetFrameworks`. The official .NET 10 workflow uses `win-x64`, not `win10-x64`. Scope publishing to the application project, not the solution. Candidate PowerShell command after that prerequisite:

   ```powershell
   dotnet publish src/TimesheetAssistant/TimesheetAssistant.csproj -f net10.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true
   ```

   This combines the documented MAUI portable-RID/Windows App SDK settings with explicit .NET self-contained deployment. It is documentation-checked, not build- or device-tested in this research. WebView2 must still be verified. [MAUI 10 guidance](https://learn.microsoft.com/en-us/dotnet/maui/windows/deployment/publish-unpackaged-cli?view=net-maui-10.0), [.NET publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/).
3. ZIP the complete verified publish folder, including dependencies and web assets. Use a name such as `TimesheetAssistant-<version>-win-x64.zip`. Extract into a clean Windows 11 environment, test launch plus implemented persistence behavior, verify update/data preservation, record size and prerequisites. The current empty test project cannot verify MVP functionality.
4. Use a prepared remote tag and exact built commit. A reviewable draft release can be created with `gh release create <tag> <zip-path> --verify-tag --draft --title <title> --notes-file <notes-path>`. Download and inspect its assets using authenticated CLI before publication. The angle-bracket values are placeholders. Draft is intentional for the initial deployment review; it is not a new permanent approval requirement. [CLI](https://cli.github.com/manual/gh_release_create).
5. After approval of the first deployment plan, implement the Windows Actions build/test/publish flow and verify published assets. Preserve auto-publication after a successful merge to main as the intended steady-state policy. Do not configure CI or publish while performing this research.

## Verification and Remaining Decisions

- Foundation stack, PRD, platform criteria and application project were read; no existing infrastructure document was overwritten.
- Official platform documentation was researched in parallel; evidence links are included above. Feature lifecycle caveats are recorded with the research date. Absence of a beta label is not treated as proof of GA.
- Build, tests, actual publish, clean-device launch, ZIP size, UI test feasibility, GitHub account access and billing were not verified during this documentation task.
- Repository visibility, exact SDK/workload pin, target architecture, signing policy, stable local data location and first-release version remain deployment/implementation decisions. No production access or secret is claimed to exist.

## Out of Scope

- Docker images and Dockerfiles.
- CI/CD pipeline implementation during research.
- Cloud execution of the desktop UI, cloud user-data storage and telemetry.
- Jira/MyWork integration and authentication.
- Production-scale multi-region HA/DR.
- Automatic desktop updater, Store/MSIX migration and signing-service selection.
