# Scenarios

A scenario is a folder here, chosen by the first path segment of a request to the stub (`/seeded/api/v1/...`).
A file answers the path it is named after (`api/v1/ServiceRegister/fips/channels.json`), verbatim and whatever the
query string says; `products/_by-id.json` answers any product id without its own file. Because the query is not
read, the listings are recorded as the application asks for them, with `status=Active`: the real API returns every
status unless asked, and a recording of every status would list products it withholds.
`empty` and `unavailable` have no folder: the stub synthesises them (a COMPASS holding nothing: every collection
empty and any product id unknown; 503).

- `seeded` - recorded 2026-09-04 from COMPASS `main` at `e6657ad`, built locally and pointed at a copy of a database
  holding the minimal seed scenario (synthetic data), seeded the way the CMS is: the same taxonomy fixture, the same
  assignment rule for every product's categories and user groups, with the dedicated lookups made to agree with the
  categorisation tags, and the same four category descriptions the browser suite asserts. Nothing in it is real: the
  products, contacts, and vocabulary values are the seed's inventions, and a value the seed gives no description shows
  as such on the page. The recordings evidence the shape of each response as the code builds it, and the tests assert
  only what the seed promised. A recording names the COMPASS commit it came from: the shape is the code's, and a
  recording from an older build cannot show what a newer one sends.
- `drift` - hand-written: members the records do not name, and members left out, so the observation path can be exercised.

To refresh `seeded`, run the estate kit's recorder, which seeds the recording database, starts a built COMPASS against
it, records every file here, and stops it:

```
node seed/record-compass-scenario.mjs --fresh --out <this repository>/src/Compass.FipsApi.Stub/scenarios/seeded --compass <COMPASS checkout>
```

then write the COMPASS commit it prints here, and run the tests. With `--fresh` the recording database is rebuilt from
the kit's template first, so two runs anywhere record the same bytes: the products' timestamps are pinned to the seed's
date, since a wall-clock stamp is the one thing a clean run cannot reproduce. The commit should be the one the records
were generated from; a recording from another build cannot show what this one sends.
A member the records do not name fails `RecordedPayload_ParsesThroughTheGeneratedRootType_WithNoMemberUnnamed`;
that means regenerate the records from COMPASS's source, not edit them.
