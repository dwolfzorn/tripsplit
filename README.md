# TripSplit
A simple web app for splitting a shared trip cost across attendees, weighted by income and adjusted for cost of living, so everyone pays equitably and relatively to their financial situation.

## Getting started
Open https://dwolfzorn.github.io/tripsplit.
> Data is **not** saved automatically. Use **Export trip** (top right) before closing the tab if you want to keep your work, and **Import trip** to load it back in later.

## How to use it

### Example
`example-trip.json` in this repo is a sample file you can download and import via **Import trip** to see the app populated with realistic data.

### 1. Cost split calculator tab
- **Attendees** — Add each person's name, annual income, and home city.
- **COL adjuster** — Add all cities and their cost-of-living (COL) index, where 100 = US national average. A "Data sources" section links to reference sites for looking up COL index values.
- **Split details** — Shows each attendee's adjusted income and percentage share once you've entered attendees and at least one expense.

### 2. Expenses tab
- **Itemized expenses** — Add each cost (item name, amount, who paid for it). You can also paste multiple rows directly from Excel (copy item/cost/purchaser columns, then paste into any cell).
- **Exclusion tags** (optional) — Create a tag (e.g. "Alcohol" or "Kids activity"), and check off which attendees it applies to. Then, on any expense row, click **+ Add** to apply a tag to that expense — it appears as a chip (click the ✕ on a chip to remove it). Anyone assigned to an applied tag is excluded from that expense's cost, and the cost is re-split among everyone else.

### 3. Trip split tab
- **Settle up** — Shows the minimal set of payments needed to settle all balances (e.g. "Bob pays Alice $145"). Click **Details** to see each person's full paid/owed/balance breakdown.

## Importing and exporting
Use the **Export trip** / **Import trip** buttons at the top of the page to save your data to a `.json` file or load a previously saved trip. This is also how you'd share a trip file with someone else or move between devices.
