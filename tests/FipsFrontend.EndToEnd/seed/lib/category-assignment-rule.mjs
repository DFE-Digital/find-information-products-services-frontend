// The one rule that decides which category values a seeded product carries. Keyed by the product's CMDB id,
// falling back to its title, never by its position in a listing, so that any store seeded from the same
// products assigns the same values whatever else it holds, and two runs agree.
//
// The assignments are invented: nothing upstream records which product is in Alpha or reaches users by SMS,
// so a count derived from them describes this fixture and not the department. They are deterministic so that
// a difference between two runs, or two stores, means something changed elsewhere.

/** Every Nth product (by index) is left without a value of that type: the site has a "Not categorised" filter and tests assert on it. Different strides so the gaps do not coincide. */
export const UNCATEGORISED_EVERY = { phase: 11, channel: 13, type: 17, 'business-area': 19, 'user-group': 23 };

/** One value per type at index * stride; different strides so phase and channel do not move together and a combined filter has something to combine. */
export const STRIDE = { phase: 1, channel: 2, type: 3, 'business-area': 5, 'user-group': 7 };

/** Deliberately malformed values, kept out of routine assignment so they can be introduced on purpose in one experiment. */
export const DEFECT_SLUGS = new Set(['decomissioned-typo', 'decommissioned-trailing-space', 'decommissioned-allcaps', 'live-trailing-space', 'tbc-placeholder']);

/** FNV-1a over UTF-16 code units, unsigned 32-bit: small, dependency-free, and stable across runtimes. */
function fnv1a32(text) {
    let hash = 0x811c9dc5;
    for (let i = 0; i < text.length; i += 1) {
        hash ^= text.charCodeAt(i);
        hash = Math.imul(hash, 0x01000193) >>> 0;
    }
    return hash;
}

/** The product's index under the rule: from its CMDB id when it has one, else from its title. */
export function productIndex(cmdbId, title) {
    const id = (cmdbId ?? '').trim();
    const key = id ? `cmdb:${id.toUpperCase()}` : `title:${(title ?? '').trim().toLowerCase().replace(/\s+/g, ' ')}`;
    return fnv1a32(key);
}

/** Values in the order the rule indexes them: by slug, the same on both sides because both read the same fixture. */
export function sortBySlug(values) {
    return [...values].sort((a, b) => a.slug.localeCompare(b.slug));
}

/**
 * The picks for one product: for each type slug in `listsByType` (values already sorted by slug and
 * free of defect slugs), at most one value. Returns [{ type, value }].
 */
export function picksFor(index, listsByType) {
    const picks = [];
    for (const [type, list] of listsByType) {
        if (!list.length) continue;
        const skipEvery = UNCATEGORISED_EVERY[type];
        if (skipEvery && index % skipEvery === 0) continue;
        const stride = STRIDE[type] ?? 1;
        picks.push({ type, value: list[(index * stride) % list.length] });
    }
    return picks;
}
