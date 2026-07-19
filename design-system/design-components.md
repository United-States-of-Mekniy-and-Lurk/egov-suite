# MKLU Components

> Full specifications for all 32 components and 4 document templates. Grouped by workflow.
> For tokens see [design.md](design.md). For rules & accessibility see [design-guidelines.md](design-guidelines.md).

---

## Actions

### Button

Primary interaction control. Sentence case. Always verb-first label.

**Types:** Filled (primary) › Outlined (secondary) › Text (tertiary) › Danger

| Property | Filled | Outlined | Text | Danger |
|----------|--------|----------|------|--------|
| Background | `#0094FF` | transparent | transparent | `#C62828` |
| Text | `#F8F8F8` | `#0072CC` | `#0072CC` | `#F8F8F8` |
| Border | none | 2px `#0094FF` | none | none |
| Height | 44px | 44px | 44px | 44px |
| Padding | 0 24px | 0 24px | 0 16px | 0 24px |
| Font | `type.label-lg` (14px/600) | `type.label-lg` | `type.label-lg` | `type.label-lg` |
| Radius | `shape.sm` (4px) | `shape.sm` | 0 | `shape.sm` |
| Hover bg | `#007ACC` | `#E6F4FF` | `#E6F4FF` | `#9C1F1F` |
| Focus | 3px `#FFD700` outline, 2px offset | same | same | same |
| Disabled | 40% opacity, no hover | same | same | same |

**Icon buttons:** Same height/radius; width = height (44px square). Always include `aria-label`.

**Do:** Use one Filled button per primary action per view.
**Don't:** Use Danger for anything other than destructive, irreversible actions (delete, revoke, expel).

---

### Link

Inline hyperlinks in prose and navigation.

| State | Color |
|-------|-------|
| Default | `#0072CC` + underline |
| Hover | `#005299` + underline |
| Visited | `#5A3EA8` + underline |
| Focus | `#FFD700` outline |
| Active | `#2F2F2F` |

Standalone links (not in prose) may omit underline at rest but must show it on hover and focus. Never use colour alone to convey link-ness — always pair with underline or explicit label.

---

### FAB (Floating Action Button)

For primary mobile actions only. Use sparingly — one per view maximum.

| Property | Value |
|----------|-------|
| Size | 56×56px |
| Background | `#0094FF` |
| Icon fill | `#F8F8F8` |
| Radius | `shape.full` |
| Elevation | `elevation.3` |
| Hover | `elevation.4` + `#007ACC` bg |

**Don't** use FAB on desktop — use a standard Filled button instead.

---

## Input

### Text Field

| Property | Value |
|----------|-------|
| Height | 48px |
| Padding | 0 16px |
| Background | `#FFFFFF` (`surface.raised`) |
| Border | 1px solid `#2F2F2F` |
| Border radius | `shape.sm` (4px) |
| Font | `type.body-md` (16px/400) |
| Label | `type.label-lg` above field; never inside as sole label |
| Focus border | 2px `#0094FF` |
| Error border | 2px `#C62828` |
| Error message | `type.body-sm` in `#C62828`, below field, `aria-describedby` linked |
| Helper text | `type.body-sm` in `color.text.secondary`, below field |
| Disabled | `surface.sunken` bg, `color.text.disabled`, no interaction |

**Nissiian/Loringian note:** Fields accepting Loringian input must use `lang="cr"` and `inputmode="text"`. Do not restrict input charset.

---

### Textarea

Same as Text Field but multi-line.

| Property | Value |
|----------|-------|
| Min height | 120px |
| Resize | vertical only |
| Padding | 12px 16px |

---

### Checkbox

| Property | Value |
|----------|-------|
| Box size | 20×20px, `shape.xs` (2px) |
| Border | 2px solid `#2F2F2F` |
| Checked bg | `#0094FF` |
| Check icon | White, 16px |
| Label | `type.body-md`, 8px left of box |
| Touch target | 44×44px minimum |
| Focus | `#FFD700` ring on box |

---

### Radio Button

| Property | Value |
|----------|-------|
| Size | 20×20px, `shape.full` |
| Border | 2px solid `#2F2F2F` |
| Selected dot | `#0094FF`, 10×10px centred |
| Label | `type.body-md`, 8px left |
| Touch target | 44×44px minimum |

---

### Toggle Switch

| Property | Value |
|----------|-------|
| Track size | 44×24px, `shape.full` |
| Track off | `#C8C8C8` |
| Track on | `#0094FF` |
| Thumb | 20×20px white circle |
| Transition | `motion.fast` (100ms) |
| Label | To the right or above |

---

### Select / Dropdown

| Property | Value |
|----------|-------|
| Height | 48px |
| Appearance | Same as Text Field |
| Chevron icon | 16px, `color.text.secondary`, right-aligned |
| Option list | `elevation.2`, `shape.sm`, white bg |
| Selected option | `surface.accent` (`#E6F4FF`) bg |
| Max height | 240px with scroll |

---

### Slider

| Property | Value |
|----------|-------|
| Track height | 4px, `#C8C8C8` (unfilled), `#0094FF` (filled) |
| Thumb | 20×20px, `#0094FF`, `shape.full` |
| Touch target | 44×44px around thumb |
| Tick marks | Optional, 2px dots |

---

## Navigation

### Navigation Header (Site Header)

The primary header for all MKLU government web services. Must be unmistakably Mekniyan — not a copy of any real government's header.

| Property | Value |
|----------|-------|
| Background | `#2F2F2F` (Mekniyan Grey) |
| Height | 64px desktop · 56px mobile |
| Logo area | Flag SVG (24px height) + wordmark in `type.h4` `#F8F8F8` |
| Wordmark | "Mekniy-Lurk" or service name in Nissiian / Czech / English |
| Language switcher | Text buttons, `type.label-md`, `#F8F8F8`, top-right |
| Nav links | `type.label-lg`, `#F8F8F8`, underline on hover |
| Active link | `#0094FF` bottom border 3px |
| Mobile | Hamburger icon (44×44px), reveals drawer |

**Mandatory:** Include a service identifier subtitle (e.g., "NVote — Citizen Voting" or "STEMMA Portal") in `type.caption` `#B0B0B0` when header is used in a specific service context.

---

### Navigation Drawer (Side Nav)

For expanded and wide breakpoints.

| Property | Value |
|----------|-------|
| Width | 256px |
| Background | `#F8F8F8` |
| Border-right | 1px `#C8C8C8` |
| Item height | 48px |
| Item padding | 0 16px |
| Item font | `type.label-lg` |
| Active item | `#E6F4FF` bg, `#0072CC` text, 3px left border `#0094FF` |
| Section headers | `type.label-md`, `color.text.secondary`, uppercase |
| Icons | 20px, left of label |

---

### Navigation Rail

For medium breakpoints.

| Property | Value |
|----------|-------|
| Width | 72px |
| Item | Icon (24px) + label below (`type.caption`) |
| Active | `#E6F4FF` pill bg, `#0094FF` icon |

---

### Tabs

| Property | Value |
|----------|-------|
| Height | 48px |
| Font | `type.label-lg` |
| Active indicator | 2px bottom border `#0094FF`, text `#0094FF` |
| Inactive | `color.text.secondary` |
| Hover | `surface.sunken` bg |
| Scroll | Horizontal scroll on compact with fade mask |

---

### Breadcrumb

| Property | Value |
|----------|-------|
| Font | `type.body-sm` |
| Separator | `/` in `color.text.secondary` |
| Current page | `color.text.primary`, no link |
| Links | `color.text.link` with underline |

---

### Search Bar

| Property | Value |
|----------|-------|
| Height | 48px |
| Border | 2px `#2F2F2F` |
| Radius | `shape.sm` |
| Search icon | 20px left-inside |
| Clear button | 20px right-inside, `aria-label="Clear search"` |
| Results dropdown | `elevation.2`, max 360px height |
| Result item | 48px height, `type.body-md` + secondary text |

---

## Containment

### Card

| Property | Value |
|----------|-------|
| Background | `#FFFFFF` |
| Border | 1px `#C8C8C8` (flat) or `elevation.1` |
| Radius | `shape.md` (8px) |
| Padding | 24px |
| Header | `type.h3` or `type.h4` |
| Body | `type.body-md` |
| Action area | Bottom-aligned, 16px from bottom |

**Official announcement card:** Use `surface.brand` (`#2F2F2F`) header bar across top (8px height) + card below. This is the "official" variant.

---

### Dialog / Modal

| Property | Value |
|----------|-------|
| Backdrop | `surface.overlay` `rgb(47 47 47/.6)` |
| Width | min 320px, max 560px |
| Radius | `shape.md` |
| Elevation | `elevation.3` |
| Header | `type.h3` + close button (top-right, 44×44px) |
| Body | `type.body-md`, scrollable |
| Footer | Right-aligned actions: secondary then primary button |
| Focus trap | Focus must stay within dialog while open |
| Close | `Escape` key or close button |

---

### Bottom Sheet

Mobile-only overlay for contextual actions.

| Property | Value |
|----------|-------|
| Handle | 4×32px pill, `#C8C8C8`, centred at top |
| Radius | `shape.lg` top corners only |
| Elevation | `elevation.4` |
| Swipe down | Dismiss (provide close button alternative) |

---

### Accordion

| Property | Value |
|----------|-------|
| Header height | 56px |
| Font | `type.label-lg` |
| Chevron | 20px, rotates 180° on open (`motion.standard`) |
| Border | 1px `#C8C8C8` bottom |
| Content | Padded 16px, `type.body-md` |

**Use for:** FAQ sections, legislation detail, collapsible form sections.

---

### Divider

| Property | Value |
|----------|-------|
| Standard | 1px `#C8C8C8` horizontal rule |
| Strong | 2px `#2F2F2F` |
| Accent | 2px `#0094FF` |
| Diagonal (creative) | CSS `clip-path` angled section break echoing flag geometry |

**Diagonal divider example:**
```css
.mklu-section-break {
  height: 64px;
  background: #2F2F2F;
  clip-path: polygon(0 0, 100% 0, 100% 60%, 0 100%);
  margin-bottom: -2px;
}
```

---

## Data Display

### Table

| Property | Value |
|----------|-------|
| Header row | `surface.brand` bg, `#F8F8F8` text, `type.label-lg` |
| Body row | white bg, `type.body-md` |
| Alternating row | `#F8F8F8` bg (optional) |
| Border | 1px `#C8C8C8` between rows; 2px `#2F2F2F` below header |
| Padding | 12px 16px per cell |
| Sortable header | Chevron icon + `cursor:pointer` |
| Selected row | `#E6F4FF` bg |
| Responsive | Horizontal scroll on compact; stack layout for < 3 columns |

**Legislation table:** Use strong header with `#2F2F2F` bg and add a document-number column in `type.code`.

---

### Badge / Tag

| Type | Background | Text | Use |
|------|-----------|------|-----|
| Default | `#EFEFEF` | `#2F2F2F` | Neutral label |
| Primary | `#E6F4FF` | `#0072CC` | Category, topic |
| Success | `#F0FFF4` | `#2E7D32` | Active, approved |
| Warning | `#FFF8E1` | `#CC7700` | Pending, review |
| Error | `#FFF0F0` | `#C62828` | Rejected, expired |

Height: 24px. Padding: 0 8px. Radius: `shape.xs` (2px). Font: `type.label-md`.

---

### Avatar

| Property | Value |
|----------|-------|
| Sizes | 24 / 32 / 40 / 56px |
| Radius | `shape.full` |
| Fallback | Initials in `type.label-lg`, `surface.brand` bg, `#F8F8F8` text |
| Border | 2px `#F8F8F8` when overlapping (stacked group) |

---

### Chip

Compact interactive filter or selection element.

| State | Background | Border | Text |
|-------|-----------|--------|------|
| Default | `#F8F8F8` | 1px `#C8C8C8` | `#2F2F2F` |
| Selected | `#E6F4FF` | 1px `#0094FF` | `#0072CC` |
| Hover | `#EFEFEF` | 1px `#C8C8C8` | `#2F2F2F` |

Height: 32px. Padding: 0 12px. Radius: `shape.full`.

---

### Skeleton Loader

| Property | Value |
|----------|-------|
| Color | `#EFEFEF` with shimmer animation |
| Animation | `background: linear-gradient(90deg, #EFEFEF 25%, #E0E0E0 50%, #EFEFEF 75%)` at 1.5s |
| Radius | Match the component it represents |
| Reduce-motion | Static `#EFEFEF`, no shimmer |

---

### Tooltip

| Property | Value |
|----------|-------|
| Background | `#2F2F2F` |
| Text | `#F8F8F8`, `type.body-sm` |
| Padding | 6px 10px |
| Radius | `shape.xs` |
| Delay | 300ms show, 100ms hide |
| Max width | 240px |
| Trigger | Hover + focus (never click-only) |
| Dismiss | Mouse leave or `Escape` |

---

## Feedback

### Alert / Notification Banner

Sitewide or section-level message banners.

| Type | Left border | Background | Icon | Text |
|------|------------|-----------|------|------|
| Info | 4px `#0094FF` | `#E6F4FF` | ℹ 20px | `#2F2F2F` |
| Success | 4px `#2E7D32` | `#F0FFF4` | ✓ 20px | `#2F2F2F` |
| Warning | 4px `#CC7700` | `#FFF8E1` | ⚠ 20px | `#2F2F2F` |
| Error | 4px `#C62828` | `#FFF0F0` | ✕ 20px | `#2F2F2F` |

Padding: 16px. `role="alert"` for error/success; `role="status"` for info. Include dismiss button (×, 44×44px) when dismissible.

---

### Toast / Snackbar

Ephemeral feedback, bottom-center on mobile, bottom-right on desktop.

| Property | Value |
|----------|-------|
| Background | `#2F2F2F` |
| Text | `#F8F8F8`, `type.body-md` |
| Padding | 12px 16px |
| Radius | `shape.sm` |
| Elevation | `elevation.3` |
| Duration | 4000ms default; persistent for errors |
| Action button | Text button in `#0094FF` |
| `aria-live` | `polite` (success/info) or `assertive` (error) |

---

### Progress Indicator

**Linear:**
| Property | Value |
|----------|-------|
| Track | 4px height, `#EFEFEF` |
| Fill | `#0094FF` |
| Indeterminate | Animated fill, respects `prefers-reduced-motion` |

**Circular:**
| Property | Value |
|----------|-------|
| Sizes | 24 / 40 / 64px |
| Stroke | 3px, `#0094FF` |
| Track | `#E6F4FF` |

Always include a text label for screen readers: `aria-label="Loading decree list"`.

---

## Document Templates

### Official Letter / Decree Template (A4)

A formal document template for edicts, letters, and official communications.

**Structure (top to bottom):**

| Zone | Height | Content |
|------|--------|---------|
| Official header bar | 24mm | `#2F2F2F` bg · Flag SVG (12mm h) left · Grand Seal (16mm h) right · centred wordmark `#F8F8F8` `type.h4` |
| Nation name block | — | "United States of Mekniy and Lurk / Shinsegye-Aēru" · `type.body-sm` · `color.text.secondary` · centred |
| Blue accent line | 3px | `#0094FF` full width, 4mm below nation block |
| Document title | — | `type.h1` · centred · 8mm top margin |
| Document metadata | — | `type.body-sm` · `color.text.secondary` · right-aligned: Document no., date, issuing body |
| Decorative rule | — | 1px `#C8C8C8` |
| Body text | — | `type.body-md` · `#2F2F2F` · 25mm side margins · 1.5 line height |
| Signature block | — | 32mm bottom margin · Signatory name `type.label-lg` · Title `type.body-sm` · `color.text.secondary` |
| Official seal area | 32mm | Centred · watermark-style Grand Seal at 15% opacity OR embossed area label |
| Footer | — | Thin `#0094FF` 2px line · `type.caption` `color.text.secondary` · Page n of N · Document reference |

**Watermark:** Manticore Mossaesa SVG, recoloured to `#2F2F2F`, 8% opacity, centred on body area, `aria-hidden="true"`.

**Print note:** Margins 25mm all sides. Body text minimum 11pt. Header bar prints in `#2F2F2F`; verify printer colour mode.

---

### Certificate Template (A4 / A5)

For honours, citizenship certificates, and awards.

**Structure:**

| Zone | Detail |
|------|--------|
| Outer border | 8mm `#2F2F2F` frame · 2mm gap · 2px `#0094FF` inner rule |
| Top emblem area | Grand Seal centred, 32mm height, full colour |
| Heading | "Certificate of [Type]" · `type.display-md` · `#2F2F2F` · centred |
| Subheading | Issuing authority · `type.h4` · `color.text.secondary` |
| Recipient block | "Awarded to" `type.body-lg` · recipient name `type.display-md` (600 weight) |
| Body | Citation text · `type.body-lg` · `color.text.secondary` · centred |
| Date | `type.label-lg` · bottom-left |
| Signatures | 2–3 signature lines bottom-right · `type.label-md` |
| Manticore motif | `manticore.svg` recoloured `#2F2F2F` · 40px · bottom-centre · decorative |

**Background:** Subtle diagonal pattern (very light `#EFEFEF` at 3%) using the flag's triangular geometry. Not present in digital, only print.

---

### Official Website Page Header (Digital)

The branded page header for individual government service portals (NVote, STEMMA, MBS, Veronabank portal, Naraeji, etc.).

**Structure:**

| Layer | Content |
|-------|---------|
| Skip link | First focusable element: "Skip to main content" · `#2F2F2F` bg · `#F8F8F8` text · visually hidden until focused |
| Site header | `#2F2F2F` bg · 64px · Flag + "United States of Mekniy and Lurk" wordmark left · Language switcher + user avatar right |
| Service bar | `#0094FF` bg · 40px · Service name in `type.label-lg` `#F8F8F8` left · Status badge optional right |
| Navigation | White bg · 48px · Breadcrumb or primary nav tabs |
| Hero/page title | `#F8F8F8` bg · `type.h1` · Optional diagonal bottom clip at 4deg echoing flag geometry |

**Service bar** distinguishes each portal. Example colours derived from Lurkish Sky: keep all service bars within the `#0094FF` family — do not introduce new brand colours per service.

---

### Passport Concept (ICAO TD3: 125 × 88mm)

A speculative layout for a Mekniyan-Lurk travel document. This is a micronational document — not valid as a real passport. Include this disclaimer on any implementation.

**Cover:**

| Element | Detail |
|---------|--------|
| Background | `#2F2F2F` (Mekniyan Grey) |
| Top emblem | Grand Seal, white fill, centred, 36mm height |
| Nation name | "United States of Mekniy and Lurk" · `type.h3` · `#F8F8F8` · centred |
| Nissiian name | "Shinsegye-Aēru" · `type.body-sm` · `#B0B0B0` |
| Document type | "MICRONATIONAL TRAVEL DOCUMENT" · `type.label-md` · `#B0B0B0` · bottom |
| Flag stripe | 4mm band of flag colours (3 stripes: grey/white/blue) along bottom edge |

**Data page (inside right):**

| Zone | Detail |
|------|--------|
| Photo area | 35×45mm · top-left · 1px `#0094FF` border |
| MRZ zone | 2 lines of OCR-B equivalent, bottom 8mm · `type.code` |
| Personal data fields | `type.label-md` labels `color.text.secondary` · `type.body-md` values · grid layout |
| Watermark | Manticore Mossaesa, white, 6% opacity, full page |
| Disclaimer | `type.caption` `color.text.secondary`: *"This document is issued by the United States of Mekniy and Lurk, a micronation. It is not a recognised travel document."* |

**Disclaimer is mandatory.** It must appear on the data page, the cover inside, or both.
