#!/usr/bin/env node
//
// Seeds the CMS's category types and category values from scenarios/cms-taxonomy.json (hand-authored) and, with
// --include-deep, the deeper user-group values from scenarios/taxonomy-from-testdata-xlsx.generated.json.
//
// No sync creates taxonomy: a CMDB ingest carries products, and category types and their values are content
// the service owns, so they are seeded directly. Without them every taxonomy page answers 404 (the controller
// returns NotFound when no published type matches the slug) and the listing renders no filter buttons.
//
// Both content types are draftAndPublish, and the frontend filters on publishedAt being non-null AND
// enabled being true. A record created without publishing is invisible in exactly the same way as a
// record that does not exist, so this script publishes explicitly and then VERIFIES through the same
// filtered query the frontend uses — not through an unfiltered count, which would go green on drafts.
//
// Usage:
//   node seed/load-cms-taxonomy.mjs            # dry run: says what it would create
//   node seed/load-cms-taxonomy.mjs --confirm  # creates
//   node seed/load-cms-taxonomy.mjs --unload   # removes everything it created, by slug

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const HERE = dirname(fileURLToPath(import.meta.url));
const CMS = process.env.CMS_BASE_URL ?? 'http://localhost:1337';
const CMS_KEY = process.env.CMS_FULL_API_KEY ?? process.env.CMS_KEY;

const args = process.argv.slice(2);
const confirm = args.includes('--confirm');
const unload = args.includes('--unload');

if (!CMS_KEY) {
    console.error('No CMS key. Set CMS_FULL_API_KEY (the read token cannot write).');
    process.exit(2);
}

const includeUnlisted = args.includes('--include-unlisted');
const includeDefects = args.includes('--include-defects');

const fixture = JSON.parse(readFileSync(join(HERE, 'scenarios', 'cms-taxonomy.json'), 'utf8'));

// The asserted baseline and the known-but-unlisted layer are merged HERE, at load time, and never in
// the fixture file. The baseline's value is that every entry is backed by a test assertion, so a
// difference between fixture and system means something; folding practitioner knowledge into it would
// quietly destroy that. Seeding the second layer is an experiment, and an experiment has to be
// distinguishable from the control.
const types = JSON.parse(JSON.stringify(fixture.categoryTypes));
let unlistedAdded = 0;
if (includeUnlisted) {
    for (const [typeSlug, values] of Object.entries(fixture.unlistedValues ?? {})) {
        if (typeSlug.startsWith('_')) continue;
        const target = types.find((t) => t.slug === typeSlug);
        if (!target) { console.error(`unlistedValues names type '${typeSlug}', which the baseline does not define.`); process.exit(2); }
        for (const v of values) {
            target.values.push({ name: v.name, slug: v.slug, sort_order: v.sort_order });
            unlistedAdded += 1;
        }
    }
}

// The user-group vocabulary is HIERARCHICAL, and nothing said so until the test suite's own
// spreadsheet was read: all twelve top-level values carry children, 63 in total, and one top-level
// value (social-worker) was missing from the hand-built fixture entirely. No CMS schema declares these,
// no seed creates them, and the filter panel shows only the flat level — so testdata.xlsx is the
// closest thing to a specification that exists. Extracted by seed/extract-taxonomy-from-tests.py.
//
// Read from the extracted file rather than copied into the fixture, because copying 75 values by hand
// is how the previous two reconstructions went wrong.
let hierarchyAdded = 0;
try {
    const extracted = JSON.parse(readFileSync(join(HERE, 'scenarios', 'taxonomy-from-testdata-xlsx.generated.json'), 'utf8'));
    const ug = types.find((t) => t.slug === 'user-group');
    if (ug && extracted.userGroups) {
        ug.multi_level = true;
        const bySlug = new Map(ug.values.map((v) => [v.slug, v]));
        extracted.userGroups.values.forEach((v, i) => {
            if (!bySlug.has(v.slug)) {
                ug.values.push({ name: v.label, slug: v.slug, sort_order: 100 + i });
                hierarchyAdded += 1;
            }
        });
        const slugify = (s) => s.toLowerCase().replace(/\(|\)/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
        ug.children = [];
        for (const [parentSlug, kids] of Object.entries(extracted.userGroups.children ?? {})) {
            kids.forEach((label, i) => {
                ug.children.push({ name: label, slug: slugify(label), sort_order: i + 1, parentSlug });
                hierarchyAdded += 1;
            });
        }
    }
} catch { /* extraction not run — the flat baseline still loads, and the count printed below says so */ }

let defectsAdded = 0;
if (includeDefects) {
    for (const [typeSlug, values] of Object.entries(fixture.defectValues ?? {})) {
        if (typeSlug.startsWith('_')) continue;
        const target = types.find((t) => t.slug === typeSlug);
        if (!target) { console.error(`defectValues names type '${typeSlug}', which the baseline does not define.`); process.exit(2); }
        for (const v of values) {
            // sort_order deliberately high so defects sort last and are visually obvious in any
            // ordered rendering, rather than interleaving with real values and looking native.
            target.values.push({ name: v.name, slug: v.slug, sort_order: 900 + defectsAdded });
            defectsAdded += 1;
        }
    }
}

const api = async (path, init = {}) => fetch(`${CMS}/api${path}`, {
    ...init,
    headers: { Authorization: `Bearer ${CMS_KEY}`, 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    signal: AbortSignal.timeout(30000),
});

const json = async (res) => { try { return await res.json(); } catch { return null; } };

// The frontend's own query, reproduced verbatim rather than approximated. Verifying with a laxer
// query than the consumer uses is how a seed passes its own check and still leaves the pages empty.
const visibleTypes = async () => {
    const res = await api('/category-types?filters%5BpublishedAt%5D%5B%24notNull%5D=true&filters%5Benabled%5D=true&sort=sort_order:asc&pagination%5BpageSize%5D=100');
    const body = await json(res);
    return body?.data ?? [];
};

// populate is REQUIRED, not decorative: Strapi omits relations from a default response, so
// v.category_type is undefined without it. The unload path matched on that field, found nothing, and
// reported "removed 5 types and 0 values" as a success while leaving 42 orphaned values behind — a
// green result produced by a query that could not have matched anything.
const allOf = async (plural, populate = '') => {
    const out = [];
    for (let page = 1; ; page += 1) {
        const res = await api(`/${plural}?pagination%5Bpage%5D=${page}&pagination%5BpageSize%5D=100${populate ? `&populate=${populate}` : ''}`);
        if (!res.ok) throw new Error(`GET /${plural} → ${res.status}`);
        const body = await json(res);
        out.push(...(body?.data ?? []));
        const pc = body?.meta?.pagination?.pageCount;
        if (!pc || page >= pc) return out;
    }
};

const create = async (plural, data) => {
    // publishedAt set at creation: Strapi treats a missing publishedAt as a draft, and a draft is
    // invisible to the frontend's filter while still counting in an unfiltered total.
    const res = await api(`/${plural}`, { method: 'POST', body: JSON.stringify({ data: { ...data, publishedAt: new Date().toISOString() } }) });
    const body = await json(res);
    if (!res.ok) throw new Error(`POST /${plural} ${data.slug} → ${res.status} ${JSON.stringify(body?.error?.message ?? body)}`);
    return body?.data;
};

const run = async () => {
    const before = await visibleTypes();
    console.log(`Before: ${before.length} category types visible to the frontend's query`);
    // Say which mode this is, every run. A fixture that sometimes carries unasserted values and
    // reports identically either way is one whose later measurements cannot be interpreted.
    const modes = [];
    if (unlistedAdded) modes.push(`${unlistedAdded} UNLISTED`);
    if (defectsAdded) modes.push(`${defectsAdded} DEFECTIVE`);
    console.log(modes.length
        ? `Mode: BASELINE + ${modes.join(' + ')} value(s) — this store deliberately holds values no baseline lists.`
        : 'Mode: asserted baseline only (--include-unlisted adds known-but-unrecorded values, --include-defects adds deliberately malformed ones).');
    if (defectsAdded) {
        console.log('       Defect values carry a PREDICTION each in the fixture. Read them before observing,');
        console.log('       so a fault appears as disagreement with a recorded answer rather than as a judgement call.');
    }

    // Removing ONLY the deliberately-malformed values, leaving the baseline intact. Needed because
    // the defect corpus does its job by breaking things: a trailing-space duplicate makes a link
    // ambiguous by accessible name and fails a test on purpose. So a run intended to be green and a
    // run intended to observe corruption cannot share a store, and the difference has to be one
    // command rather than a full unload-and-reseed that would also discard the baseline.
    if (args.includes('--unload-defects')) {
        const defectSlugs = new Set(Object.entries(fixture.defectValues ?? {})
            .filter(([k]) => !k.startsWith('_'))
            .flatMap(([, vs]) => vs.map((v) => v.slug)));
        const values = await allOf('category-values', 'category_type');
        const targets = values.filter((v) => defectSlugs.has(v.slug));
        console.log(`${targets.length} defect value(s) present of ${defectSlugs.size} defined`);
        for (const t of targets) console.log(`  ${t.name}`);
        if (!confirm) { console.log('DRY RUN — add --confirm to delete.'); return; }
        let gone = 0;
        for (const t of targets) {
            const r = await api(`/category-values/${t.documentId}`, { method: 'DELETE' });
            if (r.ok || r.status === 204) gone += 1;
        }
        const still = (await allOf('category-values')).filter((v) => defectSlugs.has(v.slug)).length;
        console.log(`Removed ${gone}/${targets.length}; ${still} still present`);
        if (still) process.exitCode = 1;
        return;
    }

    if (unload) {
        if (!confirm) { console.log('DRY RUN — add --confirm to delete.'); return; }
        const slugs = new Set(types.map((t) => t.slug));
        const valueSlugs = new Set(types.flatMap((t) => t.values.map((v) => `${t.slug}/${v.slug}`)));
        let gone = 0;
        const values = await allOf('category-values', 'category_type');
        const orphans = values.filter((v) => !v.category_type);
        for (const v of values) {
            // Match on slug alone would delete a value of the same name under a type we did not
            // create, so the type's slug is part of the key.
            const typeSlug = v.category_type?.slug;
            if (typeSlug && valueSlugs.has(`${typeSlug}/${v.slug}`)) {
                const r = await api(`/category-values/${v.documentId}`, { method: 'DELETE' });
                if (r.ok || r.status === 204) gone += 1;
            }
        }
        let typesGone = 0;
        for (const t of await allOf('category-types')) {
            if (slugs.has(t.slug)) {
                const r = await api(`/category-types/${t.documentId}`, { method: 'DELETE' });
                if (r.ok || r.status === 204) typesGone += 1;
            }
        }
        console.log(`Removed ${typesGone} types and ${gone} values`);

        // Orphans are values whose type is already gone, so the slug-pair key can no longer match
        // them. They are invisible to the site and to the by-type check, which is precisely why they
        // have to be named rather than left to be rediscovered as a mystery duplicate later.
        if (orphans.length) {
            console.log(`\n${orphans.length} values carry NO category type — orphaned by an earlier partial removal.`);
            console.log('Removing them too, since nothing else can now identify them:');
            let orphansGone = 0;
            for (const v of orphans) {
                const r = await api(`/category-values/${v.documentId}`, { method: 'DELETE' });
                if (r.ok || r.status === 204) orphansGone += 1;
            }
            console.log(`  removed ${orphansGone}/${orphans.length}`);
        }

        const leftover = (await allOf('category-values')).length;
        console.log(`After: ${(await visibleTypes()).length} types visible, ${leftover} category values remain`);
        if (leftover) { console.log('WARNING: values remain that this fixture does not account for.'); process.exitCode = 1; }
        return;
    }

    const existingTypes = await allOf('category-types');
    const bySlug = new Map(existingTypes.map((t) => [t.slug, t]));
    const planned = types.filter((t) => !bySlug.has(t.slug));

    // A type that already exists keeps whatever multi_level it was created with, and only the branch that
    // creates types set it. Measured: the user-group type sat at false on a store loaded before the flag was
    // planned, so the type page took its flat branch and showed no sub-category counts although every root
    // had children. Reconciled here so a re-run brings the flag into line with the fixture.
    for (const t of types) {
        const existing = bySlug.get(t.slug);
        if (!existing || !!existing.multi_level === !!t.multi_level) continue;
        if (!confirm) { console.log(`  ${t.slug}: multi_level would change ${!!existing.multi_level} -> ${!!t.multi_level}`); continue; }
        const res = await api(`/category-types/${existing.documentId}`, { method: 'PUT', body: JSON.stringify({ data: { multi_level: !!t.multi_level } }) });
        console.log(`  ${t.slug}: multi_level ${!!existing.multi_level} -> ${!!t.multi_level} (${res.status})`);
    }

    // Reconcile at VALUE level for types that already exist, not just at type level. Skipping a whole
    // type because its name is present makes the loader unable to add anything to it — which is
    // exactly what --include-unlisted needs to do, and would otherwise report 'nothing to do' while
    // leaving the requested value absent.
    const existingValues = await allOf('category-values', 'category_type');
    const haveValue = new Set(existingValues.filter((v) => v.category_type?.slug).map((v) => `${v.category_type.slug}/${v.slug}`));
    const missingValues = [];
    for (const t of types) {
        if (!bySlug.has(t.slug)) continue;
        for (const v of t.values) if (!haveValue.has(`${t.slug}/${v.slug}`)) missingValues.push({ type: t, value: v });
    }

    console.log(`Would create ${planned.length} types and ${planned.reduce((n, t) => n + t.values.length, 0)} values`);
    if (missingValues.length) {
        console.log(`Plus ${missingValues.length} value(s) missing from types that already exist:`);
        for (const m of missingValues) console.log(`  ${m.type.slug} → ${m.value.name}`);
    }
    // The early return must not skip the deeper passes below. It did: with every type and value already
    // present it reported "Nothing to do" and returned, so --include-deep silently did nothing and said
    // so in the most reassuring way available. A guard that reports success for work it never attempted
    // is the failure this repository keeps a document about, produced here by an ordering mistake.
    const deepRequested = args.includes('--include-deep');
    if (!planned.length && !missingValues.length && !deepRequested) { console.log('Nothing to do.'); return; }
    if (!confirm) { console.log('DRY RUN — nothing written. Re-run with --confirm.'); return; }

    for (const m of missingValues) {
        await create('category-values', {
            name: m.value.name, slug: m.value.slug, sort_order: m.value.sort_order, enabled: true,
            category_type: bySlug.get(m.type.slug).documentId,
        });
    }
    if (missingValues.length) console.log(`Added ${missingValues.length} value(s) to existing types`);

    // Top-level types and values first. The second- and third-level passes below look their parents up in the
    // store, so on an empty store they must run after this, not before: run the other way round, every deeper
    // value was reported as an orphan on a cold start and the loader needed three runs to converge.
    let madeTypes = 0;
    let madeValues = 0;
    for (const t of planned) {
        const created = await create('category-types', {
            name: t.name, slug: t.slug, description: t.description,
            sort_order: t.sort_order, multi_level: t.multi_level ?? false, enabled: true,
        });
        madeTypes += 1;
        for (const v of t.values) {
            await create('category-values', {
                name: v.name, slug: v.slug, sort_order: v.sort_order, enabled: true,
                category_type: created.documentId,
            });
            madeValues += 1;
        }
        console.log(`  ${t.slug}: 1 type + ${t.values.length} values`);
    }

    // Second level, created only after every top-level value exists, because each child needs its
    // parent's documentId. Done in a separate pass rather than inline for that reason alone — a child
    // created before its parent gets a null relation, still returns 200, and renders as an orphan the
    // site shows nowhere.
    for (const t of types) {
        if (!t.children?.length) continue;
        const typeDoc = bySlug.get(t.slug)?.documentId ?? (await allOf('category-types')).find((x) => x.slug === t.slug)?.documentId;
        const existing = await allOf('category-values', 'category_type');
        const inType = existing.filter((v) => v.category_type?.slug === t.slug);
        const parentBySlug = new Map(inType.map((v) => [v.slug, v]));
        const have = new Set(inType.map((v) => v.slug));
        let made = 0;
        // A child's parent may itself be a child created in this same pass, and may sit later in the list, so
        // each value created here joins the parent index at once and the list is swept until a sweep creates
        // nothing. What is left after that has a parent the fixture never names.
        let pending = t.children.filter((child) => !have.has(child.slug));
        for (let progress = true; progress && pending.length;) {
            progress = false;
            const stillPending = [];
            for (const child of pending) {
                const parent = parentBySlug.get(child.parentSlug);
                if (!parent) { stillPending.push(child); continue; }
                const created = await create('category-values', {
                    name: child.name, slug: child.slug, sort_order: child.sort_order, enabled: true,
                    category_type: typeDoc, parent: parent.documentId,
                });
                parentBySlug.set(child.slug, created);
                made += 1;
                progress = true;
            }
            pending = stillPending;
        }
        const orphaned = pending.map((child) => `${child.name} (no parent ${child.parentSlug})`);
        console.log(`  ${t.slug}: added ${made} second-level value(s)`);
        if (orphaned.length) { console.log(`    SKIPPED, parent missing: ${orphaned.join('; ')}`); process.exitCode = 1; }
    }

    // Deeper levels, resolved by NAME rather than by level. Each sheet in the extracted data names its
    // own parent first and lists that parent's children after it, so the same pass works whether the
    // parent sits at level one or level two — which matters because the taxonomy turned out to be three
    // deep after being confidently described as two, and hard-coding a depth would only defer the next
    // surprise.
    //
    // Runs last, after every shallower value exists, because a child needs its parent's documentId. A
    // child created before its parent gets a null relation, returns 200, and renders nowhere.
    if (args.includes('--include-deep')) {
        const extracted = JSON.parse(readFileSync(join(HERE, 'scenarios', 'taxonomy-from-testdata-xlsx.generated.json'), 'utf8'));
        const sheets = Object.entries(extracted.thirdLevel ?? {}).filter(([, v]) => Array.isArray(v) && v.length > 1);
        const ugType = (await allOf('category-types')).find((t) => t.slug === 'user-group');
        if (!ugType) { console.log('  user-group type absent — deep values skipped'); return; }

        const existing = await allOf('category-values', 'category_type');
        const inType = existing.filter((v) => v.category_type?.slug === 'user-group');
        const byName = new Map(inType.map((v) => [v.name.toLowerCase(), v]));
        const haveSlug = new Set(inType.map((v) => v.slug));
        const slugify = (s) => s.toLowerCase().replace(/[()]/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');

        let deepMade = 0; const noParent = [];
        for (const [, values] of sheets) {
            const parent = byName.get(values[0].toLowerCase());
            if (!parent) { noParent.push(values[0]); continue; }
            for (const childName of values.slice(1)) {
                const slug = slugify(childName);
                if (haveSlug.has(slug)) continue;
                await create('category-values', {
                    name: childName, slug, enabled: true,
                    category_type: ugType.documentId, parent: parent.documentId,
                });
                haveSlug.add(slug);
                deepMade += 1;
            }
        }
        console.log(`  user-group: added ${deepMade} deeper value(s) across ${sheets.length} parent(s)`);
        if (noParent.length) {
            // Named rather than counted: a parent that could not be matched is a gap in the extraction
            // or a naming mismatch, and both need the specific name to chase.
            console.log(`    NO MATCHING PARENT for: ${noParent.join('; ')}`);
            process.exitCode = 1;
        }
    }

    // Verify through the consumer's query and, for one type, that its values actually attached —
    // a value created with a bad relation still returns 200 and would otherwise be counted a success.
    const after = await visibleTypes();
    console.log(`\nCreated ${madeTypes} types and ${madeValues} values`);
    console.log(`Verified: ${after.length} types visible to the frontend's query (was ${before.length})`);

    const probe = types[0].slug;
    const res = await api(`/category-values?filters%5Bcategory_type%5D%5Bslug%5D%5B%24eq%5D=${probe}&pagination%5BpageSize%5D=100`);
    const attached = (await json(res))?.data?.length ?? 0;
    console.log(`Relation check: ${attached} values resolve under '${probe}' (fixture declares ${types[0].values.length})`);
    if (attached !== types[0].values.length) {
        console.log('MISMATCH — values were created but are not reachable through their type. The pages will still be empty.');
        process.exitCode = 1;
    }
};

run().catch((e) => { console.error(e.message); process.exit(1); });
