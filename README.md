# TripSplit
A simple web app for splitting a shared trip cost across attendees, weighted by income and adjusted for cost of living, so everyone pays equitably and relatively to their financial situation.

## Running it
TripSplit is a self-hosted ASP.NET Core / Blazor app backed by SQLite. The easiest way to run it is with Docker Compose:

```
docker compose up --build
```

Then open http://localhost:8080. Trip and account data is persisted in the `tripsplit-data` volume.

Alternatively, run the server directly with `dotnet run --project src/TripSplit.Server`.

## Accounts and trips
- **Register / Log in** — Create an account to save trips permanently and access them from any device.
- **My Trips** — Lists every trip you own or have been invited to, with your role on each.
- **Sharing** — From a trip's **Members** panel, an owner can copy or regenerate a share link and send it to others. Recipients open the link, log in or register, and are added to the trip. Owners can remove members or hand off ownership.
- **Continue as guest** — Use the app without an account. Guest trips are **not saved** — use **Export trip** / **Import trip** (top right) to keep your work between visits or share it as a file.

### Example
[`tests/TripSplit.Tests/Fixtures/example-trip.json`](tests/TripSplit.Tests/Fixtures/example-trip.json) in this repo is a sample file you can import to see the app populated with realistic data.

## How to use it

### 1. Cost split calculator tab
- **Attendees** — Add each person's name, annual income, and home city.
- **COL adjuster** — Add all cities and their cost-of-living (COL) index, where 100 = US national average. A "Data sources" section links to reference sites for looking up COL index values.
- **Split details** — Shows each attendee's adjusted income and percentage share once you've entered attendees and at least one expense.

### 2. Expenses tab
- **Itemized expenses** — Add each cost (item name, amount, who paid for it). You can also paste multiple rows directly from Excel (copy item/cost/purchaser columns, then paste into any cell).
- **Exclusion tags** (optional) — Create a tag (e.g. "Alcohol" or "Kids activity"), and check off which attendees it applies to. Then, on any expense row, click **+ Add** to apply a tag to that expense — it appears as a chip (click the ✕ on a chip to remove it). Anyone assigned to an applied tag is excluded from that expense's cost, and the cost is re-split among everyone else.

### 3. Trip split tab
- **Settle up** — Shows the minimal set of payments needed to settle all balances (e.g. "Bob pays Alice $145"). Click **Details** to see each person's full paid/owed/balance breakdown.
