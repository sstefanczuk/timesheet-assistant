---
change_id: diagnose-webview2-native-crash
title: Diagnoza natywnej awarii WebView2 na windows-2025
status: implementing
created: 2026-11-08
updated: 2026-11-09
archived_at: null
---

## Notes

Zmiana pomocnicza dla F-01 (`verify-desktop-ui-path`), którego faza 3 pozostaje nieodebrana. Użytkownik zatwierdził przygotowanie planu, złożoność MEDIUM bez dodatkowych pytań oraz trzy fazy dnia 2026-11-08. Zgoda obejmuje zapis dokumentów, nie uruchamianie kolejnych eksperymentów CI ani nowych poprawek. Dotychczasowe limity poprawek zostały wyczerpane. Surowa pamięć i pełne logi debuggera pozostają na runnerze.

## Phase 1 implementation — 2026-11-09

- User invoked phase 1 and selected delegated implementation. Added opt-in handle-backed process identity manifests, bounded private Crashpad report preservation, strict Event 1000/1001 correlation, twelve synthetic cases and a manual diagnostic workflow with explicit three-day exports. Existing F-01 changes remain uncommitted and preserved; the candidate fixture depends on them.
- Local Release solution build passed with zero warnings/errors. The first real desktop interaction passed, but validation rejected its manifest because retained handles had not yet signaled when the job active count reached zero. Correction 1 waits for those handles within the unchanged cleanup budget. Revalidation passed one desktop interaction and verified complete exit identities for all eight owned processes (session `db30ca8d69c44fe7a4fb54114eab53ab`, TRX `artifacts/native-phase1-local/native-phase1-correction1.trx`).
- Twelve synthetic correlation/sanitization cases passed, including foreign/reused PID, cleanup exits, hexadecimal event fields, path redaction and ambiguous report IDs. Actionlint and whitespace checks passed. UnitTests discovery completed empty; no functional unit coverage is claimed. Break-check and hosted baseline remain pending. No staging, phase commit, push or CI run has occurred for this phase.
- Prepared an ignored snapshot helper for one passive baseline on `ci/webview2-native-metadata-20261109`. Its experiment-only tree maps the new diagnostic workflow onto the registered `windows.yml` dispatch path, without modifying the working-tree preview workflow or main. Execution requires the separate authorization specified by the plan.

## Phase 1 baseline evidence — 2026-11-09

- User authorized one isolated technical snapshot/push and passive baseline. Snapshot `f6496537c95d471a425e3ba7e31ac49ff1d18f55` was pushed only to `ci/webview2-native-metadata-20261109`; local main remains `50fe6de`. Run: https://github.com/sstefanczuk/timesheet-assistant/actions/runs/42572422837. No release job, active debugger or raw dump export was used.
- Runner image `20261103.298.1` (`win25-vs2026`), WebView2 `156.0.4314.58`; executable signatures passed. Restore/build, twelve synthetic cases and packaging passed. The desktop test failed at the unchanged 60-second startup budget, and its original exit code 1 was preserved as a failed job.
- Session `9e81be002e1a4c659da1372a709c2011`: owned WebView2 PIDs 4112 and 9096 exited naturally with `0x80000003`, with actual creation/exit FILETIME recorded; helpers exited 0. App PID 6252 remained alive until cleanup and is classified cleanup=true. All observed process exit identities are complete, CleanupComplete=true and PrivateEvidenceRemoved=true.
- Passive collector returned QueryStatus=no-events and no-unambiguous-match. Own-profile report preservation returned absent/count0/bytes0. This is an explicit evidence gap, not a collector crash or identified root cause. The native module/check/stack remains unknown; F-01 phase3 stays unaccepted.
- Downloaded artifact contains exactly preflight.json, own-processes.json, crash-metadata.json, profile-report-status.json, baseline-status.json and desktop-ui.trx. API confirms three-day retention, created 2026-11-09T06:29:18Z and expiring 2026-11-12T06:29:17Z. Local copy: ignored artifacts/native-phase1-ci/baseline-42572422837/.
- Break-check temporarily removed creation-time matching using a separate staging index: reused-PID case failed as required. Collector was unconditionally restored from that index, and all twelve cases passed again. Real staging area remains empty. Actionlint and whitespace checks pass; no final phase commit has been made. Phase1 automated criteria are satisfied by the explicit missing-evidence outcome permitted in the plan, independently of the still-red F-01 UI gate.
