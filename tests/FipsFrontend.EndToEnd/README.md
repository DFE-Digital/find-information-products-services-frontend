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

Point the suite at the running application. `env.json` is a tracked template whose values are
placeholders; copy it to `env.local.json` beside it (gitignored, and preferred when present), and set
`applicationURL` to the address the application is listening on, with `loginRequired` false when the
application asks for no sign-in:

```json
{
  "activeEnv": "local",
  "loginRequired": false,
  "envs": [{ "env": "local", "applicationURL": "http://localhost:5505/" }],
  "timeouts": { "expectMs": 1000, "actionMs": 3000, "navigationMs": 5000 }
}
```

The timeouts are how long an assertion, an action, or a navigation waits before failing; the
template carries Playwright's defaults (5, 30, and 30 seconds), which suit a hosted environment. An
application on the same machine answers in milliseconds, and every failing test waits the whole
timeout, so set them low there or a run is mostly waiting.

Start the application (see the repository README), then:

```
dotnet test tests/FipsFrontend.EndToEnd
dotnet test tests/FipsFrontend.EndToEnd --filter "TestCategory=functional"
```

A report is written to `tests/FipsFrontend.EndToEnd/playwright-report/`, with a screenshot of each
failing test.

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
from a run). The pipeline runs the suite against the published application with the empty content source,
puts the counts and this check's verdict on the run's summary page, and keeps the results as an
artefact; it does not gate on either.

## Against a hosted environment

Set `activeEnv` to `dev`, `test`, or `prod` in `env.local.json` with that environment's `applicationURL`
and `oAuthURL`, and with `loginRequired` true give `userName` and `password` base64-encoded.

Reference: https://playwright.dev/dotnet/
