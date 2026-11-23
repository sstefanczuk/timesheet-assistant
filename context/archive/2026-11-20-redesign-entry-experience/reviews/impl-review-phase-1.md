<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Redesign entry experience

- **Plan**: context/changes/redesign-entry-experience/plan.md
- **Scope**: Phase 1 of 3
- **Reviewed phases**: 1
- **Date**: 2026-11-21
- **Verdict**: REJECTED
- **Findings**: 1 critical, 1 warning, 0 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | FAIL |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Navigation to Settings discards unsaved work without confirmation

- **Severity**: CRITICAL
- **Impact**: MEDIUM — the fix must coordinate navigation with the existing editor state and confirmation behavior.
- **Dimension**: Safety & Quality
- **Location**: src/TimesheetAssistant/Components/Layout/NavMenu.razor:10
- **Detail**: The new Settings link navigates away from `WorkdayPage`, whose input is component-local state. Navigation does not go through `WithDiscardAsync` (WorkdayPage.razor:372–386), so an unsaved add or edit is lost immediately; the link also remains outside the confirmation's inert region. Phase 2 intentionally changes how the future dialog handles drafts, but that behavior has not been implemented in phase 1.
- **Fix**: Guard navigation while the current editor is dirty and reuse the existing confirmation before leaving; include a desktop scenario covering cancel and confirm.
  - Strength: Preserves current unsaved-input protection until the phase 2 dialog replaces it.
  - Tradeoff: Requires coordinating layout navigation with feature state; phase 2 may replace this mechanism.
  - Confidence: HIGH — the link changes the route while editor state is held by the page component.
  - Blind spot: The precise integration mechanism should follow the phase 2 design.
- **Decision**: FIXED IN PHASE 2 — the author deferred this to the redesign. The add draft now survives navigation within the session; closing an edit discards it as agreed. Desktop coverage passes; author verification remains pending.

### F2 — Accent combinations lack readable text contrast

- **Severity**: WARNING
- **Impact**: MEDIUM — the user explicitly requested both the exact accent and white primary-button text, so fixing contrast requires a design choice.
- **Dimension**: Plan Adherence
- **Location**: src/TimesheetAssistant/wwwroot/app.css:2
- **Detail**: `#FB9900` against white is approximately 2.17:1. It is used for white primary-button text (line 12), and as text on white in the light theme (lines 4 and 10). These small labels do not meet the plan's readable-contrast intent. The exact color and white text were explicitly requested after planning, so do not change either without discussing the tradeoff.
- **Fix A**: Keep the exact accent as the shared background and use dark text on primary buttons; use a darker text token for accent-colored text on light surfaces.
  - Strength: Retains the specified accent and makes small text readable.
  - Tradeoff: Changes the requested white button text and introduces a separate semantic foreground color.
  - Confidence: HIGH — dark text on this bright accent has substantially higher contrast.
  - Blind spot: The preferred dark text color requires visual review.
- **Fix B**: Keep white button text and use a darker orange for primary backgrounds and light-theme text.
  - Strength: Preserves white text and improves contrast.
  - Tradeoff: Those elements no longer use the exact requested accent.
  - Confidence: HIGH — the current ratio is determined by the two specified colors.
  - Blind spot: The replacement shade needs approval.
- **Decision**: ACCEPTED — the author considers the contrast readable and chose `#f29500` as the darkest acceptable accent while keeping white primary-button text. This does not meet the usual contrast threshold for small text.

## Verification

- `dotnet build TimesheetAssistant.sln` — passed, zero warnings and errors.
- `dotnet test src/TimesheetAssistant.UnitTests/TimesheetAssistant.UnitTests.csproj` — 104 passed.
- Manual phase 1 verification was confirmed by the author on 2026-11-21 and recorded in `verification.md`; the contrast calculation above is an independent review finding.
- Previously recorded desktop runs passed 16 scenarios plus one separate retry; no clean single full-suite passing run is recorded. Phase 3 remains pending.
- Two requested parallel review agents were launched. One reported tool access unavailable; the other did not return a report. Findings were independently checked against the source by the primary reviewer.
