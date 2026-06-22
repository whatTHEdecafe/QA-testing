# Reusable UI Style Reference

## 1. Purpose and instructions for future AI agents

This file defines the preferred visual identity for future software tools. Read it in full before creating or changing an interface.

Preserve the recognizable colors, typography, surfaces, controls, spacing, status treatments, and practical visual character described here. Do not blindly copy any previous application's layout. Choose navigation and page structures that fit the new workflow: a sidebar, top navigation, tabs, dashboard, wizard, split view, or another structure may be used when it is the clearer choice.

Functional clarity and usability take priority over copying an old layout. The finished application should still feel like part of the same family of tools through its navy navigation areas, red primary actions, selective purple accents, light page background, white cards, slate text, rounded controls, and restrained shadows.

General rules:

- Read this reference before implementing UI work.
- Use the supplied tokens instead of scattering new color and spacing literals.
- Keep interfaces clean and avoid overcrowding.
- Use tabs for closely related views, not unrelated destinations.
- Use expandable sections, filters, details views, or progressive disclosure when they reduce visual noise.
- Use the moving loader for meaningful operations that require a noticeable wait.
- Do not use a blocking overlay for tiny or nearly instant actions.
- Adapt layouts to the product while preserving the design language.
- Do not copy business logic from the source application.

### Fixed identity versus flexible decisions

The color family, system typography, surface treatment, control shapes, status colors, spacing rhythm, and interaction character are the fixed identity. Page architecture, navigation pattern, information hierarchy, column count, and workflow structure are flexible decisions that must suit each application.

## 2. Visual identity summary

The style is compact, professional, and optimized for practical software tools:

- Dark navy application headers or navigation areas provide a strong frame.
- Red identifies the principal action, active destination, and important focus accents.
- Purple is an alternative or secondary action color and should be used selectively.
- The page background is light gray rather than stark white.
- White cards and panels hold related content.
- Slate text and borders create a quiet hierarchy.
- Controls and cards use rounded corners, usually 8px and 12px.
- Shadows are soft and functional, not decorative.
- Typography uses a compact operating-system font stack.
- Success, warning, error, and information states have clear background, border, and text colors.
- The overall result should feel like a dependable internal tool rather than a marketing site.

These elements define the visual identity. Exact page layouts may and should change according to the application.

## 3. Centralized design tokens

Use these values as the default token set. They are consolidated from the active styles on which this reference is based.

```css
:root {
  /* Brand and action colors */
  --color-primary: #e63946;
  --color-primary-hover: #c1121f;
  --color-primary-soft: #fee2e2;
  --color-accent: #332a88;
  --color-accent-hover: #2a226e;
  --color-accent-soft: #f5f3ff;
  --color-navy: #0d1b2a;
  --color-navy-secondary: #1d3557;

  /* Page and surfaces */
  --color-page: #f0f2f5;
  --color-surface: #ffffff;
  --color-surface-subtle: #f8fafc;
  --color-surface-control: #f9fafb;
  --color-surface-muted: #f1f5f9;

  /* Text */
  --color-text: #1a1a2e;
  --color-text-strong: #0d1b2a;
  --color-text-secondary: #475569;
  --color-text-muted: #64748b;
  --color-text-faint: #94a3b8;
  --color-text-on-dark: #ffffff;

  /* Borders */
  --color-border: #d1d5db;
  --color-border-soft: #e2e8f0;
  --color-border-subtle: #e9ecef;
  --color-border-strong: #cbd5e1;

  /* Status: success */
  --color-success-bg: #d1fae5;
  --color-success-border: #6ee7b7;
  --color-success-text: #065f46;

  /* Status: warning */
  --color-warning-bg: #fef3c7;
  --color-warning-border: #fcd34d;
  --color-warning-text: #92400e;

  /* Status: error */
  --color-error-bg: #fee2e2;
  --color-error-border: #fca5a5;
  --color-error-text: #991b1b;

  /* Status: information */
  --color-info-bg: #e0f2fe;
  --color-info-border: #7dd3fc;
  --color-info-text: #0c4a6e;

  /* Spacing rhythm */
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-7: 28px;
  --space-8: 32px;
  --space-10: 40px;
  --space-12: 48px;

  /* Shape */
  --radius-small: 6px;
  --radius-control: 8px;
  --radius-field-shell: 10px;
  --radius-card: 12px;
  --radius-modal: 16px;
  --radius-pill: 999px;

  /* Shadows */
  --shadow-subtle: 0 1px 3px rgba(0, 0, 0, 0.05);
  --shadow-card: 0 2px 8px rgba(0, 0, 0, 0.08);
  --shadow-header: 0 2px 6px rgba(0, 0, 0, 0.25);
  --shadow-dropdown: 0 8px 20px rgba(0, 0, 0, 0.1);
  --shadow-modal: 0 20px 50px rgba(0, 0, 0, 0.2);
  --shadow-selected: 0 0 0 2px rgba(230, 57, 70, 0.25);

  /* Typography */
  --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    Helvetica, Arial, sans-serif;
  --font-mono: ui-monospace, "Cascadia Code", "SFMono-Regular", Consolas,
    "Liberation Mono", monospace;
  --font-size-xs: 0.72rem;
  --font-size-sm: 0.82rem;
  --font-size-control: 0.9rem;
  --font-size-body: 0.97rem;
  --font-size-section: 1.05rem;
  --font-size-page: 1.35rem;
  --font-size-display: 1.7rem;

  /* Motion and focus */
  --transition-fast: 150ms ease;
  --transition-standard: 200ms ease;
  --focus-ring: 0 0 0 3px rgba(230, 57, 70, 0.28);

  /* Layering */
  --z-dropdown: 20;
  --z-sticky: 50;
  --z-modal: 200;

  /* Layout */
  --content-max: 1400px;
  --reading-max: 900px;
  --header-height: 58px;
}
```

## 4. Reusable foundational CSS

The following is a complete starting point for `src/styles/shared-theme.css`. Components may use CSS Modules on top of it, but should consume these tokens.

```css
:root {
  --color-primary: #e63946;
  --color-primary-hover: #c1121f;
  --color-primary-soft: #fee2e2;
  --color-accent: #332a88;
  --color-accent-hover: #2a226e;
  --color-accent-soft: #f5f3ff;
  --color-navy: #0d1b2a;
  --color-navy-secondary: #1d3557;
  --color-page: #f0f2f5;
  --color-surface: #fff;
  --color-surface-subtle: #f8fafc;
  --color-surface-control: #f9fafb;
  --color-surface-muted: #f1f5f9;
  --color-text: #1a1a2e;
  --color-text-strong: #0d1b2a;
  --color-text-secondary: #475569;
  --color-text-muted: #64748b;
  --color-text-faint: #94a3b8;
  --color-text-on-dark: #fff;
  --color-border: #d1d5db;
  --color-border-soft: #e2e8f0;
  --color-border-subtle: #e9ecef;
  --color-border-strong: #cbd5e1;
  --color-success-bg: #d1fae5;
  --color-success-border: #6ee7b7;
  --color-success-text: #065f46;
  --color-warning-bg: #fef3c7;
  --color-warning-border: #fcd34d;
  --color-warning-text: #92400e;
  --color-error-bg: #fee2e2;
  --color-error-border: #fca5a5;
  --color-error-text: #991b1b;
  --color-info-bg: #e0f2fe;
  --color-info-border: #7dd3fc;
  --color-info-text: #0c4a6e;
  --space-1: 4px;
  --space-2: 8px;
  --space-3: 12px;
  --space-4: 16px;
  --space-5: 20px;
  --space-6: 24px;
  --space-7: 28px;
  --space-8: 32px;
  --space-10: 40px;
  --space-12: 48px;
  --radius-small: 6px;
  --radius-control: 8px;
  --radius-field-shell: 10px;
  --radius-card: 12px;
  --radius-modal: 16px;
  --radius-pill: 999px;
  --shadow-subtle: 0 1px 3px rgba(0, 0, 0, 0.05);
  --shadow-card: 0 2px 8px rgba(0, 0, 0, 0.08);
  --shadow-header: 0 2px 6px rgba(0, 0, 0, 0.25);
  --shadow-dropdown: 0 8px 20px rgba(0, 0, 0, 0.1);
  --shadow-modal: 0 20px 50px rgba(0, 0, 0, 0.2);
  --shadow-selected: 0 0 0 2px rgba(230, 57, 70, 0.25);
  --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    Helvetica, Arial, sans-serif;
  --font-mono: ui-monospace, "Cascadia Code", "SFMono-Regular", Consolas,
    "Liberation Mono", monospace;
  --font-size-xs: 0.72rem;
  --font-size-sm: 0.82rem;
  --font-size-control: 0.9rem;
  --font-size-body: 0.97rem;
  --font-size-section: 1.05rem;
  --font-size-page: 1.35rem;
  --font-size-display: 1.7rem;
  --transition-fast: 150ms ease;
  --transition-standard: 200ms ease;
  --focus-ring: 0 0 0 3px rgba(230, 57, 70, 0.28);
  --z-dropdown: 20;
  --z-sticky: 50;
  --z-modal: 200;
  --content-max: 1400px;
  --reading-max: 900px;
  --header-height: 58px;
}

*,
*::before,
*::after {
  box-sizing: border-box;
}

html {
  color-scheme: light;
}

body {
  margin: 0;
  min-width: 320px;
  min-height: 100vh;
  background: var(--color-page);
  color: var(--color-text);
  font-family: var(--font-sans);
  font-size: var(--font-size-body);
  line-height: 1.55;
  -webkit-font-smoothing: antialiased;
  text-rendering: optimizeLegibility;
}

#root {
  min-height: 100vh;
}

button,
input,
textarea,
select {
  font: inherit;
}

button,
[role="button"] {
  -webkit-tap-highlight-color: transparent;
}

a {
  color: var(--color-accent);
  text-underline-offset: 3px;
}

a:hover {
  color: var(--color-accent-hover);
}

.app-shell {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-header {
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
  min-height: var(--header-height);
  background: var(--color-navy);
  color: var(--color-text-on-dark);
  box-shadow: var(--shadow-header);
}

.app-header-inner {
  width: 100%;
  max-width: var(--content-max);
  min-height: var(--header-height);
  margin: 0 auto;
  padding: 0 var(--space-6);
  display: flex;
  align-items: center;
  gap: var(--space-3);
}

.page-container {
  width: 100%;
  max-width: var(--content-max);
  margin: 0 auto;
  padding: var(--space-7) var(--space-6);
}

.page-container-narrow {
  max-width: var(--reading-max);
}

.stack {
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.cluster {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-3);
}

.responsive-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 240px), 1fr));
  gap: var(--space-6);
}

.card,
.panel {
  background: var(--color-surface);
  border-radius: var(--radius-card);
}

.card {
  padding: var(--space-6);
  box-shadow: var(--shadow-card);
}

.panel {
  padding: var(--space-5);
  border: 1px solid var(--color-border-soft);
}

.page-title {
  margin: 0 0 var(--space-2);
  padding-bottom: 10px;
  border-bottom: 2px solid var(--color-primary);
  color: var(--color-text-strong);
  font-size: var(--font-size-page);
  font-weight: 700;
  line-height: 1.3;
}

.page-description {
  max-width: 70ch;
  margin: 0 0 var(--space-5);
  color: var(--color-text-secondary);
  font-size: var(--font-size-control);
  line-height: 1.55;
}

.section-heading {
  margin: 0;
  color: var(--color-text-strong);
  font-size: var(--font-size-section);
  font-weight: 700;
  line-height: 1.35;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  color: #555;
  font-size: var(--font-size-sm);
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.field-hint {
  margin: 0;
  color: var(--color-text-muted);
  font-size: 0.78rem;
  line-height: 1.4;
}

.input,
.textarea,
.select {
  display: block;
  width: 100%;
  padding: 9px 12px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-control);
  outline: none;
  background: var(--color-surface-control);
  color: var(--color-text);
  font-size: var(--font-size-control);
  transition: border-color var(--transition-standard),
    background var(--transition-standard), box-shadow var(--transition-fast);
}

.textarea {
  min-height: 7rem;
  resize: vertical;
  line-height: 1.55;
}

.select {
  cursor: pointer;
}

.input::placeholder,
.textarea::placeholder {
  color: var(--color-text-faint);
}

.input:focus,
.textarea:focus,
.select:focus {
  border-color: var(--color-primary);
  background: var(--color-surface);
}

.input:focus-visible,
.textarea:focus-visible,
.select:focus-visible,
button:focus-visible,
a:focus-visible,
[tabindex]:focus-visible {
  outline: none;
  box-shadow: var(--focus-ring);
}

.check-field {
  display: inline-flex;
  align-items: center;
  gap: var(--space-2);
  color: #334155;
  font-size: var(--font-size-control);
  cursor: pointer;
}

.check-field input {
  width: 1rem;
  height: 1rem;
  accent-color: var(--color-primary);
}

.button-primary,
.button-secondary,
.button-accent,
.button-danger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  min-height: 40px;
  padding: 9px 18px;
  border-radius: var(--radius-control);
  font-size: var(--font-size-control);
  font-weight: 700;
  line-height: 1.2;
  cursor: pointer;
  transition: background var(--transition-standard), color var(--transition-standard),
    border-color var(--transition-fast), box-shadow var(--transition-fast),
    opacity var(--transition-fast);
}

.button-primary {
  border: 1px solid var(--color-primary);
  background: var(--color-primary);
  color: #fff;
}

.button-primary:hover:not(:disabled) {
  border-color: var(--color-primary-hover);
  background: var(--color-primary-hover);
}

.button-secondary {
  border: 1px solid var(--color-border);
  background: #f1f3f5;
  color: #444;
  font-weight: 600;
}

.button-secondary:hover:not(:disabled) {
  background: #e5e7eb;
}

.button-accent {
  border: 1px solid var(--color-accent);
  background: var(--color-accent);
  color: #fff;
}

.button-accent:hover:not(:disabled) {
  border-color: var(--color-accent-hover);
  background: var(--color-accent-hover);
}

.button-danger {
  border: 1px solid var(--color-error-border);
  background: var(--color-surface);
  color: #b91c1c;
}

.button-danger:hover:not(:disabled) {
  background: var(--color-error-bg);
}

button:disabled,
.is-disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.is-loading {
  cursor: wait;
}

.table-container {
  width: 100%;
  overflow-x: auto;
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-field-shell);
  background: var(--color-surface);
}

.data-table {
  width: 100%;
  min-width: 640px;
  border-collapse: collapse;
  font-size: 0.88rem;
}

.data-table th,
.data-table td {
  padding: 10px 12px;
  border-bottom: 1px solid var(--color-border-soft);
  text-align: left;
  vertical-align: top;
}

.data-table th {
  color: var(--color-text-muted);
  background: var(--color-surface-subtle);
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.03em;
  text-transform: uppercase;
}

.data-table tbody tr:hover {
  background: var(--color-surface-subtle);
}

.data-table tr:last-child td {
  border-bottom: 0;
}

.tab-list {
  display: inline-flex;
  max-width: 100%;
  gap: var(--space-1);
  padding: var(--space-1);
  overflow-x: auto;
  border-radius: var(--radius-control);
  background: #f1f3f5;
}

.tab-button {
  flex: 0 0 auto;
  padding: 7px 16px;
  border: 0;
  border-radius: var(--radius-small);
  background: transparent;
  color: #555;
  font-size: 0.88rem;
  font-weight: 600;
  white-space: nowrap;
  cursor: pointer;
  transition: background var(--transition-fast), color var(--transition-fast);
}

.tab-button:hover:not(:disabled) {
  background: #e5e7eb;
  color: #222;
}

.tab-button[aria-selected="true"],
.tab-button.is-active {
  background: var(--color-surface);
  color: var(--color-text-strong);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
}

.status-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--space-1);
  padding: 2px 8px;
  border-radius: var(--radius-pill);
  background: var(--color-surface-muted);
  color: var(--color-text-secondary);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  line-height: 1.4;
  text-transform: uppercase;
}

.status-badge-success {
  background: #dcfce7;
  color: #166534;
}

.status-badge-warning {
  background: #fef9c3;
  color: #854d0e;
}

.status-badge-error {
  background: var(--color-error-bg);
  color: var(--color-error-text);
}

.status-badge-info {
  background: var(--color-info-bg);
  color: #0369a1;
}

.alert-success,
.alert-warning,
.alert-error,
.alert-info {
  padding: 10px 14px;
  border: 1px solid;
  border-radius: var(--radius-control);
  font-size: 0.88rem;
  line-height: 1.45;
}

.alert-success {
  border-color: var(--color-success-border);
  background: var(--color-success-bg);
  color: var(--color-success-text);
}

.alert-warning {
  border-color: var(--color-warning-border);
  background: var(--color-warning-bg);
  color: var(--color-warning-text);
}

.alert-error {
  border-color: var(--color-error-border);
  background: var(--color-error-bg);
  color: var(--color-error-text);
}

.alert-info {
  border-color: var(--color-info-border);
  background: var(--color-info-bg);
  color: var(--color-info-text);
}

.empty-state {
  display: flex;
  min-height: 220px;
  padding: var(--space-7);
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--space-3);
  border: 1px dashed var(--color-border-strong);
  border-radius: var(--radius-card);
  background: var(--color-surface-subtle);
  color: var(--color-text-muted);
  text-align: center;
}

.toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-1);
  padding: var(--space-1);
  border-radius: var(--radius-control);
  background: #f1f3f5;
}

.toolbar-button {
  min-width: 32px;
  min-height: 30px;
  padding: 4px 8px;
  border: 0;
  border-radius: 4px;
  background: transparent;
  color: #1e293b;
  cursor: pointer;
}

.toolbar-button:hover {
  background: #d8dee6;
}

.toolbar-button:active,
.toolbar-button[aria-pressed="true"] {
  background: #c4ccd6;
}

.expandable-row {
  overflow: hidden;
  border: 1px solid var(--color-border-soft);
  border-radius: var(--radius-control);
  background: var(--color-surface);
}

.expandable-row-summary {
  width: 100%;
  padding: 12px 14px;
  border: 0;
  background: var(--color-surface-subtle);
  color: var(--color-text);
  text-align: left;
  cursor: pointer;
}

.expandable-row-summary:hover {
  background: var(--color-surface-muted);
}

.expandable-row-details {
  padding: 14px;
  border-top: 1px solid var(--color-border-soft);
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-6);
  background: rgba(15, 23, 42, 0.55);
}

.modal-card {
  width: min(100%, 520px);
  max-height: calc(100vh - 48px);
  overflow: auto;
  padding: var(--space-7) var(--space-6);
  border-radius: var(--radius-modal);
  background: var(--color-surface);
  box-shadow: var(--shadow-modal);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.hide-mobile {
  display: initial;
}

@media (max-width: 900px) {
  .page-container {
    padding: var(--space-4);
  }

  .app-header-inner {
    padding: 0 var(--space-4);
  }

  .stack-mobile {
    grid-template-columns: 1fr !important;
  }
}

@media (max-width: 640px) {
  .hide-mobile {
    display: none !important;
  }

  .card,
  .panel {
    padding: var(--space-4);
  }

  .button-primary,
  .button-secondary,
  .button-accent,
  .button-danger {
    max-width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    scroll-behavior: auto !important;
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

## 5. Layout guidance

- Do not force a two-column form-and-editor layout into every project.
- Use top navigation for a small number of major peer sections.
- Use a sidebar when many persistent destinations need to remain visible.
- Use tabs for closely related views within one task or entity.
- Use a step-based or wizard layout for sequential processes with clear completion states.
- Use responsive card grids and horizontally scrollable tables for dashboards and reports.
- Use a split layout when users must compare or edit two related views simultaneously.
- On smaller screens, stack content, reduce outer padding, and prevent navigation overflow.
- Long top navigation should wrap, scroll horizontally, collapse, or become a menu.
- Preserve consistent container widths, spacing, cards, controls, typography, and colors even when layouts differ.
- Use `--content-max: 1400px` for broad workspaces and approximately 720–1000px for reading-heavy or focused workflows.
- Keep primary actions near the content they affect. A fixed action bar is appropriate only when users edit long forms and must retain access to Save or Submit.

## 6. Component style recipes

### Application header

Use for global identity and primary navigation. Apply `.app-header` and `.app-header-inner`. Keep the dark navy surface and restrained header shadow. Active navigation may use the primary red. Ensure small-screen navigation does not overflow. Header controls need visible `:focus-visible` treatment.

### Sidebar

Use when the application has many persistent destinations. Use `--color-navy` for a strong sidebar or a white `.panel` for a quieter one. Selected destinations should combine a surface/color change with weight or an indicator; never rely on color alone. Collapse or move the sidebar behind a menu on small screens.

### Page title

Use `.page-title` for the main heading and `.page-description` for concise supporting text. The red bottom rule is part of the recognizable visual language but can be omitted where a dense dashboard heading would be clearer.

### Card

Use `.card` for major grouped content and `.panel` for nested or quieter regions. Avoid nesting many shadowed cards. Prefer one shadowed outer card with bordered inner panels.

### Form field

Use `.field`, `.field-label`, `.input`, `.textarea`, `.select`, and `.field-hint`. Connect labels with `for`/`id`. Focus uses a red border and visible focus ring. Validation should add a message and semantic state, not only a red border. Disabled controls should remain readable.

### Primary action

Use `.button-primary` for the single most important action in a region. Hover darkens to `--color-primary-hover`; focus shows the shared ring; disabled state uses reduced opacity and `not-allowed`.

### Secondary action

Use `.button-secondary` for cancel, reset, back, reload, or lower-priority actions. It uses a pale neutral surface and border. It must not visually compete with the primary action.

### Purple alternative action

Use `.button-accent` for a distinct alternate workflow or secondary feature—not merely to decorate a second primary button. Hover uses `--color-accent-hover`.

### Destructive action

Use `.button-danger` for delete, clear, remove, or irreversible actions. Confirm destructive actions when consequences are significant. Use explicit text or an accessible label; do not depend on a red icon alone.

### Status badge

Use `.status-badge` plus a semantic modifier. Include readable text or an icon and text. Badges suit compact state labels; use alerts for messages requiring attention.

### Alert banner

Use `.alert-success`, `.alert-warning`, `.alert-error`, or `.alert-info`. Include a concise message and, when useful, a recovery action. Do not use color as the only signal.

### Tab group

Use `.tab-list` and `.tab-button`, with `aria-selected="true"` on the active tab. Implement arrow-key behavior when using ARIA tab semantics. Allow horizontal scrolling on small screens.

### Data table

Wrap `.data-table` in `.table-container`. Keep headers compact and slate colored. Use row hover only as assistance, not as the sole indication that a row is interactive. On mobile, allow horizontal scrolling or switch to cards when comparison across columns is not essential.

### Expandable report row

Use `.expandable-row`, `.expandable-row-summary`, and `.expandable-row-details` for dense logs, reports, or API details. The summary must be a real button with `aria-expanded`. Preserve keyboard operation and include an explicit expand/collapse indicator.

### Empty state

Use `.empty-state` when a region has no data. State why it is empty and provide the next useful action where appropriate. Avoid decorative emptiness that consumes excessive space.

### Modal

Use `.modal-backdrop` and `.modal-card` for blocking decisions or meaningful long operations. Move focus into the modal, contain focus, restore it on close, support Escape where dismissal is safe, and prevent background interaction.

### Loading state

For short local work, disable only the affected control and show concise inline progress. For a meaningful operation that blocks the whole workflow, use the moving loader below. Loading text should describe the operation. Success, warning, and error results should be announced and displayed after completion.

## 7. Moving loader animation

Use this loader for operations that take long enough for users to wonder whether the application is still working: large generation jobs, imports, exports, multi-stage processing, report builds, or remote operations. Do not use it for quick field validation, ordinary navigation, or actions that usually complete almost instantly.

The illustration is entirely CSS-based. It needs no image files and no animation package.

### `MovingLoaderOverlay.jsx`

```jsx
import { useEffect, useId, useRef, useState } from "react";
import styles from "./MovingLoaderOverlay.module.css";

function usePrefersReducedMotion() {
  const [reduced, setReduced] = useState(false);

  useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReduced(query.matches);
    update();
    query.addEventListener?.("change", update);
    return () => query.removeEventListener?.("change", update);
  }, []);

  return reduced;
}

export default function MovingLoaderOverlay({
  open,
  title = "WORKING",
  subtitle = "Please wait while the operation completes.",
  cancelLabel = "Cancel",
  onCancel,
  showCancel = true,
}) {
  const [dots, setDots] = useState(0);
  const reducedMotion = usePrefersReducedMotion();
  const titleId = useId();
  const subtitleId = useId();
  const cardRef = useRef(null);
  const cancelRef = useRef(null);

  const canCancel = showCancel && typeof onCancel === "function";

  useEffect(() => {
    if (!open || reducedMotion) {
      setDots(0);
      return undefined;
    }

    const timer = window.setInterval(() => {
      setDots((current) => (current + 1) % 4);
    }, 450);

    return () => window.clearInterval(timer);
  }, [open, reducedMotion]);

  useEffect(() => {
    if (!open) return undefined;

    const previouslyFocused = document.activeElement;
    const focusTarget = canCancel ? cancelRef.current : cardRef.current;
    focusTarget?.focus();

    function handleKeyDown(event) {
      if (event.key === "Escape" && canCancel) onCancel();
    }

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      previouslyFocused?.focus?.();
    };
  }, [open, canCancel, onCancel]);

  if (!open) return null;

  return (
    <div className={styles.backdrop}>
      <div
        ref={cardRef}
        className={styles.card}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={subtitleId}
        tabIndex={-1}
      >
        <div className={styles.scene} aria-hidden="true">
          <div className={styles.ground} />

          <div className={styles.house}>
            <div className={styles.houseRoof} />
            <div className={styles.houseWall}>
              <div className={styles.houseWindow} />
              <div className={styles.houseWindow} />
              <div className={styles.houseDoor} />
            </div>
          </div>

          <div className={styles.truck}>
            <div className={styles.truckBody}>
              <div className={styles.truckCargo}>
                <div className={styles.truckRearDoor} />
              </div>
              <div className={styles.truckCab}>
                <div className={styles.truckCabWindow} />
              </div>
            </div>
            <div className={styles.truckWheel} />
            <div className={`${styles.truckWheel} ${styles.truckWheelFront}`} />
          </div>

          <div className={styles.figureTrack}>
            <div className={styles.mover}>
              <div className={styles.figureRow}>
                <div className={styles.figureColumn}>
                  <div className={styles.head} />
                  <div className={styles.torso} />
                  <div className={styles.legs}>
                    <div className={styles.leg} />
                    <div className={styles.leg} />
                  </div>
                </div>
                <div className={styles.box} />
              </div>
            </div>
          </div>
        </div>

        <p id={titleId} className={styles.working}>
          {title}
          <span aria-hidden="true">
            {reducedMotion ? "…" : ".".repeat(dots)}
          </span>
        </p>
        <p id={subtitleId} className={styles.subtitle} aria-live="polite">
          {subtitle}
        </p>

        {canCancel && (
          <button
            ref={cancelRef}
            type="button"
            className={styles.cancelButton}
            onClick={onCancel}
          >
            {cancelLabel}
          </button>
        )}
      </div>
    </div>
  );
}
```

### `MovingLoaderOverlay.module.css`

```css
.backdrop {
  position: fixed;
  inset: 0;
  z-index: var(--z-modal, 200);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  background: rgba(15, 23, 42, 0.55);
}

.card {
  width: min(100%, 420px);
  padding: 28px 24px 22px;
  border-radius: var(--radius-modal, 16px);
  outline: none;
  background: #fff;
  box-shadow: var(--shadow-modal, 0 20px 50px rgba(0, 0, 0, 0.2));
  text-align: center;
}

.card:focus-visible {
  box-shadow: var(--shadow-modal, 0 20px 50px rgba(0, 0, 0, 0.2)),
    var(--focus-ring, 0 0 0 3px rgba(230, 57, 70, 0.28));
}

.scene {
  position: relative;
  width: 100%;
  max-width: 360px;
  height: 120px;
  margin: 0 auto 20px;
  overflow: hidden;
}

.ground {
  position: absolute;
  right: 5%;
  bottom: 11px;
  left: 5%;
  height: 4px;
  border-radius: 2px;
  background: linear-gradient(90deg, #94a3b8, #64748b, #94a3b8);
}

.house {
  position: absolute;
  bottom: 11px;
  left: 4%;
  z-index: 2;
  display: flex;
  width: 56px;
  flex-direction: column;
  align-items: center;
}

.houseRoof {
  width: 64px;
  height: 22px;
  border-bottom: 2px solid #7f1d1d;
  background: #b91c1c;
  box-shadow: inset 0 -2px 0 rgba(0, 0, 0, 0.12);
  clip-path: polygon(8% 100%, 50% 0, 92% 100%);
}

.houseWall {
  position: relative;
  width: 50px;
  height: 36px;
  margin-top: -1px;
  border: 2px solid #92400e;
  border-top: 0;
  border-radius: 0 0 4px 4px;
  background: linear-gradient(180deg, #fef9c3 0%, #fde68a 100%);
  box-shadow: inset 0 2px 0 rgba(255, 255, 255, 0.35);
}

.houseWindow {
  position: absolute;
  top: 7px;
  width: 9px;
  height: 9px;
  border: 1px solid #0369a1;
  border-radius: 1px;
  background: #bae6fd;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.5);
}

.houseWindow:first-of-type {
  left: 6px;
}

.houseWindow:nth-of-type(2) {
  right: 6px;
}

.houseDoor {
  position: absolute;
  bottom: 0;
  left: 50%;
  width: 13px;
  height: 17px;
  transform: translateX(-50%);
  border: 1px solid #451a03;
  border-radius: 2px 2px 0 0;
  background: #78350f;
}

.houseDoor::after {
  position: absolute;
  top: 50%;
  right: 2px;
  width: 2px;
  height: 2px;
  transform: translateY(-50%);
  border-radius: 50%;
  background: #fbbf24;
  content: "";
}

.truck {
  position: absolute;
  right: 2%;
  bottom: 11px;
  z-index: 3;
  width: 88px;
  height: 40px;
}

.truckBody {
  position: absolute;
  right: 0;
  bottom: 5px;
  left: 0;
  display: flex;
  align-items: flex-end;
}

.truckCargo {
  position: relative;
  width: 52px;
  height: 32px;
  border: 2px solid #0f172a;
  border-radius: 4px 2px 2px 4px;
  background: #334155;
}

.truckRearDoor {
  position: absolute;
  top: 7px;
  bottom: 7px;
  left: 5px;
  width: 14px;
  border: 1px solid #020617;
  border-radius: 2px;
  background: repeating-linear-gradient(
    to bottom,
    #64748b 0 3px,
    #1e293b 3px 5px
  );
  opacity: 0.85;
}

.truckCab {
  position: relative;
  width: 28px;
  height: 22px;
  border: 2px solid #0f172a;
  border-left: 0;
  border-radius: 0 8px 6px 0;
  background: #1e3a8a;
}

.truckCabWindow {
  position: absolute;
  top: 5px;
  right: 5px;
  width: 12px;
  height: 9px;
  border: 1px solid #0f172a;
  border-radius: 2px;
  background: #93c5fd;
}

.truckWheel {
  position: absolute;
  bottom: 0;
  left: 14px;
  width: 12px;
  height: 12px;
  border: 2px solid #475569;
  border-radius: 50%;
  background: #0f172a;
}

.truckWheelFront {
  right: 8px;
  left: auto;
}

.figureTrack {
  position: absolute;
  bottom: 11px;
  left: 0;
  z-index: 4;
  width: 100%;
  height: 44px;
  pointer-events: none;
}

.mover {
  position: absolute;
  bottom: 0;
  left: 16%;
  width: 32px;
  height: 40px;
  animation: truckRoundTrip 4s ease-in-out infinite;
}

@keyframes truckRoundTrip {
  0% {
    left: 16%;
  }
  40%,
  52% {
    left: calc(100% - 116px);
  }
  100% {
    left: 16%;
  }
}

.figureRow {
  display: flex;
  align-items: flex-end;
  width: 100%;
  height: 100%;
}

.figureColumn {
  position: relative;
  display: flex;
  flex-shrink: 0;
  flex-direction: column;
  align-items: center;
}

.head {
  width: 10px;
  height: 10px;
  margin-bottom: 1px;
  border-radius: 50%;
  background: #0f172a;
}

.torso {
  width: 3px;
  height: 16px;
  border-radius: 1px;
  background: #0f172a;
}

.legs {
  display: flex;
  justify-content: center;
  gap: 4px;
}

.leg {
  width: 3px;
  height: 10px;
  border-radius: 1px;
  background: #0f172a;
}

.box {
  position: relative;
  z-index: 2;
  width: 16px;
  height: 12px;
  margin-bottom: 9px;
  margin-left: 1px;
  flex-shrink: 0;
  transform-origin: 20% 80%;
  border: 2px solid #9a3412;
  border-radius: 2px;
  background: linear-gradient(145deg, #fdba74 0%, #ea580c 100%);
  box-shadow: 1px 1px 0 rgba(0, 0, 0, 0.12);
  animation: boxIntoTruck 4s ease-in-out infinite;
}

@keyframes boxIntoTruck {
  0%,
  38% {
    transform: translate(0, 0) scale(1);
    opacity: 1;
  }
  41% {
    transform: translate(1px, 0) scale(1);
    opacity: 1;
  }
  48% {
    transform: translate(14px, -10px) scale(0.55);
    opacity: 0.65;
  }
  51%,
  99.99% {
    transform: translate(22px, -15px) scale(0.22);
    opacity: 0;
  }
  100% {
    transform: translate(0, 0) scale(1);
    opacity: 0;
  }
}

.working {
  margin: 0 0 8px;
  color: #0d1b2a;
  font-family: var(--font-mono, ui-monospace, "Cascadia Code", monospace);
  font-size: 1.05rem;
  font-weight: 800;
  letter-spacing: 0.12em;
}

.subtitle {
  margin: 0 0 18px;
  color: #64748b;
  font-size: 0.82rem;
  line-height: 1.4;
}

.cancelButton {
  width: 100%;
  padding: 11px 16px;
  border: 2px solid var(--color-primary, #e63946);
  border-radius: 10px;
  background: #fff;
  color: var(--color-primary-hover, #c1121f);
  font-size: 0.92rem;
  font-weight: 700;
  cursor: pointer;
  transition: background 150ms ease, color 150ms ease, box-shadow 150ms ease;
}

.cancelButton:hover {
  background: #fee2e2;
}

.cancelButton:focus-visible {
  outline: none;
  box-shadow: var(--focus-ring, 0 0 0 3px rgba(230, 57, 70, 0.28));
}

@media (max-width: 360px) {
  .backdrop {
    padding: 12px;
  }

  .card {
    padding-right: 16px;
    padding-left: 16px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .mover {
    left: 46%;
    animation: none;
  }

  .box {
    animation: none;
  }
}
```

The parent operation owns cancellation. `onCancel` should abort a request, signal a worker, or call the relevant application service; the overlay does not assume any backend technology. If cancellation is unsafe or unsupported, pass `showCancel={false}` or omit `onCancel`.

For a production modal with multiple focusable controls, add a tested focus trap. This single-action version moves focus into the dialog, supports Escape when cancellation is available, and restores prior focus on unmount.

## 8. Font and asset rules

- Use the existing system-font stack defined by `--font-sans`.
- Do not package or redistribute operating-system font files.
- No custom font download is required.
- Use the monospace stack only for compact machine/status text where appropriate.
- The moving loader requires no image asset.
- Do not carry unused Vite or React starter assets into new projects.
- Select project-specific logos, favicons, illustrations, and content images separately for each project.
- Verify the ownership and license of every project-specific image before redistribution.
- Platform-rendered emoji and Unicode symbols vary by operating system; use accessible text or a chosen icon system when consistent icon appearance matters.

## 9. Accessibility and responsive improvements

Future projects should improve on the source patterns in these ways:

- Keep visible keyboard focus rings and use `:focus-visible`.
- Maintain sufficient text, border, and control contrast.
- Use native buttons for actions and native links for navigation.
- Connect every visible field label to its input.
- Add descriptive validation messages and associate them with fields.
- Keep all controls keyboard accessible.
- Move focus into modals, contain it, and restore it after close.
- Support Escape when dismissing a modal is safe.
- Respect `prefers-reduced-motion`.
- Prevent navigation overflow through wrapping, scrolling, collapsing, or a menu.
- Wrap wide tables in an overflow container.
- Stack columns and reduce outer padding on mobile.
- Use meaningful loading text that states what is happening.
- Do not use color as the only status indicator; include text, icons, or both.
- Use `aria-live` selectively for meaningful asynchronous updates, not rapidly changing decorative dots.
- Maintain touch targets near 40–44px where practical.
- Test at narrow mobile widths, keyboard-only operation, zoomed text, and high contrast settings.

## 10. Rules for adapting the design

When requirements compete, use this priority order:

1. Correct functionality
2. Clear user workflow
3. Accessibility
4. Responsive behavior
5. Consistent visual identity
6. Exact resemblance to any earlier application

Future agents may create new components, navigation systems, data visualizations, and layouts when needed. New work should consume the supplied tokens and follow the same design language so the result remains visually recognizable without being constrained to one old structure.

Avoid introducing a new design framework merely for convenience if it visibly replaces this identity. If a component library is required, theme it with these tokens and verify its focus, spacing, radius, and status treatments.

## 11. Quick instructions for a future project

1. Read this entire reference.
2. Add or adapt the shared theme CSS near the application entry point.
3. Build the layout that best suits the new application.
4. Use the defined colors, typography, cards, forms, buttons, status styles, and spacing.
5. Add `MovingLoaderOverlay` for meaningful long-running actions.
6. Do not copy source-specific business logic.
7. Do not force an unsuitable layout.
8. Keep the visual identity consistent while prioritizing clarity and accessibility.

## 12. Source inventory

This reference was built from the following active project files:

- `frontend/src/index.css` — global reset, system font stack, page background, primary text, and root sizing.
- `frontend/src/App.module.css` — navy sticky header, navigation treatment, red active state, broad container width, two-column-to-stacked breakpoint, and information/error banners.
- `frontend/src/components/FieldShell.jsx` — reusable field composition and label/header/hint structure.
- `frontend/src/components/FieldShell.module.css` — field shell borders, uppercase labels, focus-within behavior, control typography, placeholders, and hints.
- `frontend/src/components/TextField.jsx` — generic controlled input and textarea API.
- `frontend/src/components/SelectField.jsx` — generic controlled select API.
- `frontend/src/components/GeneratingOverlay.jsx` — modal semantics, animated working text, cancellation boundary, and house/mover/truck scene structure.
- `frontend/src/components/GeneratingOverlay.module.css` — complete CSS illustration, synchronized four-second mover/box animations, modal styling, and loader typography.
- `frontend/src/components/BlogForm.module.css` — active primary, secondary, and alternate button styles; sticky card; fields; dropdown; disabled states.
- `frontend/src/components/BlogEditor.module.css` — cards, compact tabs, section panels, sticky controls, upload action, status banners, and narrow-screen editor behavior.
- `frontend/src/components/BlogPreview.module.css` — content typography, headings, featured-image treatment, links, and CTA styling.
- `frontend/src/components/FeaturedImagePicker.module.css` — responsive card grid, selected state, dashed/empty choices, metadata, and warning/information treatments.
- `frontend/src/components/BlogTopicGenerator.module.css` — page titles, action panels, tables, selected cards, badges, and copy feedback.
- `frontend/src/components/AutoTopicScheduler.module.css` — forms, badges, expandable history rows, detail grids, and compact report tables.
- `frontend/src/components/OneClickAutoBlog.module.css` — purple alternate action, progress states, result cards, and status treatments.
- `frontend/src/components/OptionsManager.module.css` — option groups, responsive form grids, destructive controls, and fixed action-bar treatment.
- `frontend/src/components/ApiUsage.module.css` — summary cards, semantic status badges, expandable rows, cost metadata, and responsive card grids.
- `frontend/src/components/RichTextField.module.css` — compact toolbar hover/active states and editable text treatment.

Unused starter assets, unrelated application logic, and inactive CSS selectors were intentionally not incorporated into the reusable design.
