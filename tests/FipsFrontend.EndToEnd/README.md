# Browser-driven tests

The suite that used to live in its own repository, copied here as it stood so that the application
and the tests that describe it change in the same commits. It drives a real browser (Playwright)
against a running instance of the application, so it is not part of the pipeline's test run, and it
does not yet pass in full: many of its tests expect the products, categories, and user groups of one
hosted environment (see `testdata.xlsx`), and it was last maintained against pages this repository
is changing. Each change to the application now shows exactly which of these tests it moves.

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
template carries Playwright's defaults (5, 30, and 30 seconds), which suit a hosted environment. An
application on the same machine answers in milliseconds, and every failing test waits the whole
timeout, so set them low there or a run is mostly waiting.

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
dotnet test tests/FipsFrontend.EndToEnd --filter "TestCategory!=Configuration"
dotnet test tests/FipsFrontend.EndToEnd --filter "TestCategory=functional"
```

A report is written to `tests/FipsFrontend.EndToEnd/playwright-report/`, with a screenshot of each
failing test. The `Configuration` category is the suite's own tests of its settings rules; they need
no browser, run in seconds, and the pipeline gates on them - hence the filter that leaves them out of
a browser run, where the known-green check would otherwise list them.

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
