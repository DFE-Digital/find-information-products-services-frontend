#!/usr/bin/env node
//
// Assigns category values to the CMS's products: the layer between "the taxonomy exists" and "the site is
// usable". Without it every category page renders an empty list, which is present in the DOM with zero
// items and fails a visibility assertion exactly as a missing page would.
//
// The assignments are invented and deterministic; lib/category-assignment-rule.mjs says why.
//
// Usage:
//   node seed/assign-product-categories.mjs            # dry run
//   node seed/assign-product-categories.mjs --confirm
//   node seed/assign-product-categories.mjs --clear    # remove all assignments

// The CMS's full-access API key, from the environment.
const cmsFullApiKey = () => process.env.CMS_FULL_API_KEY ?? process.env.CMS_KEY ?? null;
import { DEFECT_SLUGS, picksFor, productIndex, sortBySlug } from './lib/category-assignment-rule.mjs';

const CMS = process.env.CMS_BASE_URL ?? 'http://localhost:1337';
const CMS_KEY = cmsFullApiKey();

const args = process.argv.slice(2);
const confirm = args.includes('--confirm');
const clear = args.includes('--clear');
// Activation is opt-in and never implied by --confirm. A CMDB ingest creates products as 'New' and the site
// lists Active products only, so a synced product is invisible until someone activates it. Setting Active
// here stays a separate, named act so that a seed cannot quietly hide that behaviour.
const activate = args.includes('--activate');

if (!CMS_KEY) { console.error('No CMS key. Set CMS_FULL_API_KEY.'); process.exit(2); }

const api = async (path, init = {}) => fetch(`${CMS}/api${path}`, {
    ...init,
    headers: { Authorization: `Bearer ${CMS_KEY}`, 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    signal: AbortSignal.timeout(30000),
});

const allOf = async (plural, extra = '') => {
    const out = [];
    for (let page = 1; ; page += 1) {
        const res = await api(`/${plural}?pagination%5Bpage%5D=${page}&pagination%5BpageSize%5D=100${extra}`);
        if (!res.ok) throw new Error(`GET /${plural} → ${res.status}`);
        const body = await res.json();
        out.push(...(body.data ?? []));
        const pc = body.meta?.pagination?.pageCount;
        if (!pc || page >= pc) return out;
    }
};

const run = async () => {
    const products = await allOf('products', '&populate=category_values');
    const values = await allOf('category-values', '&populate=category_type');

    const byType = new Map();
    for (const v of values) {
        const t = v.category_type?.slug;
        if (!t) continue;
        if (!byType.has(t)) byType.set(t, []);
        byType.get(t).push(v);
    }
    // Stable ordering by slug: the API's own order is not guaranteed across runs, and an assignment
    // that depends on it would be deterministic in appearance only.
    for (const [t, list] of byType) byType.set(t, sortBySlug(list));

    console.log(`${products.length} products, ${values.length} category values across ${byType.size} types`);
    const already = products.filter((p) => (p.category_values ?? []).length > 0).length;
    console.log(`${already} product(s) already carry at least one assignment`);

    if (clear) {
        if (!confirm) { console.log('DRY RUN — add --confirm to clear all assignments.'); return; }
        let cleared = 0;
        for (const p of products) {
            if (!(p.category_values ?? []).length) continue;
            const res = await api(`/products/${p.documentId}`, { method: 'PUT', body: JSON.stringify({ data: { category_values: [] } }) });
            if (res.ok) cleared += 1;
        }
        console.log(`Cleared ${cleared} product(s)`);
        return;
    }

    // Exclude the deliberately malformed values from routine assignment. They exist to be introduced
    // ON PURPOSE in a specific experiment; scattering them through the whole product set would make
    // every later observation ambiguous about whether a defect or the assignment caused it.
    for (const [t, list] of byType) byType.set(t, list.filter((v) => !DEFECT_SLUGS.has(v.slug)));

    // The rule itself (which value, which products stay uncategorised, and why) lives in
    // lib/category-assignment-rule.mjs, shared with the COMPASS twin so both sides tag alike.
    // Curated products keep their curation. load-test-required-products.mjs assigns specific combinations (a product
    // per filter combination the suite asserts, a product with no phase for the "Not categorised" tests); a rule
    // cannot reproduce a curated choice, and applying it here once overwrote them all, which read as search defects.
    const curated = (p) => /^TESTDEP-/i.test(p.cmdb_sys_id ?? '');
    const skippedCurated = products.filter(curated).length;
    if (skippedCurated) console.log(`${skippedCurated} curated product(s) (TESTDEP-*) keep the test loader's assignments`);
    const plan = products.filter((p) => !curated(p)).map((p) => ({
        product: p,
        picks: picksFor(productIndex(p.cmdb_sys_id, p.title), byType).map((k) => k.value),
    }));

    console.log(`\nThe rule assigns ${plan.reduce((n, x) => n + x.picks.length, 0)} value(s) across ${plan.length} products`);
    for (const x of plan.slice(0, 3)) {
        console.log(`  ${x.product.title?.slice(0, 40).padEnd(40)} → ${x.picks.map((v) => v.name).join(', ')}`);
    }

    // Only a product whose values or state differ from the rule's is written: a PUT moves the product's updatedAt
    // whether or not anything in it changed, so a run that changes nothing must send nothing.
    const same = (a, b) => a.length === b.length && a.every((id) => b.includes(id));
    const differing = plan.filter((x) => {
        const current = (x.product.category_values ?? []).map((v) => v.documentId);
        const wanted = x.picks.map((v) => v.documentId);
        return !same(current, wanted) || (activate && x.product.state !== 'Active');
    });
    console.log(`Differs from the rule: ${differing.length} product(s)`);
    if (!differing.length) { console.log('Nothing to do.'); return; }
    if (!confirm) { console.log('\nDRY RUN — nothing written. Re-run with --confirm.'); return; }

    let done = 0;
    const failed = [];
    for (const x of differing) {
        const payload = { category_values: x.picks.map((v) => v.documentId) };
        if (activate) payload.state = 'Active';
        const res = await api(`/products/${x.product.documentId}`, {
            method: 'PUT',
            body: JSON.stringify({ data: payload }),
        });
        if (res.ok) done += 1; else failed.push(`${x.product.title}: ${res.status}`);
    }
    if (activate) console.log('  --activate: state set to Active. This is a fixture choice; the ingest produces New.');
    console.log(`\nAssigned ${done}/${differing.length}`);
    if (failed.length) console.log(`FAILED (${failed.length}): ${failed.slice(0, 5).join('; ')}`);

    // Verify by re-reading, and check coverage PER TYPE — a product carrying one value looks assigned
    // while leaving four category pages empty, so a per-product count would go green on a state that
    // still fails most of what this exists to fix.
    const after = await allOf('products', '&populate=category_values');
    const withAny = after.filter((p) => (p.category_values ?? []).length > 0).length;
    console.log(`Verified: ${withAny}/${after.length} products carry at least one category value`);

    for (const [type, list] of byType) {
        const slugs = new Set(list.map((v) => v.slug));
        const covered = new Set();
        let uncategorised = 0;
        for (const p of after) {
            const inType = (p.category_values ?? []).filter((v) => slugs.has(v.slug));
            if (!inType.length) uncategorised += 1;
            for (const v of inType) covered.add(v.slug);
        }
        console.log(`  ${type.padEnd(15)} ${covered.size}/${list.length} values have at least one product; ${uncategorised} product(s) uncategorised`);
        // Both halves are asserted: every value needs a product, AND the "not categorised" bucket needs
        // members. A fixture satisfying only the first looks complete and fails a whole class of test.
        if (uncategorised === 0) console.log('    WARNING: nothing is uncategorised — the "Not categorised" filter will be empty.');
        if (covered.size < list.length) {
            const empty = list.filter((v) => !covered.has(v.slug)).map((v) => v.slug);
            console.log(`    still empty: ${empty.join(', ')}`);
        }
    }
};

run().catch((e) => { console.error(e.message); process.exit(1); });
