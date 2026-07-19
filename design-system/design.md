# MKLU Design System — Token Reference

> Always read this file first. For component specs see [design-components.md](design-components.md). For accessibility and do's/don'ts see [design-guidelines.md](design-guidelines.md).

The MKLU Design System is the official visual language of the United States of Mekniy and Lurk. It draws from Central European government design traditions (GOV.UK, Bootstrap Italia) while expressing the nation's Dadaist-informed art-state identity. The primary typeface pairing is **Source Serif 4** (display/headings) with **Noto Sans** (body), built on an **8px spacing grid** with full Unicode support including Canadian Syllabics (Loringian, U+1400–U+167F) and Latin Extended characters for Nissiian (ā, ē, ū, x̄). The three national colours — Mekniyan Grey, Hanunkx Pure, and Lurkish Sky — anchor all derived tokens.

A distinctive creative thread runs through the system: the diagonal geometry of the flag is echoed in section dividers and decorative elements; the Manticore Mossaesa appears as a subtle emblem on official surfaces; and the design deliberately avoids mimicking any single government system, ensuring legal and perceptual distance from official state entities.

---

## Colors

### National Core

| Token | Name | Hex | Usage |
|-------|------|-----|-------|
| `mklu.color.grey` | Mekniyan Grey | `#2F2F2F` | Dark surfaces, primary text, official headers |
| `mklu.color.white` | Hanunkx Pure | `#F8F8F8` | Page background, light surfaces |
| `mklu.color.blue` | Lurkish Sky | `#0094FF` | Interactive elements, brand accent, links |

### Text

| Token | Light mode | Dark mode | Usage |
|-------|-----------|-----------|-------|
| `mklu.color.text.primary` | `#2F2F2F` | `#F0F0F0` | Body copy, headings |
| `mklu.color.text.secondary` | `#595959` | `#B0B0B0` | Captions, metadata, helper text |
| `mklu.color.text.disabled` | `#9B9B9B` | `#666666` | Inoperable controls |
| `mklu.color.text.on-brand` | `#F8F8F8` | `#F8F8F8` | Text on dark/blue surfaces |
| `mklu.color.text.link` | `#0072CC` | `#5BBBFF` | Default link (darkened for 4.5:1 on white) |
| `mklu.color.text.link-visited` | `#5A3EA8` | `#9B84E0` | Visited links |
| `mklu.color.text.link-hover` | `#005299` | `#82CFFF` | Link hover |

### Surface

| Token | Light mode | Dark mode | Usage |
|-------|-----------|-----------|-------|
| `mklu.color.surface.page` | `#F8F8F8` | `#1A1A1A` | Page background |
| `mklu.color.surface.raised` | `#FFFFFF` | `#252525` | Cards, panels |
| `mklu.color.surface.sunken` | `#EFEFEF` | `#141414` | Input backgrounds, code blocks |
| `mklu.color.surface.brand` | `#2F2F2F` | `#1A1A1A` | Official header bar, footer |
| `mklu.color.surface.accent` | `#0094FF` | `#0094FF` | Accent fills, highlighted rows |
| `mklu.color.surface.overlay` | `rgb(47 47 47 / .6)` | `rgb(0 0 0 / .7)` | Modal backdrop |

### Interactive (Lurkish Sky derivatives)

| Token | Hex | Usage |
|-------|-----|-------|
| `mklu.color.interactive.default` | `#0094FF` | Button fill, active tab |
| `mklu.color.interactive.hover` | `#007ACC` | Hover — darkened 15% |
| `mklu.color.interactive.active` | `#005FA3` | Pressed — darkened 28% |
| `mklu.color.interactive.subtle` | `#E6F4FF` | Tinted backgrounds, selected rows |
| `mklu.color.interactive.disabled` | `#8EC8FF` | Disabled interactive fill |
| `mklu.color.focus` | `#FFD700` | Focus ring — 3px solid, 2px offset |

### Border

| Token | Hex | Usage |
|-------|-----|-------|
| `mklu.color.border.default` | `#C8C8C8` | Dividers, table lines, card outlines |
| `mklu.color.border.strong` | `#2F2F2F` | Input borders, strong separators |
| `mklu.color.border.interactive` | `#0094FF` | Focused input, active outline button |
| `mklu.color.border.error` | `#C62828` | Invalid input |

### Semantic / Status

All semantic colours are derived from the national palette or chosen to be perceptually distinct and WCAG AA compliant.

| Role | Token | Surface | On-Surface | Notes |
|------|-------|---------|------------|-------|
| Error | `mklu.color.semantic.error` | `#FFF0F0` | `#C62828` | Warm red; family-distinct from blue |
| Success | `mklu.color.semantic.success` | `#F0FFF4` | `#2E7D32` | Forest green; accessible on white |
| Warning | `mklu.color.semantic.warning` | `#FFF8E1` | `#CC7700` | Amber; distinct from grey and blue |
| Info | `mklu.color.semantic.info` | `#E6F4FF` | `#0072CC` | Lurkish Sky darkened for text use |

---

## Typography

Both typefaces are free and open-source under the SIL Open Font Licence. Source Serif 4 is available via Google Fonts; Noto Sans and its language extensions via Google Fonts and fonts.google.com/noto.

**Heading stack:** `'Source Serif 4', 'Noto Serif', Georgia, serif`
**Body stack:** `'Noto Sans', 'Noto Sans Canadian Aboriginal', system-ui, -apple-system, sans-serif`
**Mono stack:** `'Noto Sans Mono', 'Courier New', monospace`

> **Language notes:** Noto Sans covers Nissiian Latin Extended characters (ā ē ū x̄ ŋ and macron variants). For Loringian, load `Noto Sans Canadian Aboriginal` which covers the Unified Canadian Aboriginal Syllabics block (U+1400–U+167F). Always declare `lang` attributes on HTML elements to enable correct font selection and screen reader pronunciation.

| Style | Token | Size | Weight | Line height | Letter spacing | Family |
|-------|-------|------|--------|-------------|---------------|--------|
| Display Large | `type.display-lg` | 48px / 3rem | 300 | 1.17 (56px) | −0.5px | Serif |
| Display Medium | `type.display-md` | 36px / 2.25rem | 400 | 1.22 (44px) | −0.25px | Serif |
| Heading 1 | `type.h1` | 32px / 2rem | 600 | 1.25 (40px) | −0.25px | Serif |
| Heading 2 | `type.h2` | 24px / 1.5rem | 600 | 1.33 (32px) | 0 | Serif |
| Heading 3 | `type.h3` | 20px / 1.25rem | 600 | 1.4 (28px) | 0 | Serif |
| Heading 4 | `type.h4` | 18px / 1.125rem | 600 | 1.44 (26px) | 0 | Sans |
| Body Large | `type.body-lg` | 18px / 1.125rem | 400 | 1.56 (28px) | 0 | Sans |
| Body Medium | `type.body-md` | 16px / 1rem | 400 | 1.5 (24px) | 0 | Sans |
| Body Small | `type.body-sm` | 14px / 0.875rem | 400 | 1.43 (20px) | 0.1px | Sans |
| Label Large | `type.label-lg` | 14px / 0.875rem | 600 | 1.43 (20px) | 0.1px | Sans |
| Label Medium | `type.label-md` | 12px / 0.75rem | 600 | 1.33 (16px) | 0.5px | Sans |
| Caption | `type.caption` | 12px / 0.75rem | 400 | 1.33 (16px) | 0.4px | Sans |
| Code | `type.code` | 14px / 0.875rem | 400 | 1.5 (21px) | 0 | Mono |

**Responsive note:** On viewports < 640px, scale Display Large → Display Medium and Heading 1 → 28px. Minimum rendered text: 12px. Never scale below 16px for body copy.

---

## Shape

The system leans square — government legibility over softness. Rounding is used sparingly and purposefully.

| Token | Radius | Components |
|-------|--------|------------|
| `shape.none` | 0 | Tables, official document blocks, flag-derived dividers |
| `shape.xs` | 2px | Status badges, notification dots |
| `shape.sm` | 4px | Buttons, text inputs, chips, tags, dropdowns |
| `shape.md` | 8px | Cards, dialog boxes, popovers |
| `shape.lg` | 12px | Feature panels, image containers |
| `shape.full` | 9999px | Toggle switches, pills, avatar indicators |

---

## Elevation

| Level | Token | CSS box-shadow | Usage |
|-------|-------|----------------|-------|
| 0 | `elevation.0` | none | Page background, tables, flat content |
| 1 | `elevation.1` | `0 1px 3px rgb(0 0 0/.12), 0 1px 2px rgb(0 0 0/.08)` | Cards, input fields |
| 2 | `elevation.2` | `0 3px 8px rgb(0 0 0/.14), 0 1px 4px rgb(0 0 0/.10)` | Dropdowns, popovers, tooltips |
| 3 | `elevation.3` | `0 8px 24px rgb(0 0 0/.18), 0 2px 8px rgb(0 0 0/.10)` | Modals, dialogs |
| 4 | `elevation.4` | `0 16px 40px rgb(0 0 0/.20), 0 4px 12px rgb(0 0 0/.12)` | Side drawers, bottom sheets |

---

## Interaction States

| State | Overlay | Notes |
|-------|---------|-------|
| Rest | — | Base appearance |
| Hover | 8% `#0094FF` tint | Light blue wash over interactive surface |
| Focus | — | 3px `#FFD700` outline, 2px offset; never removed for keyboard users |
| Pressed | 12% `#0094FF` tint | Deeper tint; instantaneous |
| Disabled | 40% opacity | Entire element; no hover or focus |
| Selected | `#E6F4FF` background | Row highlight, active tab fill |
| Error | `#C62828` border + `#FFF0F0` bg | Input validation failure |

---

## Layout

| Class | Breakpoint | Columns | Gutter | Navigation |
|-------|-----------|---------|--------|------------|
| Compact | < 640px | 4 | 16px | Hamburger menu / bottom nav bar |
| Medium | 640–1024px | 8 | 24px | Navigation rail (icons + labels) |
| Expanded | 1024–1280px | 12 | 24px | Persistent side nav drawer |
| Wide | > 1280px | 12 | 32px | Persistent side nav + max-width container |

**Max content width:** 768px prose/forms · 1200px dashboard/admin · full-bleed for hero images.
**Spacing scale (8px base):** `4 / 8 / 12 / 16 / 24 / 32 / 48 / 64 / 96 / 128px`

---

## Motion

| Token | Duration | CSS easing | Use |
|-------|----------|-----------|-----|
| `motion.instant` | 0ms | — | Toggle switches, immediate feedback |
| `motion.fast` | 100ms | `cubic-bezier(.2,0,.4,1)` | Button press ripple, badge update |
| `motion.standard` | 200ms | `cubic-bezier(.2,0,0,1)` | Dropdowns, tab transitions |
| `motion.gentle` | 300ms | `cubic-bezier(.05,.7,.1,1)` | Modal appear, side drawer slide |
| `motion.slow` | 400ms | `cubic-bezier(.3,0,.8,.15)` | Page hero, emphasis animation |

Always respect `prefers-reduced-motion: reduce` — collapse all transitions to 0ms.

---

## Icons & National Symbols

### National Symbols

| Asset | File | Usage rules |
|-------|------|-------------|
| Manticore Mossaesa | `manticore.svg` | Fill is **white**. Use only on `#2F2F2F` or `#0094FF` surfaces. Recolour to `#2F2F2F` for use on light backgrounds. Never render white-on-white. |
| Grand Mekniyan Seal | `Grand_Mekniyan_Seal.svg` | Formal headers and official documents only. Minimum 48px display size. |
| Mekniy Meehkkxu | `Mekniy_Meehkkxu.svg` | Ceremonial and cultural contexts. Not for generic UI decoration. |
| Flag | `MKLU-Flag-Small.svg` | Alongside wordmark in navigation header. Never altered, cropped, or recoloured. |

The diagonal geometry of the flag (angled triangular divisions in grey/white/blue) may be echoed as a **decorative divider motif** — a thin diagonal stripe or cut at section breaks — to add a creative Mekniyan identity without reproducing the flag itself.

### Icon System

Use outline-style icons on a 24px grid; 20px and 16px variants for dense UI. Recommended libraries (both open-source):
- **Phosphor Icons** — MIT licence, consistent weight, good Unicode/symbol coverage
- **Lucide** — ISC licence, clean stroke system

Stroke weight: 1.5px at 24px. Icon colour inherits from adjacent text token. Do not use filled icons alongside outlined, except for "active" state indicators.

---

## Design Tokens — Naming Convention

Format: `mklu.[category].[role].[variant?]`

```
mklu.color.text.primary
mklu.color.surface.brand
mklu.color.semantic.error
mklu.color.interactive.hover
mklu.color.focus

mklu.type.h1
mklu.type.body-md
mklu.type.label-lg

mklu.shape.md
mklu.elevation.2
mklu.motion.standard

mklu.spacing.16     → 16px
mklu.spacing.24     → 24px
```
