# Scenarios

A scenario is a folder here, chosen by the first path segment of a request to the stub (`/seeded/api/v1/...`).
A file answers the path it is named after (`api/v1/ServiceRegister/fips/channels.json`), verbatim;
`products/_by-id.json` answers any product id without its own file.
`empty` and `unavailable` have no folder: the stub synthesises them (a COMPASS holding nothing: every collection
empty and any product id unknown; 503).

- `seeded` - recorded 2026-09-02 from COMPASS `main` at `e6657ad`, built locally and pointed at a copy of a database
  holding the minimal seed scenario (synthetic data). Nothing in it is real: the products, contacts, and vocabulary
  values are the seed's inventions. The recordings evidence the shape of each response as the code builds it, and the
  tests assert only what the seed promised. A recording names the COMPASS commit it came from: the shape is the code's,
  and a recording from an older build cannot show what a newer one sends.
- `drift` - hand-written: members the records do not name, and members left out, so the observation path can be exercised.

To refresh `seeded`, re-record every file from the same seed against the COMPASS commit the records were generated from,
write that commit here, and run the tests.
A member the records do not name fails `RecordedPayload_ParsesThroughTheGeneratedRootType_WithNoMemberUnnamed`;
that means regenerate the records from COMPASS's source, not edit them.
