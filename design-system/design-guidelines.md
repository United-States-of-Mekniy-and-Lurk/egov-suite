# MKLU Design System — Accessibility & Guidelines

> See [design.md](design.md) for token values. See [design-components.md](design-components.md) for component specs.

These guidelines govern how the MKLU Design System is used — what is permitted, what is prohibited, and what the system must do to remain accessible to all citizens of Mekniy and Lurk, regardless of ability, language, or device.

The system must also be **clearly distinguishable** from real-world government design systems (GOV.UK, DSFR, Bootstrap Italia) to avoid any perception of impersonation or phishing. This is a legal and ethical requirement, not just a design preference.

---

## Accessibility

### Contrast Requirements

All text must meet WCAG 2.1 AA as a minimum; WCAG AAA is the target for body copy.

| Requirement | Minimum ratio | Applies to |
|------------|--------------|------------|
| Normal text (< 18px) | 4.5:1 | Body, labels, captions |
| Large text (≥ 18px or 14px bold) | 3:1 | Headings, Display |
| UI components and icons | 3:1 | Borders, icon fills, button outlines |
| Focus indicators | 3:1 | Focus ring against adjacent colours |

**Key verified ratios (light mode):**

| Pair | Ratio | Pass |
|------|-------|------|
| `#2F2F2F` on `#F8F8F8` | 12.6:1 | AAA |
| `#0072CC` on `#F8F8F8` | 4.7:1 | AA (link) |
| `#F8F8F8` on `#2F2F2F` | 12.6:1 | AAA (reversed) |
| `#F8F8F8` on `#0094FF` | 3.1:1 | AA large / UI only |
| `#FFD700` on `#2F2F2F` | 9.2:1 | AAA (focus ring) |
| `#C62828` on `#F8F8F8` | 5.9:1 | AA (error text) |
| `#2E7D32` on `#F8F8F8` | 5.1:1 | AA (success text) |
| `#CC7700` on `#F8F8F8` | 3.2:1 | AA large only → use `#995700` for small text |

> `#0094FF` (Lurkish Sky) on white fails 4.5:1 for small text. **Always darken to `#0072CC` when used as text.** Use `#0094FF` for fills, borders, and decorative elements only.

### Touch Targets

| Context | Minimum size | Recommended |
|---------|-------------|-------------|
| Interactive elements | 44×44px | 48×48px |
| Inline links in prose | — | Ensure 8px padding above/below |
| Compact/dense UI | 32×32px (with 8px clearance) | Avoid dense layouts on touch |
| Icon-only buttons | 44×44px tap area | Always include `aria-label` |

### Keyboard Navigation

| Key | Action |
|-----|--------|
| `Tab` | Move focus forward |
| `Shift+Tab` | Move focus backward |
| `Enter` / `Space` | Activate button or link |
| `Escape` | Close modal, dialog, dropdown |
| `Arrow keys` | Navigate within menus, radio groups, tabs |
| `Home` / `End` | Jump to first/last item in list or table |
| `Page Up` / `Page Down` | Scroll or paginate |

Focus must always be visible. The yellow focus ring (`#FFD700`, 3px, 2px offset) is mandatory and must never be suppressed via `outline: none` without an equally visible custom replacement.

### Assistive Technology

- All images must have descriptive `alt` text; decorative images use `alt=""`.
- National symbols used decoratively (manticore watermark) must be `aria-hidden="true"`.
- Form inputs must have visible `<label>` elements — never `placeholder` as the sole label.
- Error messages must be associated via `aria-describedby` and announced by screen readers.
- Page language must be declared: `<html lang="mis">` for Nissiian, `<html lang="cr">` for Loringian (Canadian Syllabics), `<html lang="cs">` for Czech, `<html lang="en">` for English. Use `lang` attribute on individual elements when mixed within a page.
- Use ARIA landmark roles: `<header role="banner">`, `<nav>`, `<main>`, `<footer role="contentinfo">`.
- Skip-to-content link must be the first focusable element on every page.

---

## Language & Unicode Support

Mekniy-Lurk is officially multilingual. The design system must support all languages without layout degradation.

| Language | Script | Unicode range | Font |
|----------|--------|--------------|------|
| Nissiian | Latin Extended | U+0000–U+024F + macrons | Noto Sans |
| Loringian | Canadian Syllabics | U+1400–U+167F | Noto Sans Canadian Aboriginal |
| Czech | Latin | U+0000–U+017E | Noto Sans |
| English | Latin | U+0000–U+007F | Noto Sans |

**Loading recommendation:**
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link href="https://fonts.googleapis.com/css2?family=Source+Serif+4:wght@300;400;600&family=Noto+Sans:wght@400;600&family=Noto+Sans+Canadian+Aboriginal&family=Noto+Sans+Mono&display=swap" rel="stylesheet">
```

- Always declare character encoding: `<meta charset="UTF-8">`.
- Text containers must handle right-to-left and mixed-script content gracefully — use `unicode-bidi: plaintext` where needed.
- Do not use fixed-width containers that clip extended Latin characters (ā ē ū ŋ x̄) or syllabic symbols.

---

## Gestures

| Gesture | Use |
|---------|-----|
| Tap | Activate button, link, checkbox |
| Double tap | Zoom (reserved for browser; do not override) |
| Long press | Reveal contextual options (use sparingly; always have alternative) |
| Scroll | Navigate page or list content |
| Swipe (horizontal) | Navigate carousels, dismiss bottom sheets |
| Swipe (vertical) | Scroll; dismiss drawers only if swipe target is explicit |
| Drag | Reorder list items only; never required for core functions |
| Pinch | Zoom (browser-native; do not disable) |

Never require a gesture as the **only** way to perform an action. Always provide a tap/click alternative.

---

## Content Design

### Tone of Voice

The MKLU design system serves government-adjacent functions, but Mekniy-Lurk is an art-state. The tone is:

- **Formal but not cold** — authoritative without being intimidating
- **Clear before clever** — Dadaist sensibility lives in design, not in ambiguous copy
- **Multilingual-aware** — avoid idioms that fail in translation; use plain-language constructs
- **Inclusive** — write for citizens, not experts

### Writing Rules

| Rule | Detail |
|------|--------|
| Sentence case | Headlines, buttons, labels — never ALL CAPS (except abbreviations like CRN) |
| Active voice | "Submit your application" not "Your application should be submitted" |
| Second person | Address the user as "you" in interfaces |
| Line length | 65–75 characters for prose; 45–55 for narrow columns |
| Button labels | 1–4 words; verb-first ("Submit", "Download decree", "View document") |
| Error messages | Explain what happened and how to fix it; no blame language |
| Dates | Use unambiguous formats: `20 September 2009` or `2009-09-20` (ISO) — not `09/20/09` |
| Currency | `1 CRN`, `150 CRN` — symbol after number |

---

## Do's and Don'ts

### Color

- **Do** use `#0072CC` (darkened Lurkish Sky) for text links on white backgrounds
- **Do** use `#0094FF` for fills, borders, and decorative elements
- **Do** apply the yellow focus ring (`#FFD700`) consistently across all interactive elements
- **Don't** use `#0094FF` as small text on `#F8F8F8` — contrast fails 4.5:1
- **Don't** add colours outside the token system without consultation
- **Don't** use semantic colours (red, green) for decoration — they carry meaning

### National Symbols

- **Do** use the Manticore Mossaesa on dark (`#2F2F2F`) or blue (`#0094FF`) surfaces only — it is white-fill
- **Do** recolour the manticore to `#2F2F2F` when placing on light surfaces
- **Do** use the Grand Mekniyan Seal exclusively on official documents and formal page headers
- **Don't** crop, rotate, stretch, or apply effects to national symbols
- **Don't** use the flag as a background texture or decorative fill
- **Don't** place any national symbol on a busy or photographic background without a clear backing surface

### Differentiation from Real Government Systems

- **Do** ensure the header wordmark reads "United States of Mekniy and Lurk" or its Nissiian equivalent — not just "Government" or "GOV"
- **Do** include a visible disclaimer on formal-looking document pages: *"This is an official document of the United States of Mekniy and Lurk, a micronation."*
- **Don't** replicate the GOV.UK black header + crown logo combination exactly
- **Don't** use the French Marianne or Italian Republic symbols
- **Don't** make pages that could be mistaken for a phishing replica of a real national government service

### Typography

- **Do** use Source Serif 4 for headings — it carries formal weight without mimicking any specific government system
- **Do** maintain minimum 16px body text in all interfaces
- **Do** declare `lang` attributes to ensure correct font selection per language
- **Don't** use only sans-serif throughout — the serif/sans pairing is part of the system identity
- **Don't** fake small caps with `font-size` reduction; use `font-variant: small-caps` or a proper weight
- **Don't** justify body text — it creates uneven spacing that harms readability in mixed-script content

### Shape & Elevation

- **Do** keep corners square or minimally rounded on official/document surfaces (radius 0–4px)
- **Do** use elevation sparingly — flat surfaces convey authority
- **Don't** apply elevation to headers, footers, or document body areas
- **Don't** use `border-radius > 8px` on primary content containers

### Layout & Spacing

- **Do** respect the 8px spacing grid
- **Do** provide sufficient white space — Hanunkx Pure (`#F8F8F8`) space is intentional and meaningful
- **Don't** crowd interactive elements — maintain 8px minimum between adjacent tap targets
- **Don't** use fixed pixel widths on text containers — use max-width with percentage or viewport units

### Motion

- **Do** keep transitions under 300ms for functional UI (dropdowns, modals)
- **Do** use `prefers-reduced-motion` to disable or shorten all animations
- **Don't** animate on initial page load for critical content
- **Don't** use looping or pulsing animations outside explicit loading states

### Creative Elements (Dadaist Identity)

- **Do** use the diagonal flag-geometry motif as a decorative section divider (e.g., `clip-path: polygon(0 0, 100% 0, 100% 85%, 0 100%)` on section breaks)
- **Do** use the manticore as a subtle background watermark at 4–6% opacity on brand surfaces
- **Don't** let creative decoration interfere with reading or interaction — decoration must not touch text, inputs, or focus rings
- **Don't** apply the creative motifs to error messages, form validation, or emergency notifications

---

## Print & Document Guidelines

### Paper Formats

| Format | Size | Use |
|--------|------|-----|
| A4 | 210 × 297mm | Decrees, letters, certificates |
| A5 | 148 × 210mm | Certificates, internal memos |
| Passport booklet | 125 × 88mm (ICAO TD3) | Passport covers and pages |
| Envelope DL | 110 × 220mm | Official correspondence |

### Print Margins

| Context | Margin |
|---------|--------|
| A4 decree / letter | 25mm all sides |
| Certificate | 20mm all sides |
| Passport data page | 4mm all sides |

### Print Colors

- Use `#2F2F2F` for all body text (prints richer than pure black on most printers)
- Use `#0094FF` sparingly in print — verify with actual printer; specify CMYK equivalent `C:100 M:42 Y:0 K:0` (approximate)
- Avoid semantic colour fills in print documents — use borders and labels instead
- Include a greyscale fallback: documents must be readable when printed in black and white
