#!/usr/bin/env node
//
// Sets short_description on the category values whose text a test asserts verbatim.
//
// The product detail page lists a product's categories in a table with a Description column, and
// US168AC compares all four rows cell by cell. A category value with an empty description renders an
// empty cell, so the assertion fails on content the taxonomy seed never had a source for: the
// spreadsheet the taxonomy is extracted from carries names and urls, not descriptions.
//
// THE WORDING IS THE DEPLOYED SERVICE'S, NOT A CHOICE MADE HERE. The test is the only available
// evidence of what that environment says, the same provenance argument as the contact address. One of
// them contains a spelling mistake ("Continously"), reproduced exactly — correcting it would be
// inventing content and would fail the test that supplied it. That is a small, useful reminder that
// these strings are copied from a system rather than authored here.
//
// Only the values a test actually reads are set. Filling in the whole vocabulary would be inventing
// text for a hundred-odd values nobody has asserted anything about.
//
// Usage:
//   node seed/set-category-descriptions.mjs            # dry run
//   node seed/set-category-descriptions.mjs --confirm

const CMS = process.env.CMS_BASE_URL ?? 'http://localhost:1337';
const CMS_KEY = process.env.CMS_FULL_API_KEY ?? process.env.CMS_WRITE_KEY;
const confirm = process.argv.includes('--confirm');

if (!CMS_KEY) { console.error('No CMS key. Set CMS_FULL_API_KEY.'); process.exit(2); }

const DESCRIPTIONS = [
    {
        slug: 'customer-experience-and-design',
        neededBy: 'VerifyCategoriesDetailsInTableUS168AC',
        text: 'Partner with DfE teams to champion user needs and connect, improve and simplify services.',
    },
    {
        slug: 'web',
        neededBy: 'VerifyCategoriesDetailsInTableUS168AC',
        text: 'Real-time text-based communication delivered through web or mobile interfaces, often supporting automated and human interactions.',
    },
    {
        slug: 'live',
        neededBy: 'VerifyCategoriesDetailsInTableUS168AC',
        // "Continously" is the deployed service's spelling, asserted by the test. Not a typo here.
        text: 'Continously improving.',
    },
    {
        slug: 'information',
        neededBy: 'VerifyCategoriesDetailsInTableUS168AC',
        text: 'Provide guidance, policy content, or structured information to help people make decisions or understand their responsibilities.',
    },
];

// search_text is a COMMA-SEPARATED list, split by the view. A user-group row on the product categories
// page only shows its "Search terms (n)" disclosure when this is non-empty, and US168AC opens that
// disclosure — so an empty value means the control the test clicks does not exist at all.
//
// These are alternative words for the same audience, which is what the field is for: a person searching
// "civil servant" should find products aimed at departmental staff. The particular words are ours; only
// the presence of some is dictated by the test.
const SEARCH_TEXT = [
    {
        slug: 'department-for-education-workforce',
        neededBy: 'VerifyLinkInUsersProductTableUS168AC',
        // "DfE Staff" is capitalised because the test matches that text exactly. Written as the test
        // wants it rather than as house style would have it: this is copied content, and a lowercase
        // "s" here fails an assertion for a reason nobody would think to look for.
        // "DfE Staff" and "DfE workforce" are matched EXACTLY by the test, capitals included. The rest
        // are ours and could be anything.
        text: 'DfE Staff, DfE workforce, departmental staff, civil servant, policy official',
    },
];

const api = (path, init = {}) => fetch(`${CMS}/api${path}`, {
    ...init,
    headers: { Authorization: `Bearer ${CMS_KEY}`, 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    signal: AbortSignal.timeout(30000),
});

const run = async () => {
    let changed = 0;
    for (const d of DESCRIPTIONS) {
        const res = await api(`/category-values?filters%5Bslug%5D%5B%24eq%5D=${encodeURIComponent(d.slug)}`);
        if (!res.ok) throw new Error(`CMS returned ${res.status} looking up '${d.slug}'`);
        const record = (await res.json())?.data?.[0];
        if (!record) throw new Error(`No category value with slug '${d.slug}'`);

        const current = record.short_description ?? '';
        if (current === d.text) { console.log(`  ok     ${d.slug} already correct`); continue; }
        if (!confirm) { console.log(`  would  ${d.slug}: ${JSON.stringify(current)} -> ${JSON.stringify(d.text.slice(0, 50) + '...')}`); continue; }

        // publishedAt is included because these are draft-and-publish: updating only the draft leaves
        // the published row unchanged and the page still renders the old, empty value.
        const put = await api(`/category-values/${record.documentId}`, {
            method: 'PUT',
            body: JSON.stringify({ data: { short_description: d.text, publishedAt: new Date().toISOString() } }),
        });
        if (put.ok) { changed += 1; console.log(`  set    ${d.slug}  (${d.neededBy})`); }
        else console.log(`  FAIL   ${d.slug}  ${put.status} ${(await put.text()).slice(0, 100)}`);
    }

    for (const s of SEARCH_TEXT) {
        const res = await api(`/category-values?filters%5Bslug%5D%5B%24eq%5D=${encodeURIComponent(s.slug)}`);
        if (!res.ok) throw new Error(`CMS returned ${res.status} looking up '${s.slug}'`);
        const record = (await res.json())?.data?.[0];
        if (!record) throw new Error(`No category value with slug '${s.slug}'`);
        if ((record.search_text ?? '') === s.text) { console.log(`  ok     ${s.slug} search_text already correct`); continue; }
        if (!confirm) { console.log(`  would  ${s.slug} search_text -> ${s.text.split(',').length} term(s)`); continue; }
        const put = await api(`/category-values/${record.documentId}`, {
            method: 'PUT',
            body: JSON.stringify({ data: { search_text: s.text, publishedAt: new Date().toISOString() } }),
        });
        if (put.ok) { changed += 1; console.log(`  set    ${s.slug} search_text  (${s.neededBy})`); }
        else console.log(`  FAIL   ${s.slug}  ${put.status} ${(await put.text()).slice(0, 100)}`);
    }

    if (!confirm) { console.log('\nDRY RUN — re-run with --confirm.'); return; }

    // Read back from the store rather than trusting the write: a 200 from a draft-and-publish content
    // type does not by itself mean the published row carries the new value.
    console.log(`\nChanged ${changed}. Verifying by reading back:`);
    let wrong = 0;
    for (const d of DESCRIPTIONS) {
        const res = await api(`/category-values?filters%5Bslug%5D%5B%24eq%5D=${encodeURIComponent(d.slug)}`);
        const got = (await res.json())?.data?.[0]?.short_description ?? '';
        const ok = got === d.text;
        if (!ok) wrong += 1;
        console.log(`  ${ok ? 'ok   ' : 'WRONG'}  ${d.slug}`);
    }
    if (wrong) process.exitCode = 1;
};

run().catch((e) => { console.error(e.message); process.exit(1); });
