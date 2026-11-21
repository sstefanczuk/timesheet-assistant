# UI Playbook

Use this playbook when building or changing the Timesheet Assistant interface. It describes the current implementation and conventions; update it alongside any agreed change to the design. Shared styles and theme tokens live in [wwwroot/app.css](wwwroot/app.css). Bootstrap supplies the base classes; application overrides come after Bootstrap.

## Principles

- Reuse existing components, CSS classes and theme tokens before introducing new ones. Define shared colors once; do not repeat their literal values in feature styles.
- Support both themes, narrow windows, keyboard navigation, visible focus, disabled controls and clear validation and error states. Check changes in the actual Windows application.
- Use English for interface text. Give controls clear labels and use semantic HTML.
- Match the existing compact typography and spacing. `app.css` defines the base font and heading hierarchy.

## Buttons

Use a native `<button>` with `btn` and one variant class. Button labels are bold. Specify `type="button"` unless the button submits a form; use `type="submit"` for submission. Use `disabled` when an action cannot run, rather than relying only on visual styling.

| Variant | Classes | Current appearance and use |
| --- | --- | --- |
| Primary | `btn btn-primary` | Accent background and white text; the main action, such as Save or Add entry. Hover darkens the button. |
| Secondary | `btn btn-outline-secondary` | Muted filled background and border; supporting actions such as Cancel, Refresh history and switching theme. Hover uses the border color as the background. |
| Outlined primary | `btn btn-outline-primary` | Neutral background and border; supporting actions such as Edit, Retry history and Load older day. Hover uses the accent background and white text. |
| Outlined danger | `btn btn-outline-danger` | Danger-colored text and border; an action that starts a destructive operation, such as Delete. |
| Danger | `btn btn-danger` | Bootstrap's filled danger style; currently used for confirmation-dialog actions. It is not customized by the application theme. |

Add `btn-sm` for compact table-row actions. Bootstrap spacing utilities such as `ms-2` and `mb-3` can provide local spacing. The three common non-destructive variants are primary, secondary and outlined primary; danger variants are separate. Do not infer that a style is a reusable Razor component: these are CSS classes on native buttons.

```razor
<button type="submit" class="btn btn-primary">Save</button>
<button type="button" class="btn btn-outline-secondary">Cancel</button>
<button type="button" class="btn btn-sm btn-outline-primary">Edit</button>
<button type="button" class="btn btn-sm btn-outline-danger">Delete</button>
```

## Colors and themes

- `--accent` is the shared accent for primary buttons, links, focus, branding and the active menu indicator. It is defined once in `app.css`; the current value is `#FB9900`.
- Theme classes `.theme-dark` and `.theme-light` define tokens including `--background`, `--sidebar`, `--surface`, `--surface-raised`, `--text`, `--muted`, `--border`, `--secondary-bg`, `--danger` and `--success`. The dark theme is the default; Settings changes the locally saved preference.
- Primary button text is currently white on the accent. This combination has limited contrast; assess readability in both themes when changing colors or typography.
- Use tokens instead of hard-coded feature colors. Keep error and success messages understandable without color alone.

## Page structure and navigation

- Use `TimesheetAssistant.Components.PageHeader` with a `Title` for page titles. It renders an `h1` and a shared divider. Routing focuses the heading after navigation; the heading intentionally has no visible focus outline.
- Add navigation destinations through `Components/Layout/NavMenu.razor`, using `NavLink` and a decorative, `aria-hidden` SVG icon. The active link uses the accent indicator.
- Keep feature pages and feature-specific styling in their feature folder. Shared layout belongs in `Components/Layout/`.

## Inputs, feedback and data

- Pair every input with a visible `<label>` and use `form-control`; associate help and error text with `aria-describedby`. Mark invalid fields with `aria-invalid` and give errors a textual explanation.
- Preserve the visible keyboard focus style. Inputs use a thinner accent outline; disabled inputs use the raised surface token.
- Use `alert alert-danger` for errors, `alert alert-warning` for warnings and `alert alert-light` for neutral summaries. Use `role="alert"` for relevant errors and `role="status"` or `aria-live` for status updates.
- Wrap wide tables in `table-responsive`, use `table`, meaningful headers and a caption. Preserve horizontal access in narrow windows.
- Reuse the existing `WorkdayConfirmation` for its current workday confirmation flow. Dialogs must identify their title and message, support keyboard operation and manage focus when opened and closed; do not assume the existing component is a general-purpose dialog API.
