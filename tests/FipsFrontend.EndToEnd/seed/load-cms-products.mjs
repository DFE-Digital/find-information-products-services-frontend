#!/usr/bin/env node
//
// Loads the scale fixture's products into the FIPS CMS ALONE.
//
// load-scale.mjs deliberately loads all three sides together — COMPASS, the CMDB stub and the CMS —
// because the hunter it was built for reads all three, and it refuses to run if COMPASS's scale
// database has not been migrated. That is right for the hunter and wrong for the FIPS test suite,
// which only ever reads the CMS through the frontend. Rather than add a scoping flag to a script
// whose whole design is a three-way reversibility contract, this does the one side.
//
// Teardown is NOT implemented here: seed/teardown-scale-cms.mjs already removes by correlation-id
// prefix, which covers whatever put the records there. One loader per source, one teardown for all
// of them, rather than a matching pair per source that can drift apart.
//
// Usage:
//   node seed/load-cms-products.mjs            # dry run
//   node seed/load-cms-products.mjs --confirm

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const HERE = dirname(fileURLToPath(import.meta.url));
const CMS = process.env.CMS_BASE_URL ?? 'http://localhost:1337';
const CMS_KEY = process.env.CMS_FULL_API_KEY ?? process.env.CMS_WRITE_KEY;
const confirm = process.argv.includes('--confirm');

if (!CMS_KEY) { console.error('No CMS key. Set CMS_FULL_API_KEY.'); process.exit(2); }

const fixture = JSON.parse(readFileSync(join(HERE, 'scenarios', 'scale.cms.generated.json'), 'utf8'));
const entries = fixture.products;

const api = (path, init = {}) => fetch(`${CMS}/api${path}`, {
    ...init,
    headers: { Authorization: `Bearer ${CMS_KEY}`, 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    signal: AbortSignal.timeout(30000),
});

const countProducts = async () => {
    const res = await api('/products?pagination%5BpageSize%5D=1');
    return (await res.json())?.meta?.pagination?.total ?? -1;
};

const run = async () => {
    const before = await countProducts();
    console.log(`CMS holds ${before} products; fixture carries ${entries.length}`);

    if (!confirm) {
        console.log(`Would create ${entries.length}, each published and state=${entries[0].data.state}.`);
        console.log('DRY RUN — nothing written. Re-run with --confirm.');
        return;
    }

    let created = 0;
    const failures = [];
    for (const entry of entries) {
        // publishedAt is required for the record to be visible: the content type is draft-and-publish,
        // and `state: "Active"` is a separate axis that publishes nothing. A product can be Active and
        // unpublished, which passes every field-level check and renders nowhere.
        const body = { data: { ...entry.data, publishedAt: new Date().toISOString() } };
        const res = await api('/products', { method: 'POST', body: JSON.stringify(body) });
        if (res.ok) created += 1;
        else failures.push(`${entry.data.cmdb_sys_id}: ${res.status} ${(await res.text()).slice(0, 90)}`);
    }

    console.log(`Created ${created}/${entries.length}`);
    if (failures.length) console.log(`FAILED (${failures.length}): ${failures.slice(0, 3).join(' | ')}`);

    // Verified against the CMS rather than from the loop's own tally, and specifically through the
    // PUBLISHED+Active filter the site uses — an unfiltered count would go green on drafts.
    const after = await countProducts();
    const visible = await api('/products?filters%5Bstate%5D%5B%24eq%5D=Active&pagination%5BpageSize%5D=1')
        .then((r) => r.json()).then((b) => b?.meta?.pagination?.total ?? -1);
    console.log(`Verified: ${after} products total (was ${before}), ${visible} published AND Active`);
    if (visible < created) { console.log('WARNING: fewer visible than created — some are drafts.'); process.exitCode = 1; }
};

run().catch((e) => { console.error(e.message); process.exit(1); });
