# Design notes

Why this interface looks and behaves the way it does. Each section names the
pattern being replaced, what this demo does instead, and what it costs.

Interaction counts below describe the flows as written — they are counted from
the markup in this repository and from the conventional flow being compared, not
taken from a user study.

---

## 1. Frequent actions live in the row

**The pattern being replaced.** To move an order along, you click the row, wait
for a detail page, click Edit, change a dropdown, click Save, then navigate back
to where you were — and usually lose your filters and your page number on the
way. Six interactions and two full page loads to change one field.

**Here.** Each row carries a single button naming the next stage: *Mark
processing*, *Mark shipped*, *Mark completed*. One click. The POST carries the
current filter state as route values and redirects back to exactly the view you
were on, with a toast confirming what changed.

**What it costs.** The action only covers the forward path through the pipeline.
Anything else — corrections, cancellations, editing the amount — still goes
through the edit form. That is the right trade: the common case gets one click,
the uncommon case keeps a full form.

**Why actions are not hidden until hover.** A hover-revealed menu is invisible on
touch, invisible to keyboard users until focus lands on it, and undiscoverable
for anyone who does not already know it is there. Two visible buttons per row
cost a little width and remove that whole class of problem.

---

## 2. Every view is a URL

Search, status, sort and page are GET parameters. That buys four things at once:

- **The back button works.** Filtering is navigation, so browser history is correct.
- **Views are shareable.** `?q=webb&status=Completed&sort=amount_desc` can be pasted into chat.
- **It works without JavaScript.** Column headers are links; the filter bar is a
  form with a real submit button. With scripting off, the table still filters,
  sorts and pages. `app.js` only removes the Apply button and adds a 350 ms
  debounce, because at that point it is redundant.
- **The server stays the single source of truth.** No duplicated filter logic
  between C# and JavaScript.

The CSV export follows the same rule: it exports what the current filters show,
not the entire table. An export that silently ignores your filters is a bug
report waiting to happen.

---

## 3. The chart palette was validated, not chosen by eye

The first palette for the status doughnut was the obvious semantic one: green for
completed, amber for pending, red for cancelled. Run through a contrast and
colour-vision check, it failed:

```
CVD separation  worst pair #d03b3b ↔ #0ca30c  ΔE 4.1 (deuteranopia)
```

Red against green, the single most common confusion, sat at roughly half the
separation threshold. Around one in twelve men would have seen two nearly
identical wedges.

Order status is not really a set of unrelated categories — it is an ordered
pipeline. So the doughnut uses a **single-hue ordinal ramp**, light to dark, in
pipeline order: pending is the lightest blue, completed the darkest. Cancelled
sits outside the ramp in grey, because it is outside the pipeline. That version
passes monotonic lightness, adjacent-step separation and surface contrast in both
light and dark mode.

Semantic colour still appears on the **status badges** in the table — but there
the colour sits beside a text label, so it is reinforcement, never the only
channel. The dot inside each badge is decorative; remove all colour and the table
still reads correctly.

Both charts also ship a `<details>` table of the same numbers, and the doughnut
legend prints the count for every slice. Nothing is encoded by colour alone.

---

## 4. Form errors appear in three places at once

A validation failure shows:

1. a summary at the top of the card, in a `role="alert"` region, listing every problem;
2. a message directly under each failing field, linked with `aria-describedby`;
3. a red border plus `aria-invalid` on the field itself.

Focus moves to the first invalid field on load. The summary matters when the form
is long enough that the broken field is off-screen; the inline message matters
when you are looking straight at it. Each covers the other's blind spot.

Validation runs on the server, with `novalidate` on the form so the browser's own
bubbles do not pre-empt it. One code path, one set of messages, no drift between
client and server rules. Saving redirects rather than re-rendering, so a refresh
cannot resubmit.

---

## 5. The table becomes a list of cards, not a scrollbar

Below 720px the table header is hidden and each row becomes a stacked card, with
the column name emitted from `data-label` next to each value. No horizontal
scrolling, no pinch-zoom, no pretending a six-column table fits a phone.

The sidebar follows the same logic: a rail you can collapse on desktop, an
off-canvas drawer on mobile that closes on `Escape` and on a tap outside, with
focus moved into it on open and back to the trigger on close.

---

## 6. Dark mode is a second set of values, not an inversion

Dark mode is not an automatic flip of the light palette. Surfaces, ink, borders
and the chart ramp are all restated for the dark surface, and the ramp was
re-validated against `#1a1a19` rather than assumed to survive the move.

The token values are declared twice on purpose: once under
`@media (prefers-color-scheme: dark)` guarded by `:root:not([data-theme="light"])`,
and once under `:root[data-theme="dark"]`. That is what lets the in-app toggle win
in both directions — including choosing light while the OS is dark. A small inline
script in `<head>` applies the stored choice before first paint, so reloading in
dark mode never flashes white.

---

## 7. Smaller decisions

- **Empty states lead somewhere.** No results shows what was searched and a button
  that clears the filters, rather than a blank panel.
- **One focus style, everywhere**, via `:focus-visible`, so keyboard users always
  see where they are and mouse users never see a stray ring.
- **A skip link** to the main content, because the sidebar is the first thing in
  the tab order on every page.
- **Numbers use tabular figures** in table columns so digits line up, and
  proportional figures in the large KPI values where alignment does not matter.
- **Active filters are shown as removable chips**, so it is always visible why a
  list is short — the most common "the data is missing" support ticket.
- **Icons never travel alone.** Every icon-only control carries a visually hidden
  label and a `title`.
- **Motion is suppressed** under `prefers-reduced-motion`.

---

## What I would measure next

This is a demo, so the claims above are structural, not empirical. On a real
system the things worth instrumenting are: time from list to a completed status
change, how often users reach page 2 instead of refining the filter (a filter
discoverability signal), and the rate of validation failures per field, which
usually points at a bad label rather than a careless user.
