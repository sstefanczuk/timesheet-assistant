# Review follow-ups

## Phase 2: navigation and unsaved input

- Source: phase 1 implementation review, F1. The author deferred this issue to phase 2.
- Ensure navigation to Settings follows the phase 2 contract: retain an add draft in memory for the session and discard unsaved edit changes on close without a warning. Cover navigation away and back for both modes in desktop tests. Until phase 2 is implemented, navigating to Settings can discard the current unsaved form.
