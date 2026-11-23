# Duplicate Work Entries — Plan Brief

> Full plan: `context/changes/copy-earlier-day-entries/plan.md`

## What & Why

Duplicate a previous entry through an editable form to reduce repeated typing while keeping each saved entry independent.

## Starting Point

The home table shows today and paged earlier entries. The existing dialog supports adding and editing, while the store provides range reads and assigns new identities.

## Desired End State

An accessible Duplicate action opens the exact same control and form as “Add entry”, prefilled with the source fields. The suggested date is the first weekday after the source through today with less than 7 h 12 min recorded, or today if none exists. The user can change the date and fields, save a separate entry, or cancel without saving.

## Key Decisions Made

| Decision | Choice | Why |
| --- | --- | --- |
| Interaction | Duplicate one entry at a time through the exact same add control and form opened by “Add entry” | The user can review and edit before saving. |
| Date | Earliest incomplete Monday–Friday after source through today; otherwise today | Suggests the next day needing work without allowing future dates. |
| Day total | Less than 432 minutes is incomplete; exact and excess are skipped | Keeps the selection rule aligned with the user's definition. |
| Destination | User may select any date up to today, including the source date | The suggested date remains editable. |
| Existing entries | Append a new independent entry, including duplicate Jira IDs | Existing records and the source remain intact. |
| Holidays | No automatic holiday or leave recognition | The application has no calendar or day types yet. |

## Scope

**In scope:** Single-entry duplication, date suggestion, independent editable draft, validation, unit and real desktop tests, and acceptance evidence.

**Out of scope:** Bulk selection, immediate save, future dates, holiday calendars, transfer confirmations and summaries.

## Architecture / Approach

Portable workday logic reads the complete relevant interval from the store rather than relying on the paged history UI. The duplicate action opens the exact same add-entry control and form as “Add entry”; its save path creates a new entry from a separate draft. Failures remain visible and do not mutate the source.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Working duplication | Button, suggested date, editable form, save and unit tests | Sparse history or boundaries yield the wrong date. |
| 2. Desktop verification and acceptance | Published-app scenarios, restart evidence and author confirmation | Local desktop execution or manual acceptance may be blocked. |

**Prerequisites:** S-09 completed; Windows, .NET 10 MAUI workload and WebView2 for desktop verification; prior package for upgrade regression.
**Estimated effort:** Not estimated.

## Open Risks & Assumptions

- Holidays and leave are not recognized automatically in this slice; later day-type work may require revisiting the suggestion rule.
- Desktop CI and clean-device compatibility remain separate, unverified gates.

## Success Criteria (Summary)

- Duplicate opens an editable form with the agreed date, and cancel makes no change.
- Save creates an independent entry, retained after restart, without altering the source.
- Unit and real desktop tests pass and author acceptance is recorded.
