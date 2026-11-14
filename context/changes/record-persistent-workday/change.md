---
change_id: record-persistent-workday
title: Record persistent workday
status: implemented
created: 2026-11-11
updated: 2026-11-14
archived_at: null
---

## Notes

S-01 from roadmap v3. Complexity MEDIUM and seven design questions confirmed by the user. Four implementation phases approved on 2026-11-11. Decisions are recorded in plan.md and plan-brief.md; implementation started on 2026-11-12.

2026-11-12: User requested feature-oriented folder organization. Added the durable convention to src/AGENTS.md and aligned planned production/test paths under Features/Workdays within their existing project boundaries. Product behavior, phase structure and execution Progress remain unchanged.

2026-11-12: User requested readable unit tests with explicit Arrange/Act/Assert comments and incidental setup extracted into builders, extension methods, private methods or similar helpers. Recorded in src/AGENTS.md and the plan/brief; test scenarios and execution Progress remain unchanged.

2026-11-12: Phase 1 implemented: portable Core rules, service/store contracts and pure fake-store tests. Local verification: 87 passed, 0 failed, 0 skipped; solution build succeeded with 0 warnings and 0 errors. A temporary zero-duration parser mutation caused 3 expected test failures, was restored from staging, and the full pure suite passed again. No persistence or desktop acceptance is claimed; phases 2–4 remain pending.

2026-11-12: Phase 2 adds SQLite schema v1 and transactional CRUD, canonical data-directory resolution, an exclusive app-lifetime file lease, storage composition and an English unavailable/Retry screen. Microsoft.Data.Sqlite is pinned to 10.0.11. Local build, pure tests and dependency audit are recorded in verification.md. The selected implementation subagent lacked filesystem tools and returned without modifications; implementation continued in the parent context. No real-store/UI acceptance is claimed; phases 3–4 remain pending.

2026-11-13: During phase 2 review triage, the user approved F1's temporary-database fix. New databases are fully initialized before publication to the final path; existing final files remain protected. Implementation is in the working tree; real desktop failure/retry evidence remains deferred to phase 4. F2 is awaiting its triage decision.

2026-11-13: User approved F2. The current desktop fixture now always supplies a unique child-only domain directory, separate from its WebView2 profile, and validates its test-owned session root before cleanup after process exit. Both phase 2 review findings are fixed in the working tree. Full scenario-owned restart persistence remains phase 4 scope.

2026-11-13: F1/F2 verification passed: Release build with 0 warnings/errors, 87 pure tests and 1 real desktop smoke against fresh working-tree publication publish-0.1.0-preview.111301. The caller's deliberately invalid data override was replaced for the child; its logged test-owned domain directory was removed after successful cleanup. See verification.md for TRX and session log identity. This is starter smoke/startup-isolation evidence, not product CRUD/restart or induced-initialization-failure acceptance.

2026-11-13: Phase 3 implemented by the user-selected subagent: feature-owned workday screen, date selection, durable-service CRUD, totals, field errors and keyboard-operable discard/delete confirmations. Removed sample pages and About link; Workday navigation is static to avoid resetting drafts. Release build passed with 0 warnings/errors; pure tests passed 87/87. Manual criterion 3.2 remains pending, so phase 3 is not closed and phase 4 has not started. See verification.md for commands and local publication identity.

2026-11-14: Author confirmed that everything works as expected after manually checking publication 0.1.0-preview.111303. Manual criterion 3.2 is complete. Phase 3 awaits its commit; phase 4 remains pending. Status returned to implementing because the earlier review covered phase 2, not the new phase 3 implementation.

2026-11-14: Phase 3 committed as e47608c and reviewed by two parallel agents. Review verdict APPROVED with one low-impact observation about culture-dependent date text in the table caption; triage pending. Release build passed with 0 warnings/errors and all 87 pure tests passed again. Report: reviews/impl-review-phase-3.md. Phase 4 remains pending; this review does not establish persistence/failure or CI acceptance.

2026-11-14: Phase 4 implementation started. User selected delegation; the subagent had no filesystem/shell tools and returned without edits. The parent continued the approved scope. Automated desktop and final author acceptance remain pending.

2026-11-14: Phase 4 automated desktop verification passed 9/9 against working-tree package 0.1.0-preview.111402; 87 pure tests and Release build passed. A deliberate ordering mutation was caught and restored. All 18 passing-run launch sessions and scenario data cleaned up. Manual criterion 4.5 and final review/triage remain pending; no local S-01/CI/full-MVP acceptance is claimed. See verification.md.

2026-11-14: The author confirmed that package 0.1.0-preview.111402 works as expected, explicitly including loading the same database and previously saved entries after launching from another application location. Manual criterion 4.5 is complete. All plan criteria are now checked; the phase 4 commit and final implementation review/triage remain pending. Desktop CI, clean-device and full-MVP acceptance remain outside this confirmation.

2026-11-14: Phase 4 committed as b3a3a30. All four phases are implemented and verified, including author confirmation for package 0.1.0-preview.111402. Phase 4 SHA annotations and implemented status are recorded in the closing documentation commit. Final implementation review/triage remains required before merging; deferred desktop CI and full-MVP acceptance are unchanged.
