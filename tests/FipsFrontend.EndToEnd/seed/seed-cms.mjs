#!/usr/bin/env node
// Seeds a running Strapi with everything the browser suite names, in the order the loaders need, and times each
// step. Stops at the first step that fails, since every later one builds on it.
//
//   CMS_BASE_URL=http://127.0.0.1:1337 CMS_FULL_API_KEY=<token> FIPS_BASE_URL=http://localhost:5506 node seed-cms.mjs
//
// FIPS_BASE_URL must be an application reading THIS Strapi: the test-required loader tops up filler products by
// counting through the listing page, because the page's filter is the only judge of whether a second page appears.
// Pointed at any other application it counts that one's content and skips the top-up.
import { spawnSync } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const steps = [
  ['load-cms-taxonomy.mjs', '--confirm'],
  ['load-cms-taxonomy.mjs', '--confirm', '--include-deep'],
  ['load-cms-products.mjs', '--confirm'],
  ['assign-product-categories.mjs', '--confirm'],
  ['set-category-descriptions.mjs', '--confirm'],
  ['load-test-required-products.mjs', '--confirm'],
];

for (const [script, ...args] of steps) {
  const started = Date.now();
  const result = spawnSync(process.execPath, [join(here, script), ...args], { stdio: 'inherit', env: process.env });
  const seconds = ((Date.now() - started) / 1000).toFixed(1);
  console.log(`\n== ${script} ${args.join(' ')}: exit ${result.status} in ${seconds}s\n`);
  if (result.status !== 0) process.exit(result.status ?? 1);
}
