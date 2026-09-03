#!/usr/bin/env node
// Regenerates scenarios/scale.cms.generated.json: the 250 synthetic products the suite uses for volume. Runs
// generate-scale.mjs into a temporary folder and keeps only the CMS projection, stamped with how it was made.
// Deterministic, so running this without changing the generator or its parameters changes nothing.
//
//   npm run generate:scale
import { spawnSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const parameters = { count: 250, seed: 1 };
const target = join(here, '..', 'scenarios', 'scale.cms.generated.json');

const work = mkdtempSync(join(tmpdir(), 'scale-'));
try {
  const result = spawnSync(process.execPath, [join(here, 'generate-scale.mjs'), '--count', String(parameters.count), '--seed', String(parameters.seed), '--out', join(work, 'scale')], { stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
  const projection = JSON.parse(readFileSync(join(work, 'scale.cms.json'), 'utf8'));
  const output = {
    _generated: { by: 'generators/regenerate-scale.mjs', ...parameters, deterministic: true, note: 'Do not hand-edit; change the generator or its parameters and run npm run generate:scale.' },
    products: projection.products,
  };
  writeFileSync(target, JSON.stringify(output, null, 1) + '\n');
  console.log(`wrote ${target}: ${output.products.length} products`);
} finally {
  rmSync(work, { recursive: true, force: true });
}
