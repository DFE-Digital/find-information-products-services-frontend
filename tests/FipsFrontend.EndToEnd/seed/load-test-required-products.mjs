#!/usr/bin/env node
//
// The products, contacts, and volumes the browser suite names and does not create. Every record here exists
// because a test asserts it, and that test is named beside it, so a record whose test no longer needs it
// can be found and removed. Kept apart from the scale fixture: nothing here is for demonstration or browsing.
//
// A green test after this runs proves the search plumbing end to end; its assertions compare this seed
// with itself and say nothing about the service's search quality, ranking, or acronym handling.
//
// The two contact-search tests search for visibly synthetic names, and this loader creates the contact
// records and owning products behind them, so no test names a person.
//
// Usage:
//   node seed/load-test-required-products.mjs             # dry run, then verify what is already there
//   node seed/load-test-required-products.mjs --confirm
//   node seed/load-test-required-products.mjs --remove --confirm

import http from 'node:http';
import https from 'node:https';

const CMS = process.env.CMS_BASE_URL ?? 'http://localhost:1337';
const CMS_KEY = process.env.CMS_FULL_API_KEY ?? process.env.CMS_WRITE_KEY;
const FRONTEND = process.env.FIPS_BASE_URL ?? 'https://localhost:7601';
const confirm = process.argv.includes('--confirm');
const remove = process.argv.includes('--remove');

if (!CMS_KEY) { console.error('No CMS key. Set CMS_FULL_API_KEY.'); process.exit(2); }

// The prefix keeps these separable from SCALE-* records: teardown-scale-cms.mjs removes by correlation
// prefix and must NOT take these with it, because the two have different lifetimes — the scale fixture
// is demo content, this is test scaffolding that should outlive it only until the tests are fixed.
const PREFIX = 'TESTDEP';

// `searchedFor` is the exact keyword the test types. It is asserted through the FRONTEND after loading,
// because a record that exists in the CMS but does not match the search is a silent half-failure: the
// loader would report success and the test would still go red.
const RECORDS = [
    {
        searchedFor: 'GIAS',
        neededBy: 'VerifySearchBoxFunctionality_UsingAcronymUS285AC1',
        title: 'Get Information About Schools (GIAS)',
        summary: 'GIAS is the register of educational establishments. Seeded for the automated test suite.',
    },
    {
        searchedFor: 'FIPS',
        neededBy: 'VerifySearchBoxFunctionality_UsingAcronymUS285AC2',
        title: 'Find Information about Products and Services (FIPS)',
        summary: 'FIPS is the product and service register this frontend serves. Seeded for the automated test suite.',
    },
    {
        searchedFor: 'CRM',
        neededBy: 'VerifySearchBoxFunctionality_UsingNonPreferredTermsUS285AC5',
        title: 'Customer Relationship Management (CRM) Platform',
        summary: 'CRM platform used across the department. Seeded for the automated test suite.',
        // Also carries a channel because US285AC7 searches CRM AND filters to Web in one go. Without
        // it that test finds nothing — and, because these tests share one browser page, it then fails
        // before clearing its filters and the next two run their searches with Web still applied. One
        // missing value on one product accounted for three consecutive failures.
        slugs: ['web'],
    },
    {
        searchedFor: 'AI',
        neededBy: 'VerifySearchBoxFunctionUpdate_UsingKeywordAIUS306AC2',
        title: 'AI Assurance Toolkit',
        summary: 'AI assurance guidance and tooling. Seeded for the automated test suite.',
    },
    {
        searchedFor: 'HE workforce',
        neededBy: 'VerifyBusinessAreaSearchFunctionality_EnterpriseDataUS234AC4',
        title: 'HE workforce data collection',
        summary: 'Collects HE workforce records from providers. Seeded for the automated test suite.',
    },
    {
        searchedFor: 'DfE staff',
        neededBy: 'VerifyUserGroupCategoryListUS30AC',
        title: 'DfE staff directory',
        summary: 'Directory of DfE staff and teams. Seeded for the automated test suite.',
    },
];

// Contact search does NOT match a product field. It matches a related entra-user through the product's
// service_owner relation (displayName / firstName / lastName / emailAddress), so satisfying these tests
// means creating an owner record and relating it — not putting a name in a description. Doing the
// latter would turn them green while never exercising the relation the tests exist to cover, which is
// the kind of green that is worse than red.
//
// The names are deliberately, visibly synthetic. The suite originally searched for person-shaped names;
// whether or not those belonged to real people, a test fixture should never make anyone wonder. These
// cannot be mistaken for anybody, and they keep the two-word shape the original terms had, so the
// multi-word search path is still exercised.
const CONTACTS = [
    {
        searchedFor: 'Alpha Testcontact',
        neededBy: 'VerifySearchBoxFunctionUpdate_UsingContactsUS306AC5a',
        firstName: 'Alpha', lastName: 'Testcontact',
        displayName: 'Alpha Testcontact', emailAddress: 'alpha.testcontact@example.com',
        productTitle: 'Alpha Owned Service',
    },
    {
        searchedFor: 'Beta Testcontact',
        neededBy: 'VerifySearchBoxFunctionUpdate_UsingContactsUS306AC5b',
        firstName: 'Beta', lastName: 'Testcontact',
        displayName: 'Beta Testcontact', emailAddress: 'beta.testcontact@example.com',
        productTitle: 'Beta Owned Service',
    },
];

// Filter-combination tests tick several filter boxes and assert that products come back. FIPS ANDs
// across category types, so each of these needs ONE product carrying every listed value at once —
// a product per value will not do, and neither will products that each carry most of them.
//
// ALL SIX are seeded, not only the three that were failing. The other three passed by accident of how
// the bulk assignment happened to distribute values, which is not a property anybody chose and not one
// that survives the next change to the seed. A test passing for a reason nobody selected is a test
// that will fail later for a reason nobody expects.
//
// `phaseAbsent` is a real requirement rather than an omission: that test filters on phase NOT
// categorised, so its product must carry a business area and a type while having no phase at all.
// Records added after the first cut carry their OWN ids. The three lists above are numbered by
// position (RECORDS, then CONTACTS, then COMBINATIONS), so appending to RECORDS would renumber every
// contact and combination product and re-create them under new ids on any store that already has
// them. Fixed ids from 0020 upwards keep the original numbering intact.
const EXTRA_RECORDS = [
    {
        id: 'TESTDEP-0020',
        searchedFor: 'Apprentice',
        neededBy: 'ValidateNotCategorisedFilterOptions_CombinedWithKeywordSearchFunctionalityUS213AC',
        title: 'Apprenticeship Service Account',
        summary: 'Apprenticeship service account for employers and training providers. Seeded for the automated test suite.',
        // Searched as a keyword COMBINED with phase = not categorised, so this product must carry no
        // phase at all and still carry the other three vocabularies (invariant: a product missing a
        // vocabulary is a member of that vocabulary's not-categorised bucket — here that is the point).
        slugs: ['web', 'transactional', 'skills-and-growth'], phaseAbsent: true,
    },
];

// Searches satisfied by something other than a record of their own, verified the same way. The
// keyword search matches category value NAMES (OptimizedCmsApiService adds
// category_values.name $containsi to every term), so a product carrying the user-group value
// "Child or young person" answers a search for those words. The user-group coverage below creates it.
const KEYWORD_CHECKS = [
    {
        searchedFor: 'Child or young person',
        neededBy: 'VerifySearchBoxFunctionality_UsingUserGroupTermUS285AC4',
        satisfiedBy: 'the user-group coverage filler carrying that value',
    },
];

const COMBINATIONS = [
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC1',
        title: 'Schools Digital Web API', slugs: ['schools-digital', 'web', 'public-beta', 'api'],
    },
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC2',
        title: 'Operations Transactional Service', slugs: ['operations-and-infrastructure', 'live', 'transactional'],
    },
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC3',
        title: 'Skills and Strategy Retired Service', slugs: ['skills-and-growth', 'strategy', 'decommissioned'],
    },
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC4',
        title: 'Children and Families Multi-channel Service', slugs: ['children-and-families', 'face-to-face', 'web'],
    },
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC5',
        title: 'Funding Oversight Information Service', slugs: ['funding-and-financial-oversight', 'live', 'information'],
    },
    {
        neededBy: 'ValidateFilterLogicUpdate_ANDLogic_SearchFunctionalityUS220TC6',
        title: 'Enterprise Data Unphased Service', slugs: ['enterprise-data', 'data'], phaseAbsent: true,
    },
];

// PAGINATION NEEDS VOLUME, and volume is a fixture requirement like any other. Three tests click
// through to a page that only exists once enough products match a filter, and the page size is 25. The
// bulk assignment spreads values evenly across the vocabulary, which is a reasonable default and
// produces roughly 25 products per channel value — one short of the second page for the busiest one.
//
// Topping up to a stated minimum is honest about what the test needs. Biasing the bulk assignment
// towards these values instead would change every combination the other tests rely on, to fix three.
const MINIMUMS = [
    {
        neededBy: 'VerifyChannelSearchFunctionality_WebCategoryUS235AC9',
        // Count the pages the test actually VISITS, not the first one it names. This asserts page 2,
        // then 3, then clicks Next again for 4 — so it needs a fourth page to exist, and stopping at
        // "enough for page 2" only moved the failure from a missing page link to a missing Next link.
        query: 'channel=web', slugs: ['web'], atLeast: 76,
    },
    {
        neededBy: 'VerifyBusinessAreaSearchFunctionality_EnterpriseDataUS234AC4',
        query: 'group=enterprise-data', slugs: ['enterprise-data'], atLeast: 51,
    },
    {
        // No categories AT ALL, deliberately: this filter selects products the bulk assignment skipped,
        // so a filler carrying any value would count towards the wrong bucket.
        neededBy: 'VerifyBusinessAreaSearchFunctionality_NotCategorisedUS234AC12',
        query: 'group=__not_categorised__', slugs: [], atLeast: 51,
    },
    // Three filters whose tests pass hasPagination: true and so need a second page: 26 at page size 25.
    // The fixture's size, not its content, is what these tests depend on.
    {
        neededBy: 'VerifyPhaseSearchFunctionality_PublicBetaCategoryUS236AC5',
        query: 'phase=public-beta', slugs: ['public-beta'], atLeast: 26,
    },
    {
        neededBy: 'VerifyTypeSearchFunctionality_InformationCategoryUS237AC4',
        query: 'type=information', slugs: ['information'], atLeast: 26,
    },
    {
        neededBy: 'VerifyTypeSearchFunctionality_TransactionalCategoryUS237AC5',
        query: 'type=transactional', slugs: ['transactional'], atLeast: 26,
    },
];

// The product the detail tests open by its title through the listing, created with the categories those tests
// expect to see linked on its page.
const DETAIL_PRODUCT = {
    neededBy: 'ProductDetailTests (7 tests, by title)',
    title: 'Accessibility and inclusion manual',
    // VerifyProposeAChangeFormUS168AC reads the product URL field back verbatim: the deployed
    // service's own URL, not a placeholder.
    productUrl: 'https://accessibility.education.gov.uk',
    // VerifyProductOverviewPageHeadersUS168AC clicks a link whose name matches /contacts/ in the
    // overview row. "0 contacts" renders as a span, so the product needs at least one product-contact
    // row — which joins a users-permissions USER (not an entra-user; the two are different content
    // types) to the product with a role the detail view knows how to name.
    contact: {
        username: 'gamma.testcontact', email: 'gamma.testcontact@example.com',
        firstName: 'Gamma', lastName: 'Testcontact', displayName: 'Gamma Testcontact',
        role: 'senior_responsible_owner',
    },
    // The detail page counts contacts by the ROLE entra-users (service owner, product manager,
    // delivery manager) and falls back to product_contacts only when that count is zero; the
    // "Responsibilities and contacts" section renders only the role entra-users and says "No contact
    // information available." otherwise. VerifyProductOverviewPageHeadersUS168AC follows the count
    // link and expects a link bearing this name, then the first summary-list key on the page to be
    // visible — which it is only when that section exists above the collapsed identifiers. So the
    // service owner is the same entra-user the contact search tests use, with its e-mail, and the
    // product_contacts row above stays as the fallback path's fixture.
    serviceOwner: CONTACTS[0],
    // One value per table row the tests assert, in the order the Categories table shows them —
    // VerifyCategoriesDetailsInTableUS168AC expects exactly four rows (Business area, Channel, Phase,
    // Type, each with the verbatim description set-category-descriptions.mjs writes) and
    // VerifyLinkInUsersProductTableUS168AC expects the user group in the Users table.
    slugs: ['customer-experience-and-design', 'web', 'live', 'information', 'department-for-education-workforce'],
    // Asserted verbatim by US168AC, so it is the service's own wording rather than anything chosen here.
    description: 'Standards and guidance for designing and building accessible and inclusive products and services in DfE.',
};

const api = (path, init = {}) => fetch(`${CMS}/api${path}`, {
    ...init,
    headers: { Authorization: `Bearer ${CMS_KEY}`, 'Content-Type': 'application/json', ...(init.headers ?? {}) },
    signal: AbortSignal.timeout(30000),
});

// PEOPLE ARE FOUND BEFORE THEY ARE MADE. Neither entra-users nor users-permissions users carry a
// TESTDEP-* marker, so --remove cannot select them, and every create-after-remove cycle used to add
// another copy — four Testcontact entra-users were observed where two were intended. Looking each
// one up by e-mail address first makes the cycle converge on one copy; the copies already made are
// not removed here (nothing can tell which of four identical rows is the original).
const ensureEntraUser = async (c, entraId) => {
    const found = await api(`/entra-users?filters%5BemailAddress%5D%5B%24eq%5D=${encodeURIComponent(c.emailAddress)}&pagination%5BpageSize%5D=1`);
    if (!found.ok) throw new Error(`CMS returned ${found.status} looking up entra-user ${c.emailAddress}`);
    const existing = (await found.json())?.data?.[0]?.documentId;
    if (existing) return { documentId: existing, created: false };
    const res = await api('/entra-users', {
        method: 'POST',
        body: JSON.stringify({
            data: {
                firstName: c.firstName, lastName: c.lastName, displayName: c.displayName,
                emailAddress: c.emailAddress, entraId,
                publishedAt: new Date().toISOString(),
            },
        }),
    });
    if (!res.ok) throw new Error(`entra-user ${c.emailAddress}: ${res.status} ${(await res.text()).slice(0, 90)}`);
    const documentId = (await res.json())?.data?.documentId;
    if (!documentId) throw new Error(`entra-user ${c.emailAddress} created but returned no documentId`);
    return { documentId, created: true };
};

// A users-permissions user needs a role id, which the API refuses to default ("role is a required
// field"); the authenticated role is looked up by type rather than assumed to be id 1.
const ensurePluginUser = async (c) => {
    const found = await api(`/users?filters%5Bemail%5D%5B%24eq%5D=${encodeURIComponent(c.email)}`);
    if (!found.ok) throw new Error(`CMS returned ${found.status} looking up user ${c.email}`);
    const existing = (await found.json())?.[0]?.id;
    if (existing) return { id: existing, created: false };
    const rolesRes = await api('/users-permissions/roles');
    if (!rolesRes.ok) throw new Error(`CMS returned ${rolesRes.status} listing users-permissions roles`);
    const role = ((await rolesRes.json())?.roles ?? []).find((r) => r.type === 'authenticated')?.id;
    if (!role) throw new Error('No users-permissions role of type "authenticated" — cannot create a user');
    const res = await api('/users', {
        method: 'POST',
        body: JSON.stringify({
            username: c.username, email: c.email, confirmed: true, role,
            // A credential is required by the content type and never used: nothing signs in as this
            // user, and the value is the same local-only one the CMS admin uses.
            password: 'LocalDev-2026!',
            first_name: c.firstName, last_name: c.lastName, display_name: c.displayName,
        }),
    });
    if (!res.ok) throw new Error(`user ${c.email}: ${res.status} ${(await res.text()).slice(0, 90)}`);
    const id = (await res.json())?.id;
    if (!id) throw new Error(`user ${c.email} created but returned no id`);
    return { id, created: true };
};

// One product-contact row per (product, user); the role is reconciled if it differs.
const ensureProductContact = async (productDocumentId, userId, role) => {
    const found = await api(`/product-contacts?filters%5Bproduct%5D%5BdocumentId%5D%5B%24eq%5D=${productDocumentId}`
        + `&filters%5Busers_permissions_user%5D%5Bid%5D%5B%24eq%5D=${userId}&pagination%5BpageSize%5D=1`);
    if (!found.ok) throw new Error(`CMS returned ${found.status} looking up product-contacts for ${productDocumentId}`);
    const existing = (await found.json())?.data?.[0];
    if (existing?.documentId) {
        if (existing.role === role) return 'present';
        const put = await api(`/product-contacts/${existing.documentId}`, { method: 'PUT', body: JSON.stringify({ data: { role } }) });
        if (!put.ok) throw new Error(`product-contact role update: ${put.status} ${(await put.text()).slice(0, 90)}`);
        return 'role updated';
    }
    const res = await api('/product-contacts', {
        method: 'POST',
        body: JSON.stringify({ data: { role, product: productDocumentId, users_permissions_user: userId, publishedAt: new Date().toISOString() } }),
    });
    if (!res.ok) throw new Error(`product-contact: ${res.status} ${(await res.text()).slice(0, 90)}`);
    return 'created';
};

// A local application may serve the ASP.NET development certificate, which is self-signed, so verification
// has to be relaxed for the verification fetch. It is relaxed PER REQUEST via an https.Agent rather than
// through NODE_TLS_REJECT_UNAUTHORIZED, because that variable is process-global: it would disable
// verification for every other request this process makes, including ones added later by someone who
// never read this comment, and it stays disabled for the whole window rather than for one call. A
// non-loopback host is refused outright rather than fetched insecurely — the escape hatch stays the
// size of the problem.
//
// EVERY FRONTEND READ ASKS FOR A PAGE NUMBER NO REQUEST HAS USED BEFORE. The listing is served from a
// cache keyed on page, page size, search terms and filters (products_listing3_* in
// OptimizedCmsApiService, five minutes), so a query repeated inside the window returns the total from
// before this loader's own writes: without this, a second pass read a stale total, believed itself short,
// and created a hundred fillers it did not need. The page
// number is the one key component a caller controls without changing the result set, and the page
// renders its "N results" total even past the last page (measured: page 9999 shows 0 cards and the
// correct total). A fresh number per CALL, not per run, because the top-up reads a query before
// writing and the verification reads it again after.
//
// The frontend's own POST /Cache/ClearAllCache was the obvious remedy and does not work: it reflects
// into MemoryCache's private fields by names that no longer exist on .NET 8, clears nothing, and
// reports success. checks/frontend-cache-clear.mjs demonstrates both that and this workaround, and
// says when the route starts working so the workaround can go.
let freshPage = 100000 + (Math.floor(Date.now() / 1000) % 100000);

const pageBody = (query) => new Promise((resolve, reject) => {
    const url = new URL(`${FRONTEND}/products?${query}&page=${freshPage++}`);
    const isLoopback = ['localhost', '127.0.0.1', '::1'].includes(url.hostname);
    if (url.protocol === 'https:' && !isLoopback) {
        reject(new Error(`Refusing to relax certificate checks for a non-loopback host (${url.hostname}).`));
        return;
    }
    // A locally hosted frontend may be plain http (the clean-slate local run is); the agent that
    // relaxes certificate checks only means anything over https.
    const client = url.protocol === 'https:' ? https : http;
    const options = url.protocol === 'https:'
        ? { agent: new https.Agent({ rejectUnauthorized: !isLoopback }), timeout: 30000 }
        : { timeout: 30000 };
    const request = client.get(url, options, (res) => {
        let body = '';
        res.setEncoding('utf8');
        res.on('data', (chunk) => { body += chunk; });
        res.on('end', () => resolve(body));
    });
    request.on('timeout', () => request.destroy(new Error(`Timed out fetching /products?${query}`)));
    request.on('error', reject);
});

// COUNTING CARDS CANNOT MEASURE A TOTAL, because a page shows at most one page of them. Asking for 51
// and counting a 25-card page returns 25 forever, so a top-up loop reading that measure can never
// finish and cannot tell "not enough yet" from "as many as a page will ever show". The page states its
// own total, which is the number the pagination is derived from, so that is what to read — and since
// every read lands on a page past the last one (see pageBody), cards are never there to count anyway.
const totalResults = async (query) => {
    const body = await pageBody(query);
    const m = /([\d,]+)\s+results?\b/i.exec(body);
    if (m) return Number(m[1].replace(/,/g, ''));
    // No total shown at all means no result set: the page says something else entirely when empty.
    return (body.match(/dfe-chevron-card__link/g) ?? []).length;
};

const searchCount = (term) => totalResults(`keywords=${encodeURIComponent(term)}`);

// The not-categorised filters cannot be measured freshly through the frontend at all. They take a
// different path in ProductsController: one internal request for every Active product (page 1, page
// size 1000, sort by title), cached under THAT request's key, then filtered and paginated in memory.
// The listing page number never reaches the cache key, so the fresh-page trick above cannot touch it,
// and the entry is held for the cache duration across this loader's own writes, so a run that read the
// frontend here counted the figure from before its own fillers and created them again every time.
//
// So for these queries the measure is taken from the store, applying the predicate the frontend
// applies (ProductsController: Active, and no category value whose type carries the vocabulary's
// name) to the same fetch it makes. The frontend's own figure is still printed beside it, marked as
// cached, because the test reads the frontend — and a gap between the two that persists past the
// cache window is the frontend disagreeing with its own source, which is a finding, not a seed fault.
const NOT_CATEGORISED_TYPE = new Map([
    ['phase', 'Phase'], ['channel', 'Channel'], ['type', 'Type'], ['group', 'Business area'],
]);
const notCategorisedParam = (query) => /^(\w+)=__not_categorised__$/.exec(query)?.[1] ?? null;

const storeCountLacking = async (typeName) => {
    let page = 1;
    let lacking = 0;
    for (;;) {
        // Page size 500 is the CMS's configured maximum (config/api.ts); asking for more is silently
        // clamped to it, which is why the page count from the response drives the loop rather than
        // an assumption that one request saw everything.
        const res = await api('/products?filters%5B%24and%5D%5B0%5D%5Bstate%5D%5B%24eq%5D=Active'
            + `&pagination%5Bpage%5D=${page}&pagination%5BpageSize%5D=500&sort=title%3Aasc`
            + '&populate%5Bcategory_values%5D%5Bpopulate%5D%5Bcategory_type%5D%5Bfields%5D%5B0%5D=name');
        if (!res.ok) throw new Error(`CMS returned ${res.status} listing Active products for the not-categorised measure`);
        const body = await res.json();
        if (!Array.isArray(body?.data)) throw new Error('CMS response had no data array for the not-categorised measure');
        lacking += body.data.filter((p) => !(p.category_values ?? []).some((v) => v.category_type?.name === typeName)).length;
        if (page >= (body.meta?.pagination?.pageCount ?? 1)) return lacking;
        page += 1;
    }
};

// One measure for a MINIMUMS query: the frontend's fresh total where the frontend can be read fresh,
// the store's count under the frontend's predicate where it cannot.
const measure = async (query) => {
    const param = notCategorisedParam(query);
    if (!param) return totalResults(query);
    const typeName = NOT_CATEGORISED_TYPE.get(param);
    if (!typeName) throw new Error(`No category type known for the '${param}' not-categorised filter`);
    return storeCountLacking(typeName);
};
const describeMeasure = async (query) => {
    const n = await measure(query);
    if (!notCategorisedParam(query)) return { n, note: '' };
    // A frontend figure below the store's has two known causes on that path, neither a seed fault: its
    // cache entry predates this loader's writes, or the store has more than 500 Active products and the
    // frontend's page loop never fetches the rest.
    const frontendSays = await totalResults(query);
    return { n, note: frontendSays === n ? '' : `  (frontend page says ${frontendSays}: cached, or truncated past 500 Active products)` };
};

// The category type a value belongs to decides which query parameter carries it, and the filter panel
// uses one parameter per type. Derived from the CMS rather than hard-coded, so a renamed type surfaces
// as an unmapped name instead of a filter that silently never matches.
const PARAM_BY_TYPE = new Map([
    ['Phase', 'phase'], ['Channel', 'channel'], ['Business area', 'group'], ['Type', 'type'],
]);

// Throws rather than returning [] on a failed call. An unauthorised or unreachable CMS produces the
// same empty list as a genuine "none present", and the loader would then report "0 already present",
// create six duplicates on a second run, and look like it had worked both times. The distinction
// between "asked and the answer was none" and "never got an answer" has to survive to the caller.
const findExisting = async () => {
    // A page size large enough for every record this script creates, and a check that it was. A
    // truncated list would understate what exists, which now also drives the filler numbering: the
    // sequence would restart inside a range already taken, every id would collide, and the run would
    // report success having created nothing.
    const res = await api(`/products?filters%5Bcmdb_sys_id%5D%5B%24startsWith%5D=${PREFIX}&pagination%5BpageSize%5D=500&pagination%5BwithCount%5D=true`);
    if (!res.ok) {
        throw new Error(`CMS returned ${res.status} listing existing ${PREFIX}-* products `
            + `— cannot tell "none present" from "not asked". ${(await res.text()).slice(0, 120)}`);
    }
    const body = await res.json();
    if (!Array.isArray(body?.data)) throw new Error(`CMS response had no data array: ${JSON.stringify(body).slice(0, 120)}`);
    const total = body?.meta?.pagination?.total;
    if (typeof total === 'number' && total > body.data.length) {
        throw new Error(`Listed ${body.data.length} of ${total} ${PREFIX}-* products — the page size is too small. `
            + 'Raise it rather than proceeding: a partial list understates what exists and the filler numbering restarts inside a range already taken.');
    }
    return body.data;
};

const run = async () => {
    const existing = await findExisting();
    console.log(`Test-required records: ${RECORDS.length + CONTACTS.length} `
        + `(${RECORDS.length} products, ${CONTACTS.length} owned by a seeded contact), `
        + `${existing.length} already present\n`);

    // KNOWN GAP, stated rather than left to be discovered: --remove deletes the products and NOT the
    // entra-user records created alongside the contact ones, because those carry no correlation prefix
    // to select them by. A remove-then-add cycle therefore leaves orphaned contacts behind, and four
    // were observed where two were intended. Harmless so far — the contact searches match either copy —
    // but it will not stay harmless if a test ever counts them. Fixing it means giving the users a
    // recognisable marker, which is a change to how they are created rather than to how they are
    // deleted, so it is not a one-line addition here.
    if (remove) {
        if (!confirm) { console.log(`Would delete ${existing.length} ${PREFIX}-* products. DRY RUN.`); return; }
        let gone = 0;
        for (const r of existing) {
            const res = await api(`/products/${r.documentId}`, { method: 'DELETE' });
            if (res.ok) gone += 1;
        }
        console.log(`Deleted ${gone}/${existing.length}`);
        return;
    }

    if (!confirm) {
        console.log('Current state, measured through the frontend search each test uses:');
        for (const r of [...RECORDS, ...EXTRA_RECORDS, ...CONTACTS, ...KEYWORD_CHECKS]) {
            console.log(`  ${String(await searchCount(r.searchedFor)).padStart(3)} results  "${r.searchedFor}"  (${r.neededBy})`);
        }
        console.log('\nDRY RUN — nothing written. Re-run with --confirm.');
        return;
    }

    const have = new Set(existing.map((r) => r.cmdb_sys_id));
    let created = 0;
    // Continue the filler numbering past whatever already exists. Restarting at 1 collides with
    // previous fillers, and the collision was SILENT: the loop skipped each taken id and moved on, so
    // asking for 19 more produced none and the run reported success. A top-up that quietly tops up
    // nothing is worse than one that fails, because the number it reports afterwards looks like a
    // measurement rather than an unchanged starting point.
    let fillerSeq = 1 + Math.max(0, ...existing
        .map((r) => /^TESTDEP-F(\d+)$/.exec(r.cmdb_sys_id ?? ''))
        .filter(Boolean)
        .map((m) => Number(m[1])));

    // Every slug any record needs is resolved up front, before a single product is written, so a
    // renamed or missing category value stops the run rather than producing products that look created
    // and carry fewer values than intended.
    const wantedSlugs = [...new Set([...RECORDS, ...EXTRA_RECORDS, ...COMBINATIONS, ...MINIMUMS, DETAIL_PRODUCT].flatMap((c) => c.slugs ?? []))];
    const slugToId = new Map();
    const slugToParam = new Map();
    const slugToType = new Map();
    for (const slug of wantedSlugs) {
        const res = await api(`/category-values?filters%5Bslug%5D%5B%24eq%5D=${encodeURIComponent(slug)}&populate%5Bcategory_type%5D=true&pagination%5BpageSize%5D=1`);
        if (!res.ok) throw new Error(`CMS returned ${res.status} resolving category value '${slug}'`);
        const record = (await res.json())?.data?.[0];
        if (!record?.documentId) throw new Error(`No category value with slug '${slug}' — the filter it belongs to cannot be satisfied`);
        slugToId.set(slug, record.documentId);
        slugToParam.set(slug, PARAM_BY_TYPE.get(record.category_type?.name));
        slugToType.set(slug, record.category_type?.name);
    }

    // A product present from an earlier run keeps whatever category values it was last given, and the bulk category
    // assigner once overwrote every one of them, which read as search defects. The set of values IS the fixture, so an
    // existing product is reconciled to it rather than skipped; one without a curated set is left alone.
    const reconcile = async (id, slugs, label) => {
        if (!slugs) { console.log(`  skip   ${id} already present`); return; }
        const res = await api(`/products?filters%5Bcmdb_sys_id%5D%5B%24eq%5D=${id}&populate%5Bcategory_values%5D%5Bfields%5D%5B0%5D=slug`);
        const current = res.ok ? (await res.json())?.data?.[0] : null;
        if (!current?.documentId) { console.log(`  skip   ${id} listed as present but could not be read back`); return; }
        const currentSlugs = (current.category_values ?? []).map((v) => v.slug).sort();
        if (currentSlugs.join(',') === [...slugs].sort().join(',')) { console.log(`  skip   ${id} already present with every value`); return; }
        const put = await api(`/products/${current.documentId}`, { method: 'PUT', body: JSON.stringify({ data: { category_values: slugs.map((s) => slugToId.get(s)) } }) });
        console.log(put.ok ? `  repair ${id}  ${label}  [${slugs.join(' + ')}] (was ${currentSlugs.join(' + ') || 'none'})` : `  FAIL   ${id}  ${put.status}`);
    };

    for (const [i, r] of RECORDS.entries()) {
        const id = `${PREFIX}-${String(i + 1).padStart(4, '0')}`;
        if (have.has(id)) { await reconcile(id, r.slugs, r.title); continue; }
        // publishedAt is required or the record renders nowhere: the content type is draft-and-publish
        // and `state: "Active"` is a separate axis that publishes nothing.
        const body = {
            data: {
                title: r.title, cmdb_sys_id: id,
                short_description: r.summary, long_description: r.summary,
                product_url: `https://example.org/${id.toLowerCase()}`,
                parent_category: 'Test Dependency', state: 'Active',
                // Categories only where a test filters on them alongside the keyword. Left off
                // otherwise on purpose: these products would then appear in every category listing and
                // change the counts the "not categorised" tests assert on, so giving them all a tidy
                // set of values would fix nothing and break something.
                ...(r.slugs ? { category_values: r.slugs.map((s) => slugToId.get(s)) } : {}),
                publishedAt: new Date().toISOString(),
            },
        };
        const res = await api('/products', { method: 'POST', body: JSON.stringify(body) });
        if (res.ok) { created += 1; console.log(`  create ${id}  ${r.title}`); }
        else console.log(`  FAIL   ${id}  ${res.status} ${(await res.text()).slice(0, 90)}`);
    }

    // Contacts: one entra-user each, then a product relating to it through service_owner. Created after
    // the plain products so a failure here is distinguishable from a failure there.
    for (const r of EXTRA_RECORDS) {
        if (have.has(r.id)) { await reconcile(r.id, r.slugs, r.title); continue; }
        const res = await api('/products', {
            method: 'POST',
            body: JSON.stringify({
                data: {
                    title: r.title, cmdb_sys_id: r.id,
                    short_description: r.summary, long_description: r.summary,
                    product_url: `https://example.org/${r.id.toLowerCase()}`,
                    parent_category: 'Test Dependency', state: 'Active',
                    category_values: r.slugs.map((s) => slugToId.get(s)),
                    publishedAt: new Date().toISOString(),
                },
            }),
        });
        if (res.ok) { created += 1; console.log(`  create ${r.id}  ${r.title}  [${r.slugs.join(' + ')}${r.phaseAbsent ? ', no phase' : ''}]`); }
        else console.log(`  FAIL   ${r.id}  ${res.status} ${(await res.text()).slice(0, 90)}`);
    }

    for (const [i, c] of CONTACTS.entries()) {
        const id = `${PREFIX}-${String(RECORDS.length + i + 1).padStart(4, '0')}`;
        if (have.has(id)) { console.log(`  skip   ${id} already present`); continue; }

        const user = await ensureEntraUser(c, `00000000-0000-0000-0000-${String(i + 1).padStart(12, '0')}`);
        const userDocumentId = user.documentId;
        if (!user.created) console.log(`  reuse  entra-user ${c.emailAddress}`);

        const res = await api('/products', {
            method: 'POST',
            body: JSON.stringify({
                data: {
                    title: c.productTitle, cmdb_sys_id: id,
                    short_description: `${c.productTitle}. Seeded for the automated test suite.`,
                    long_description: `${c.productTitle}. Seeded for the automated test suite.`,
                    product_url: `https://example.org/${id.toLowerCase()}`,
                    parent_category: 'Test Dependency', state: 'Active',
                    service_owner: [userDocumentId],
                    publishedAt: new Date().toISOString(),
                },
            }),
        });
        if (res.ok) { created += 1; console.log(`  create ${id}  ${c.productTitle} owned by ${c.displayName}`); }
        else console.log(`  FAIL   ${id}  ${res.status} ${(await res.text()).slice(0, 90)}`);
    }

    // Slugs were resolved before anything was written — see the block above the first creation loop.

    for (const [i, c] of COMBINATIONS.entries()) {
        const id = `${PREFIX}-${String(RECORDS.length + CONTACTS.length + i + 1).padStart(4, '0')}`;
        if (have.has(id)) { await reconcile(id, c.slugs, c.title); continue; }
        const res = await api('/products', {
            method: 'POST',
            body: JSON.stringify({
                data: {
                    title: c.title, cmdb_sys_id: id,
                    short_description: `${c.title}. Seeded for the automated test suite.`,
                    long_description: `${c.title}. Seeded for the automated test suite.`,
                    product_url: `https://example.org/${id.toLowerCase()}`,
                    parent_category: 'Test Dependency', state: 'Active',
                    category_values: c.slugs.map((s) => slugToId.get(s)),
                    publishedAt: new Date().toISOString(),
                },
            }),
        });
        if (res.ok) { created += 1; console.log(`  create ${id}  ${c.title}  [${c.slugs.join(' + ')}]${c.phaseAbsent ? ' (no phase, deliberately)' : ''}`); }
        else console.log(`  FAIL   ${id}  ${res.status} ${(await res.text()).slice(0, 90)}`);
    }

    // The product the detail tests reach by hardcoded id. Created like any other; the id is forced
    // afterwards by seed/force-product-documentid.mjs, which needs the CMS stopped.
    {
        const id = `${PREFIX}-0100`;
        let detailDocumentId = null;
        const owner = await ensureEntraUser(DETAIL_PRODUCT.serviceOwner, '00000000-0000-0000-0000-000000000001');
        if (have.has(id)) {
            // Present from an earlier run, possibly with fewer values than the tests read (the first
            // cut carried two of five). Reconciled rather than skipped: the set of values is the
            // fixture, and a product that exists with the wrong set is the failure that reads as a
            // test defect.
            const wanted = DETAIL_PRODUCT.slugs.map((s) => slugToId.get(s));
            const res = await api(`/products?filters%5Bcmdb_sys_id%5D%5B%24eq%5D=${id}&fields%5B0%5D=product_url&populate%5Bcategory_values%5D%5Bfields%5D%5B0%5D=slug&populate%5Bservice_owner%5D%5Bfields%5D%5B0%5D=emailAddress`);
            if (!res.ok) throw new Error(`CMS returned ${res.status} reading ${id}'s category values`);
            const current = (await res.json())?.data?.[0];
            const currentSlugs = (current?.category_values ?? []).map((v) => v.slug);
            const missing = DETAIL_PRODUCT.slugs.filter((s) => !currentSlugs.includes(s));
            if (!current?.documentId) throw new Error(`${id} was listed as present but could not be read back`);
            detailDocumentId = current.documentId;
            const patch = {};
            const changes = [];
            if (missing.length) { patch.category_values = wanted; changes.push(`added ${missing.join(', ')}`); }
            if (current.product_url !== DETAIL_PRODUCT.productUrl) { patch.product_url = DETAIL_PRODUCT.productUrl; changes.push(`product_url → ${DETAIL_PRODUCT.productUrl}`); }
            if (!(current.service_owner ?? []).some((u) => u.emailAddress === DETAIL_PRODUCT.serviceOwner.emailAddress)) {
                patch.service_owner = [owner.documentId];
                changes.push(`service_owner → ${DETAIL_PRODUCT.serviceOwner.displayName}`);
            }
            if (changes.length === 0) { console.log(`  skip   ${id} already present with every value`); }
            else {
                const put = await api(`/products/${current.documentId}`, { method: 'PUT', body: JSON.stringify({ data: patch }) });
                if (put.ok) console.log(`  update ${id}  ${changes.join('; ')}`);
                else console.log(`  FAIL   ${id}  ${put.status} ${(await put.text()).slice(0, 90)}`);
            }
        }
        else {
            const res = await api('/products', {
                method: 'POST',
                body: JSON.stringify({
                    data: {
                        title: DETAIL_PRODUCT.title, cmdb_sys_id: id,
                        short_description: DETAIL_PRODUCT.description,
                        long_description: DETAIL_PRODUCT.description,
                        product_url: DETAIL_PRODUCT.productUrl,
                        parent_category: 'Test Dependency', state: 'Active',
                        service_owner: [owner.documentId],
                        category_values: DETAIL_PRODUCT.slugs.map((s) => slugToId.get(s)),
                        publishedAt: new Date().toISOString(),
                    },
                }),
            });
            if (res.ok) {
                created += 1;
                detailDocumentId = (await res.json())?.data?.documentId ?? null;
                console.log(`  create ${id}  ${DETAIL_PRODUCT.title}  [${DETAIL_PRODUCT.slugs.join(' + ')}]`);
            }
            else console.log(`  FAIL   ${id}  ${res.status} ${(await res.text()).slice(0, 90)}`);
        }
        if (detailDocumentId) {
            const user = await ensurePluginUser(DETAIL_PRODUCT.contact);
            const state = await ensureProductContact(detailDocumentId, user.id, DETAIL_PRODUCT.contact.role);
            console.log(`  ${state === 'present' ? 'skip  ' : 'update'} ${id} contact ${DETAIL_PRODUCT.contact.displayName} (${DETAIL_PRODUCT.contact.role}): ${user.created ? 'user created, ' : ''}contact ${state}`);
        }
    }

    // Volume top-up. Counted through the FRONTEND rather than the store, because the filter's own
    // notion of what matches is the only one that decides whether a page-two link appears — a store
    // count would include drafts, disabled values and anything the filter excludes for its own reasons.
    // The type vocabulary, read from the store rather than listed here, so a renamed or added value is
    // picked up instead of quietly skewing the spread. Defect values are excluded: they exist to be
    // introduced deliberately and would otherwise arrive attached to unrelated filler products.
    const DEFECT_SLUGS = new Set(['decomissioned-typo', 'decommissioned-trailing-space', 'decommissioned-allcaps', 'live-trailing-space', 'tbc-placeholder']);
    const VOCABULARIES = ['Phase', 'Channel', 'Type', 'Business area'];
    const vocab = new Map();
    for (const name of VOCABULARIES) {
        const res = await api(`/category-values?filters%5Bcategory_type%5D%5Bname%5D%5B%24eq%5D=${encodeURIComponent(name)}&pagination%5BpageSize%5D=200`);
        if (!res.ok) throw new Error(`CMS returned ${res.status} listing '${name}' category values`);
        const values = ((await res.json())?.data ?? [])
            .filter((v) => !DEFECT_SLUGS.has(v.slug) && !v.parent);
        if (!values.length) throw new Error(`No '${name}' values — fillers would all land in its not-categorised bucket.`);
        vocab.set(name, values.map((v) => v.documentId));
    }
    console.log(`  spreading fillers across ${VOCABULARIES.map((n) => `${vocab.get(n).length} ${n}`).join(', ')}`);

    // USER-GROUP COVERAGE: every enabled User group value, at every level, is carried by at least one
    // Active product. The bulk taxonomy assignment never reaches the second and third levels, so the
    // click-through tests under "Education provider and early years workforce" (US277AC5) navigated
    // to category pages with nothing in them — the page renders its product list only when something
    // is in it — and a keyword search for a value's name ("Child or young person", US285AC4) found
    // nothing. One filler per uncovered value, carrying that value plus one value from each of the
    // four other vocabularies (the invariant above: a filler missing a vocabulary lands in that
    // vocabulary's not-categorised bucket). Idempotent by the rule itself: a value with a product is
    // not uncovered, so a second run creates nothing.
    const ugRes = await api('/category-values?filters%5Bcategory_type%5D%5Bname%5D%5B%24eq%5D=User%20group'
        + '&pagination%5BpageSize%5D=500&pagination%5BwithCount%5D=true'
        + '&fields%5B0%5D=name&fields%5B1%5D=slug&fields%5B2%5D=enabled&populate%5Bproducts%5D%5Bfields%5D%5B0%5D=id');
    if (!ugRes.ok) throw new Error(`CMS returned ${ugRes.status} listing User group values`);
    const ugBody = await ugRes.json();
    if (!Array.isArray(ugBody?.data)) throw new Error('CMS response had no data array listing User group values');
    if ((ugBody.meta?.pagination?.total ?? 0) > ugBody.data.length) {
        throw new Error(`Listed ${ugBody.data.length} of ${ugBody.meta.pagination.total} User group values — raise the page size rather than cover a subset.`);
    }
    const uncovered = ugBody.data.filter((v) => v.enabled !== false && !(v.products ?? []).length);
    console.log(`  user-group coverage: ${ugBody.data.length} values, ${uncovered.length} without a product`);
    let groupSeq = 1 + Math.max(0, ...existing
        .map((r) => /^TESTDEP-G(\d+)$/.exec(r.cmdb_sys_id ?? ''))
        .filter(Boolean)
        .map((m) => Number(m[1])));
    for (const [i, v] of uncovered.entries()) {
        const id = `${PREFIX}-G${String(groupSeq++).padStart(3, '0')}`;
        const res = await api('/products', {
            method: 'POST',
            body: JSON.stringify({
                data: {
                    title: `User group ${v.name} filler`, cmdb_sys_id: id,
                    short_description: `Carries the user group "${v.name}" so its category page and search have a product. Seeded for the automated test suite.`,
                    long_description: `Carries the user group "${v.name}" so its category page and search have a product. Seeded for the automated test suite.`,
                    product_url: `https://example.org/${id.toLowerCase()}`,
                    parent_category: 'Test Dependency', state: 'Active',
                    category_values: [
                        v.documentId,
                        ...VOCABULARIES.map((name) => {
                            const list = vocab.get(name);
                            return list[(i + name.length) % list.length];
                        }),
                    ],
                    publishedAt: new Date().toISOString(),
                },
            }),
        });
        if (res.ok) created += 1;
        else { console.log(`  FAIL   ${id} (${v.name})  ${res.status} ${(await res.text()).slice(0, 80)}`); break; }
    }
    if (uncovered.length) console.log(`  covered ${uncovered.length} user-group value(s) with one filler each`);

    // A top-up that does not move its own measure is refused loudly rather than repeated. Each query is
    // re-measured after its fillers are written; a rise smaller than the number created means the
    // fillers do not match the filter as the frontend applies it (or the measure is stale), and the
    // next run would create the same fillers again — which is exactly how 54 bare fillers accumulated.
    const topUpDisagreements = [];
    for (const m of MINIMUMS) {
        const before = await describeMeasure(m.query);
        const have_ = before.n;
        const short = Math.max(0, m.atLeast - have_);
        if (short === 0) { console.log(`  ok     ${m.query} already has ${have_} (needs ${m.atLeast})${before.note}`); continue; }
        console.log(`  fill   ${m.query} has ${have_}, needs ${m.atLeast} — creating ${short}${before.note}`);
        const createdBefore = created;
        // A filler carries a value from EVERY vocabulary, not only the one it exists for. The site
        // offers a "not categorised" filter per vocabulary, so a filler missing a value is not neutral
        // in that vocabulary — it is a member of that bucket.
        //
        // This was learned twice. Seventy-two fillers with no type shifted seven type-search tests and
        // cost a run. Adding a type fixed those and broke two phase tests the same way, because the
        // same fillers still had no phase. Fixing one vocabulary at a time just moves the distortion
        // along, so all four are filled at once.
        //
        // The exception is the filler that exists FOR a not-categorised filter, which must stay bare:
        // there, absence is the value being tested.
        //
        // A vocabulary the query already pins (phase=public-beta pins Phase) is not spread as well:
        // a filler with two phases would count under both, which is a second distortion of the kind
        // the spread exists to avoid, and a phase is one thing a product is in.
        const pinnedTypes = new Set(m.slugs.map((s) => slugToType.get(s)));
        const spreadAcross = m.query.includes('__not_categorised__') ? [] : VOCABULARIES.filter((v) => !pinnedTypes.has(v));
        for (let i = 0; i < short; i += 1) {
            const id = `${PREFIX}-F${String(fillerSeq++).padStart(3, '0')}`;
            if (have.has(id)) continue;
            const res = await api('/products', {
                method: 'POST',
                body: JSON.stringify({
                    data: {
                        // Capitalised so the CMS (binary sort, capitals first) and COMPASS (case-insensitive) order fillers alike.
                        title: `${m.query.replace(/[^a-z]+/gi, ' ').trim().replace(/^[a-z]/, (c) => c.toUpperCase())} filler ${i + 1}`, cmdb_sys_id: id,
                        short_description: 'Volume filler so a pagination test has a second page. Seeded for the automated test suite.',
                        long_description: 'Volume filler so a pagination test has a second page. Seeded for the automated test suite.',
                        product_url: `https://example.org/${id.toLowerCase()}`,
                        parent_category: 'Test Dependency', state: 'Active',
                        ...((m.slugs.length || spreadAcross.length)
                            ? {
                                category_values: [
                                    ...m.slugs.map((s) => slugToId.get(s)),
                                    // One value per vocabulary, by rotating index, so the fillers land
                                    // across each list rather than piling onto one value of it.
                                    ...spreadAcross.map((name) => {
                                        const list = vocab.get(name);
                                        return list[(i + name.length) % list.length];
                                    }),
                                ],
                            }
                            : {}),
                        publishedAt: new Date().toISOString(),
                    },
                }),
            });
            if (res.ok) created += 1;
            else { console.log(`  FAIL   ${id}  ${res.status} ${(await res.text()).slice(0, 80)}`); break; }
        }
        const madeHere = created - createdBefore;
        const after = await measure(m.query);
        if (after < have_ + madeHere) {
            topUpDisagreements.push(`${m.query}: measured ${have_}, created ${madeHere}, now measures ${after} (expected at least ${have_ + madeHere})`);
            console.log(`  DISAGREEMENT ${m.query} measures ${after} after creating ${madeHere} on top of ${have_}`);
        }
    }
    if (topUpDisagreements.length) {
        console.log('\nThe top-up did not move its own measure. The fillers exist but the frontend does not count them,');
        console.log('and re-running would create them again. Stopping here rather than verifying numbers that cannot be right:');
        for (const d of topUpDisagreements) console.log(`  ${d}`);
        process.exit(1);
    }

    // The verification that matters. A created record proves a write succeeded; only the search proves
    // the thing the test actually does. These are reported per-term because one term failing to match
    // while five succeed is the interesting case, and a total would hide it.
    //
    // These reads go through pageBody's fresh page number, so they are not served from the listing cache:
    // a term asked for earlier in the cache window would otherwise report the count from before this
    // loader's writes.
    //
    // If a number here looks wrong, query the CMS directly for ground truth. Do not rename products to
    // chase it.
    console.log(`\nCreated ${created}. Verifying through the frontend search:`);
    let unmatched = 0;
    for (const r of [...RECORDS, ...EXTRA_RECORDS, ...CONTACTS, ...KEYWORD_CHECKS]) {
        const n = await searchCount(r.searchedFor);
        if (n === 0) unmatched += 1;
        console.log(`  ${n === 0 ? 'STILL ZERO' : String(n).padStart(10)}  "${r.searchedFor}"  (${r.neededBy})`);
    }
    // Combinations are verified through the SAME query the filter panel builds, not by re-reading the
    // record we just wrote. A product can carry every value and still not be returned — a value that is
    // disabled, unpublished, or attached to a type the panel does not filter on all produce a record
    // that looks right and a filter that finds nothing.
    console.log('\nVerifying pagination volumes through the frontend:');
    for (const m of MINIMUMS) {
        const { n, note } = await describeMeasure(m.query);
        if (n < m.atLeast) unmatched += 1;
        console.log(`  ${String(n).padStart(10)}${n < m.atLeast ? ' SHORT' : '     '}  ${m.query} (needs ${m.atLeast})  (${m.neededBy})${note}`);
    }

    console.log('\nVerifying filter combinations through the frontend:');
    for (const c of COMBINATIONS) {
        const query = c.slugs
            .map((s) => (slugToParam.get(s) ? `${slugToParam.get(s)}=${encodeURIComponent(s)}` : null))
            .filter(Boolean).join('&');
        const unmapped = c.slugs.filter((s) => !slugToParam.get(s));
        const n = await totalResults(query);
        if (n === 0) unmatched += 1;
        console.log(`  ${n === 0 ? 'STILL ZERO' : String(n).padStart(10)}  ${query}`
            + (unmapped.length ? `  [UNMAPPED category type for: ${unmapped.join(', ')}]` : '')
            + `  (${c.neededBy.replace(/^ValidateFilterLogicUpdate_ANDLogic_SearchFunctionality/, '')})`);
    }

    if (unmatched) {
        console.log(`\n${unmatched} term(s) still return nothing. The record exists but the search does not match it —`);
        console.log('which is a fact about how FIPS searches, not a failed write. Do not "fix" it by renaming');
        console.log('the product until you know which fields the search covers.');
        process.exitCode = 1;
    }
};

run().catch((e) => { console.error(e.message); process.exit(1); });
