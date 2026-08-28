# Recorded COMPASS payloads (synthetic seed data)

One file per service-register endpoint, named by its last path segment under `/api/v1/ServiceRegister/`
(`products` and `enterprise-active` at `pageSize=100`), recorded 2026-08-28 from a COMPASS running
locally on its minimal seed scenario.

Nothing in these files is real: the products, contacts, and vocabulary values are the seed's inventions.
They evidence the shape of each response as the code builds it, and the tests assert only what the seed
promised.

To refresh, re-record every file from the same seed and run the tests.
A member the records do not name fails `RecordedPayload_ParsesThroughTheGeneratedRootType_WithNoMemberUnnamed`;
that means regenerate the records from COMPASS's source, not edit them.
