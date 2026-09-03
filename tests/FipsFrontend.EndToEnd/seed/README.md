# Seed for the browser suite's content source

The browser suite names one seeded data set (see the suite README, "Seed data"). This folder is that data set
as text, the scripts that load it into a Strapi content source, and the generators that produce the parts of
it that are derived rather than written. It is self-contained: the loaders read their inputs relative to this
folder, and everything a generated file was made from is committed beside it.

## What is here, and where to make a change

Files whose name ends `.generated.json` are outputs. Editing one by hand works until the next regeneration
overwrites it, so a change to their content goes into the generator or its inputs, and the file is regenerated.
Everything else is hand-authored and edited in place.

| File | Origin | To change it |
| --- | --- | --- |
| `scenarios/cms-taxonomy.json` | Hand-authored: the category types and their labels | Edit in place |
| `scenarios/taxonomy-from-testdata-xlsx.generated.json` | Generated from `../testdata.xlsx`, the suite's data sheet: the category values and the three-level user groups the tests name | Change the sheet, then `npm run generate:taxonomy` |
| `scenarios/scale.cms.generated.json` | Generated: synthetic products for volume, as many as the wrapper asks for, deterministic from a fixed seed, addresses under `example.org` | Change `generators/generate-scale.mjs` or the parameters in `generators/regenerate-scale.mjs`, then `npm run generate:scale` |
| `load-test-required-products.mjs` | Hand-authored data inside a loader: the named products, contacts, and per-category minimums the tests assert on, plus the filler top-up | Edit in place; keep it in step with the tests that name the same values |
| `lib/category-assignment-rule.mjs` | Hand-authored: how products are spread across category values | Edit in place |

A new seeded product that a test will name goes in `load-test-required-products.mjs`, not in the scale set:
the scale set is regenerated wholesale and carries nothing a test refers to by name.

The generators:

- `generators/generate-scale.mjs` produces the synthetic products; `generators/regenerate-scale.mjs` runs it
  with the committed parameters and writes only the CMS projection, stamped with how it was made.
- `generators/extract-taxonomy-from-testdata.cs` reads the data sheet with ClosedXML. It is a file-based C#
  program, run with the .NET SDK; the package version comes from the repository's central package file.

Both are deterministic: running them without changing their inputs changes nothing, which is how a reviewer
can check that a committed generated file is what its inputs say it should be.

## Loading it

`cms.mjs` is the one entry point, driven by `package.json` scripts:

| Script | What it does |
| --- | --- |
| `npm run cms:start -- --cms <cms checkout> [--port 1337] [--log <file>]` | Runs Strapi on SQLite with throwaway placeholder secrets in the foreground, like any development server: its output goes to the terminal and to the log file, and Ctrl+C stops it. Once it answers, creates the first admin (or logs in on a cached database), mints a full-access token, and prints it as `TOKEN=` on stdout |
| `npm run cms:seed -- --fips <url> [--port 1337]` | Runs `seed-cms.mjs`: the loaders in order, timed |
| `npm run cms:stop -- [--port 1337]` | Stops the Strapi started by `cms:start` |

The loaders drive Strapi's REST API with the token. `load-test-required-products.mjs` also tops up filler
products so the tests that turn a page have a second page to turn; it counts through the listing page of an
application reading the same store, which is what `--fips` points at. The placeholder secrets are fine for a
throwaway database and must never be a real environment's.

The seeded database is a build output: the pipeline caches it by the hash of this folder and the CMS commit
it pins, and regenerates it only when one of those changes. A regeneration is a Strapi start plus the seed; a
cache hit is a Strapi start.

## Running it on this machine

Strapi 5 needs Node 22. With the CMS repository checked out at the commit the pipeline pins (`CMS_REF` in the
workflow) and `npm ci` run there, from this folder:

```
npm run cms:start -- --cms <cms checkout>            # stays in this terminal; Ctrl+C stops it
# in another terminal: start the application against http://127.0.0.1:1337 on, say, port 5506, then
npm run cms:seed -- --fips http://localhost:5506
# run the suite; afterwards, Ctrl+C in the first terminal, or
npm run cms:stop
```
