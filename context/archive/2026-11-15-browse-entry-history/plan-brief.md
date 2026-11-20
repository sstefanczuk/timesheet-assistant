# Browse Entry History — Plan Brief

> Full plan: [plan.md](plan.md)

## What & Why

S-07 makes saved work visible immediately after opening the application.
Show Today separately and the three latest earlier dates containing entries,
then load one older nonempty day per request without any retention cutoff.

## Starting Point

S-01 already supplies persistent SQLite CRUD, an editor, daily totals and draft
protection. The root page reads one date; multi-day history is missing.
Existing desktop infrastructure supports isolated restarts and app-folder copies.

## Desired End State

History and the existing editor share one screen. Every earlier day shows full
entries and its total; Edit opens the exact entry in the form. Today has a clear
add-from-scratch action. Changes refresh history while preserving the loaded
date interval; empty groups disappear and older paging continues correctly.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Initial history | Three earlier nonempty dates, newest first; Today separate | Matches the agreed browsing workflow | Roadmap / PRD |
| Older history | Append one complete nonempty day; unlimited retention | Skip gaps without hiding old entries | Roadmap / PRD |
| Screen layout | History and current editor together | User wants a single screen | Plan |
| Editing | Edit each historical entry directly in the existing form | Faster correction without a separate day-opening step | Plan |
| Visible content | Date, Jira ID, title, description, duration and day total | Full inspection and a foundation for S-08 | Plan |
| Refresh | Preserve oldest loaded boundary, remove empty days, keep at least three when available | Continue browsing after mutations | Plan |
| Form redesign | Defer compact layout improvements | Keep S-07 focused on history | Plan |
| Persistence | Keep schema v1 and existing external data directory | Avoid unnecessary migrations | Code research |

## Scope

**In scope:** bounded history reads, independent Core state, shared-screen UI,
draft-safe direct editing, empty/error/retry/exhaustion states, pure and real
desktop tests, restart and old-package-to-new-package compatibility evidence.

**Out of scope:** copying (S-08), summaries, outstanding days, confirmations,
templates, compact form redesign, persistent drafts/scroll, CI work and releases.

## Architecture / Approach

Extend the existing Workdays feature. SQLite selects indexed distinct dates and
complete entries; a separate scoped history service owns pagination and retained
interval state. The current editor keeps its selected-day snapshot. Guarded
context changes preserve drafts on cancellation; refresh failures preserve the
last loaded history and never turn a committed save into a repeated mutation.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. History Reads and Core State | Bounded reads, retained interval and pure tests | Cursor movement after deleting an entire day |
| 2. History and Editor on One Screen | Full history, direct Edit, Today/add actions | Draft loss or ambiguous UI selectors |
| 3. Desktop Verification and Persistence Evidence | Real flows, restart, upgrade and author acceptance | Confusing folder copies with actual version compatibility |

**Prerequisites:** accepted S-01; Windows/.NET MAUI/WebView2 environment and an identified S-01 package for upgrade verification.
**Estimated effort:** approximately 3–5 focused sessions across three phases, subject to desktop verification results.

## Open Risks & Assumptions

- Local history state resets to three dates on restart; no exact scroll restoration.
- Explicit refresh/actions re-evaluate local today; no background midnight timer.
- Accepted S-01 package 111402 is present locally; retain it for the upgrade test or reproduce its identified revision if it becomes unavailable.
- Desktop CI and clean-device compatibility remain unverified and outside S-07.

## Success Criteria (Summary)

- Startup, one-day paging, direct editing and refresh show exact retained entries.
- Pure tests and the full isolated desktop suite pass against a fresh package.
- Old-to-new version data survives; results and author confirmation are recorded.

## References

- `context/foundation/roadmap.md` S-07; `context/foundation/prd.md` FR-002/006/011.
- `context/foundation/tech-stack.md`; archived S-01 `verification.md` (read-only).
- Full plan includes source anchors, file contracts and canonical execution Progress.
