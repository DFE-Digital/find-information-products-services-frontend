# Browser-driven tests

The suite that used to live in its own repository, copied here so that the application and the
tests that describe it change in the same commits. It drives a real browser (Playwright) against a
running instance of the application: on this machine, one you start; in the pipeline, a copy the
job starts and seeds itself (see "Seed data"). Its tests name the products, categories, and user
groups of one seeded data set, so each change to the application or to that data shows exactly
which of these tests it moves.

## Run it against the application on this machine

Build once, and install the browser once:

```
dotnet build tests/FipsFrontend.EndToEnd
pwsh tests/FipsFrontend.EndToEnd/bin/Debug/net10.0/playwright.ps1 install chromium
```

Point the suite at the running application. It is configured the way the application is:
`testsettings.json` is the tracked template (every value empty or a default, with a hint per
section); `testsettings.local.json` beside it (gitignored) holds this machine's values; and any key
can be given as an environment variable instead (`Target__ApplicationUrl`), which is how the
pipeline does it. The rules - a section is all-or-nothing, an empty value counts as absent, the
address must be absolute - are stated and enforced in `FIPSAutomation/Configuration/`. For an
application on this machine with no sign-in:

```json
{
  "Target": { "ApplicationUrl": "http://localhost:5505/" },
  "Timeouts": { "ExpectMs": 1000, "ActionMs": 3000, "NavigationMs": 5000 }
}
```

The timeouts are how long an assertion, an action, or a navigation waits before failing; the
template carries Playwright's defaults, which suit a hosted environment. An
application on the same machine answers in milliseconds, and every failing test waits the whole
timeout, so set them low there or a run is mostly waiting.

At that pace the suite exceeds the application's request limiter at its default, and every page
after that is a 429 that a test reports as a missing heading or filter. Start the
application with `RateLimiting__PermitLimitPerWindow=1000` (the pipeline's `appsettings.ci.json` does), and
read a run's trx with `dotnet run tests/FipsFrontend.EndToEnd/tools/summarise-trx.cs -- <trx>
--app-log <application log>`, which names the tests that ran while the limiter was refusing.

Start the application (see the repository README). To run it exactly as the pipeline does - the
`ci` environment, whose settings are the committed `src/FipsFrontend/appsettings.ci.json` and whose
content source is the stub below - run the published output, since static files are served from a
build folder only under `Development`:

```
dotnet publish src/FipsFrontend/FipsFrontend.csproj -c Release -o out
ASPNETCORE_ENVIRONMENT=ci dotnet out/FipsFrontend.dll --urls http://localhost:5505
```

Then:

```
dotnet test tests/FipsFrontend.EndToEnd --logger "console;verbosity=normal"
```

The console logger at normal verbosity shows the suite's start-up line, which names the timeouts
the run uses and says when they are Playwright's defaults, and each test as it finishes; the same
line is in the trx and the report either way. A report is written to `tests/FipsFrontend.EndToEnd/playwright-report/`, with a screenshot of each
failing test. The suite's own rules - its settings rules and the url comparison behind its pagination
assertions - are tests in `tests/FipsFrontend.EndToEnd.Rules`, a project with no browser in it: they
run in seconds anywhere, and the pipeline gates on them before it starts a browser.

```
dotnet test tests/FipsFrontend.EndToEnd.Rules
```

The pipeline also measures which of the application's code the browser suite reaches: it starts the
published application under `dotnet-coverage`, stops it gracefully after the run so the collector can
write its file, and puts that figure on the summary page beside the combined figure with the
in-process scenarios. Locally the same is `dotnet-coverage collect --session-id suite --output-format
cobertura --output app.cobertura.xml -- dotnet out/FipsFrontend.dll --urls …`, then
`dotnet-coverage shutdown suite` once the tests have run.

For a run with no content, `tests/FipsFrontend.Tests.StubCmsApi` stands in for the content source
and answers every request with an empty collection:

```
dotnet run --project tests/FipsFrontend.Tests.StubCmsApi --urls http://127.0.0.1:1338
```

and point the application at it with `CmsApi__BaseUrl=http://127.0.0.1:1338/api`. The application
then renders every page with no data and answers quickly, so every failure the suite reports is
about the pages.

## Seed data

The page objects and tests under `FIPSAutomation/` name the products, categories, user groups,
search terms, and contacts of one seeded data set: the hosted environment's (`testdata.xlsx`), which
the local composition's seed reproduces with visibly synthetic contact names such as "Alpha
Testcontact". Every such literal depends on that seed, so a change to the seed and to the tests
that name the changed value must land together.

Tests that need that content carry the `Integration` category, on the class where every test in it
does and on the test otherwise. Against an instance with no content they are red by design, and the
rest must be green, so the two are run apart:

```
dotnet test tests/FipsFrontend.EndToEnd --filter "TestCategory!=Integration"   # expected green anywhere
dotnet test tests/FipsFrontend.EndToEnd --filter "TestCategory=Integration"    # green only against the seed
```

The pipeline seeds its content source (below), so it runs the whole suite as one step, gated by the
known-green check (which ignores the tests listed as flaky, so one timing flake does not fail the
job). The category is for a run against an instance with no content. A test that starts naming a
seeded value gets the category with it; a test that passes against the empty content stub without
it is wrongly tagged.

### The seed itself

`seed/` holds the data set as text and the scripts that load it into a Strapi content source, with
one entry point. Its README says which files are hand-authored and which are generated (named
`*.generated.json`, regenerated by a script beside them rather than edited), and where a new
seeded value goes:

```
cd tests/FipsFrontend.EndToEnd/seed
npm run cms:start -- --cms <checkout of the CMS>     # runs Strapi on 1337 in this terminal; prints TOKEN=... once ready
npm run cms:seed -- --fips http://localhost:5506      # from another terminal; the url is an application reading that Strapi
npm run cms:stop                                      # or Ctrl+C where it runs
```

The pipeline runs exactly those, with the CMS checked out at the commit `CMS_REF` in the workflow
pins and the seeded database cached by the hash of `seed/` and that commit, so a run regenerates it
only when one of them changes and otherwise pays a Strapi start, which the cache does not cover; the
run's own timings say what each costs. Each test opens the page it needs itself: a filtered run selects tests out of their
fixture's order, so a test that relies on the one before it having navigated somewhere fails for
that reason alone.

## The tests known to pass

`known-green.txt` lists the tests that pass, and `Check-KnownGreen.ps1 -Trx <file>` refuses a run in
which one of them did not: a test that stays red is allowed, a test that is removed is allowed, and
a test brought to green is added to the list by whoever brings it there (`-Record` rewrites the list
from a run). `known-flaky.txt` lists tests seen both green and red against the same build: the check
reports their outcome and never fails on them, and each is a signpost to a test to fix (usually a
wait) and then move to the green list. The pipeline runs the suite against the published application with the empty content source,
puts the counts and this check's verdict on the run's summary page, and keeps the results as an
artefact; it does not gate on either.

## Against a hosted environment

Set `Target:ApplicationUrl` to that environment's address in `testsettings.local.json`, and fill the
`SignIn` section (all four values, plain text, not encoded) so the suite signs in through the
identity provider first; leave the section empty for an environment with no sign-in.

The report embeds a full-page screenshot of every failing test, so against a hosted environment it
holds real content: it stays on your machine (`playwright-report/` is gitignored), and the pipeline
uploads a report only when the target is the copy of the application it started itself.

Reference: https://playwright.dev/dotnet/
