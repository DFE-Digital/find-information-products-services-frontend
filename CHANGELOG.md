# Changelog

All notable changes to this service - its behaviour, its build, how it is deployed - are recorded
here, newest first, as part of the change that makes them. Entries say why a change was made, not
what the diff already shows.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), with one addition.
Its groups - Added, Changed, Deprecated, Removed, Fixed, Security - describe what a **user** of the
service meets, and are used only for that. Everything behind the scenes - deployment, build,
tooling, repository hygiene, dependencies, operator-facing endpoints - goes under **Maintenance**,
each entry led by its area in bold. Under Unreleased every group is listed, empty ones as "None",
so a contributor sees where an entry belongs; when a version is cut, the empty groups are dropped,
so a version showing Maintenance alone changed nothing for users.

Versions are calendar-based tags on `main`, `vYYYY.MM.DD-NNNN` - the date and the zero-padded
count of versions cut that day (it resets daily, and is padded so tags sort) - made by renaming
the Unreleased heading to that version in the commit that is tagged and starting a fresh
Unreleased above it; the date is in the version, so headings carry no separate date. Directly
under each version heading, one sentence says what the version is for. If that takes more than
a sentence or two, the version is carrying unrelated changes and should have been cut sooner.
Before the first entry below there were no version numbers, tags, or releases; the running
application reported `1.0.0` for every build.

To cut a version: rename `## [Unreleased]` to `## [vYYYY.MM.DD-NNNN]`, write its one-sentence
summary under the heading, delete its "None" groups, and paste this above it:

```markdown
## [Unreleased]

### Added

- None.

### Changed

- None.

### Deprecated

- None.

### Removed

- None.

### Fixed

- None.

### Security

- None.

### Maintenance

- None.
```

## [Unreleased]

### Added

- None.

### Changed

- None.

### Deprecated

- None.

### Removed

- None.

### Fixed

- None.

### Security

- None.

### Maintenance

- **Content source:** a contracts project, `src/Compass.FipsApi.Contracts`, holds the records for the
  service-register API the service will read products from, generated from that API's own source rather
  than written by hand or from its documentation, which describes shapes the API does not send.
  Every member is optional; a member the records do not name is observed and logged once, never rejected.
  Tests parse responses recorded from a local instance holding synthetic seed data (no real products) through
  the records, and hold the seeded rows to known values, so a change on either side fails as a disagreement.
  Nothing reads the records yet.
- **Footer:** the accessibility statement's address is the `AccessibilityStatement:Url` setting, with the departmental
  statement as the default, so an instance can point the link at its own statement; the link itself is never absent.
- **Feedback:** the feedback store's address is the `Airtable:BaseUrl` setting, defaulting to the Airtable service, so a
  local or pipeline instance can send the form to a stand-in and the browser suite can see it succeed.
- **Request limiter:** a caller refused for its pace now gets 429 Too Many Requests with a Retry-After header;
  previously the limiter answered its default 503, which read as the service being down. The limit and window are
  the `RateLimiting` settings, defaulting to what the constant was, so the pipeline's copy can admit the browser
  suite's pace and a deployed instance admits exactly what it did before.
- **Browser suite:** pagination assertions check the route and query a click produced, not which host served it,
  so they pass against any environment; the comparison parses the url and treats query order as irrelevant, and
  holds those decisions in fifteen browser-free tests.
- **Browser suite:** link locators that a longer link name could also satisfy now match exactly, so strict mode
  reports a real ambiguity rather than a substring. One test file is converted from Windows-1252 to UTF-8, which is
  what every other file and the compiler on Linux expect.
- **Browser suite:** tests that named a particular mailbox or a person now assert the configured address or the
  seeded contact.
- **Browser suite:** a trx summariser under `tests/FipsFrontend.EndToEnd/tools` groups a run's failures by cause and
  reads them against the known-green list. Against a seeded local copy every test passes; in the pipeline the
  known-green list still gates.

## [v2026.08.28-0001]

FIPS now runs without crashing in the absence of manually set configuration or external systems
(settings are explicit and validated at start-up, with meaningful defaults), the end-to-end Playwright
tests now run in the CI/CD pipeline, and product information is now read-only (the pages to create or
edit it are removed).

### Removed

- The "Propose a change" form on product pages, the "request a new product entry" form, and the admin "create new FIPS entry" form are removed (#315),
  along with the e-mails they sent: the service no longer uses GOV.UK Notify.
  Product information comes from the data source only. The home, updates, and product listing pages say to contact the team for now; #314 will direct people to COMPASS instead.
- The admin area (dashboard, product management, title and description editing, categories, state changes, deletion, and CMDB matching)
  and the product edit page are removed, with the CMS update and delete calls behind them: COMPASS does the matching and holds the record now.
  Nothing in this service edits product information any more; search-term logging is the one remaining write to the CMS.

### Maintenance

- **Tests:** the repository has a test project, `tests/FipsFrontend.Tests`, and the workflow runs it before anything is published.
  Its first scenario keeps the settings template complete: every key the code reads must be named in it.
  The next host the whole application in-process, with every outbound HTTP client replaced by a stand-in,
  and hold that the service's own pages render from this repository without reaching for another system.
  Each run's summary page shows the results and the line and branch coverage; the raw results are kept as an artefact.
- **Build:** package versions for every project live in one file, `Directory.Packages.props`.
- **Configuration:** the settings template names the eight caching and search-logging keys the code reads with defaults, which the new test found missing.
- **Repository:** the web project lives in `src/FipsFrontend` and the loose documents in `docs/`, with a solution file at the root.
  Previously the project was the repository root, which left no room for a test project beside it.
- **Build:** a fresh clone builds with only the .NET SDK and Node.js installed:
  the project file installs the npm packages itself (skipped under CI, where the workflow's `npm ci` does it).
  Previously `npm install` was an unwritten step, and a fresh build failed inside the Sass compiler.
- **Build:** a build no longer rewrites `wwwroot/css/accessible-autocomplete.min.css`, so the working tree stays clean.
  The page also stops fetching that stylesheet twice.
- **Repository:** the README gives the addresses the launch profiles actually use (5505 and 7601).
- **Configuration:** the settings template's placeholders show the shape of each value
  (an all-zero GUID, an empty string, or the code's own default) instead of `YOUR_…` text.
  The unread `Entra` section is removed.
- **Configuration:** `SAS:TenantId` is renamed `SAS:BaseUrl`, which is what the value has always been.
  The old name still works, with a start-up warning, until every hosted app uses the new one.
- **Dependencies:** the application runs on .NET 10 (support for .NET 8 ends in November 2026) with every package at its current release,
  and both vulnerability audits, NuGet and npm, are clean (each reported four advisories before).
  The GOV.UK and MoJ frontend packages are deliberately left as they are, since their updates change what users see.
- **Build:** the stylesheets compile on Node 24 (Node 20 is out of support) with the current Sass; the compiled output is unchanged.
- **Tests:** the browser-driven suite moves in from its own repository as `tests/FipsFrontend.EndToEnd`,
  so the application and its end-to-end tests change in the same commits.
  It does not yet pass in full, so the pipeline runs it against the published application without gating on it,
  reporting the counts and whether every test known to pass still does; `tests/FipsFrontend.Tests.StubCmsApi` stands in for the content source.
  The pipeline runs that copy as its own environment, `ci`, from a committed `appsettings.ci.json` of placeholders,
  and uploads the suite's report (which embeds a full-page screenshot per failure) only when the target is that copy.
  Playwright is brought up to the current release (the old driver crashed part-way through every run),
  and the suite's waits become configuration, sized for an application on the same machine.
  The suite is configured the way the application is - a tracked `testsettings.json`, a gitignored local copy, then environment variables -
  bound to validated options, replacing its own `env.json` and the base64-encoded credentials.
  The pipeline measures which of the application's code the browser suite reaches, and shows it beside the combined figure
  with the in-process scenarios, so the two can be watched as the suite turns green; the suite's own tests of its settings rules gate.
- **Configuration:** a fresh clone runs with nothing configured. `appsettings.json` is committed (every value empty or a default; the template it replaces is gone),
  and each optional section is off when left empty - sign-in, the content source (pages render empty), the assessments service, Redis, search-term logging -
  while a section partly supplied, an address that is not absolute, or a feature switched on without the service it needs, stops the application at start-up naming the keys.
  Previously every page, the error page included, failed with "IDW10106: The 'ClientId' option must be provided", and the listing hung on a content source nobody runs.
- **Configuration:** a developer's machine runs as its own environment, `local-dev` (the launch profiles set it), and an unnamed environment is Production, as the framework intends.
  The developer exception page, the error page's exception details, and the short HSTS header are shown under `local-dev` and `Development` only;
  the exception page and the error details previously showed in every environment, production included. Under `local-dev`, stylesheets and scripts are served from the source tree as they are under `Development`.
  Every environment but the live one carries a phase banner naming it on every page.

## [v2026.08.27-0002]

The service's own pages (about, contact, data, updates, and help) come from this repository rather than the CMS.

### Fixed

- Pages with no view model behind them (privacy policy, page not found, error, cookie preferences, maintenance)
  showed no service navigation, because the layout's check hid the navigation when there was no model to ask.
  The navigation is now shown unless a page's model asks for it to be hidden.

### Maintenance

- **Pages:** the About, Contact, Data, Updates, and Help pages no longer fetch their content from the CMS on every request.
  The copy now lives in this repository as plain page markup, edited and released like any other change.
  Before this change each page requested a CMS record per visit; when the CMS answered nothing,
  four of the five rendered an empty page with no error, and when it was unreachable the empty page arrived after the retry policy gave up.
  The markdown rendering helper and its package go with the CMS records, as nothing else used them,
  and so do the five view models: each page now has the shape of the repository's other content-only pages,
  with its title set in the view and no model.
  Links between these pages are generated from controller and action names rather than typed as paths,
  so a link to a page that no longer exists fails when the page is rendered, not when somebody clicks it,
  and the IDE can check them.
- **Configuration:** the address of the feedback survey ("Give us feedback about this service" in the footer,
  "Give feedback" on the data page) and the mailbox on the contact page are read from configuration,
  as `Feedback:SurveyUrl` and `Contact:Email`, instead of being written into the page markup.
  A blank value hides that link (the application logs a warning at start-up); a value that is not a URL, or not an e-mail address, stops the application starting.
  Until the hosted apps have the application settings `Feedback__SurveyUrl` and `Contact__Email`, the application uses built-in defaults, the current addresses,
  so nothing changes for users; the defaults are to be removed once the settings are in place.
  The settings template now also lists the `Notify` section (API key, mailbox, template IDs) that the notification service has always required but the template never mentioned.

## [v2026.08.27-0001]

Merging to `main` no longer deploys to the public site: one workflow builds once, deploys to `development` on merge, and deploys to `production` only when a person asks, with the build made reproducible along the way.

### Maintenance

- **Build:** tool versions (Node, .NET SDK) are now stated explicitly in the repository,
  allowing them to be shared between developer machines and GitHub Actions/workflows.
  See `.nvmrc` and `global.json` for details (note the versions themselves are unchanged for now but do need updating soon;
  `global.json` now names a real SDK version, `8.0.100`, because the setup action rejects the placeholder `8.0.0`).
- **Build:** builds in CI are now reproducible and deterministic, using both 
  the npm lockfile (`package-lock.json`) and the nuget lockfile (`packages.lock.json`).
  These are used in CI via `npm ci` and `dotnet restore --locked-mode`, respectively.
- **Build:** the GitHub Actions the workflow uses are updated to their current major versions
  (for example `actions/checkout` v4 → v7, `azure/webapps-deploy` v2 → v3),
  and the workflow follows their current/modern conventions rather than older patterns.  
  In particular, dependency caching is now done by the setup actions themselves:
  `package.json` declares its package manager (`"packageManager": "npm@…"`), which switches on `actions/setup-node`'s automatic npm cache,
  and `actions/setup-dotnet` caches NuGet packages keyed on the lockfile.
  Previously, the workflow hand-crafted its own cache key against a lockfile that did not exist, so the cache never matched.
  Every job now also has a time limit.
- **Security:** the workflow grants minimal privileges at each step, in line with secure by design and least privilege principles.
  No permissions are granted workflow-wide; each job declares only what it needs (read access to the repository, nothing more),
  and checkouts no longer keep the repository token on the runner once the job has finished.
  Previously a single blanket grant applied to every job, whether it needed it or not, and the token stayed on the runner.
- **Deployment:** GitHub environments are now configured for the two hosted apps, with environment variables
  being used to instruct the GitHub Actions workflow which app to deploy to. 
  The `development` and `production` environments are now in use, though some additional migration of secrets is pending.
  Where variables for an environment are not yet set, the workflow (rightly) refuses/fails to deploy.
- **Deployment:** merging to `main` no longer automatically deploys to the public site.
  Before this change, every push to `main` deployed to _all_ hosted apps at the same time,
  including the one behind https://fips.education.gov.uk,
  regardless of whether the deployment to lower environments succeeded first.
  Now:
    - a pull request or branch is built and checked before it can be merged into `main` (nothing is deployed)
    - a merge to `main` deploys to the hosted `development` environment, then smoke-tests to confirm the app responds
    - nothing deploys to `production` automatically: once a person is happy that the code on `main` is ready for
      public release, they run the workflow by hand and select the `production` environment as the target.
      The workflow then sanity-checks the change through the lower environments before deploying to `production`.
      If any of those checks fail, the deployment to `production` is aborted for review,
      so nothing that has failed lower down reaches the user-facing site.

  The two old workflows (`azure-webapps-dotnet-core.yml`, `main_find-products-services-test.yml`)
  are removed, replaced by one workflow (`cicd.yml` - "continuous integration and continuous deployment").
  The app is built once, and the same package is deployed wherever it goes.
- **Health:** `/health/detailed` now says which commit is running (`commit`, and
  `informationalVersion` as `1.0.0+<sha>`). 
  Previously it only ever reported a `1.0.0.0` for every build and could not distinguish one deployment from another.
  In the future we may be able to use this in the smoke tests to be absolutely confident of which build is deployed
  (currently not possible due to the authentication in place).
- **Repository:** to prevent any issues, we are now explicit about consistently using UTF-8 encoding and LF line endings.
  This prevents any issues like raw multiplication signs (`×`) being replaced with `?` in the build, or other encoding issues.
  It also removes any churn / noise in diffs caused by line-ending changes.
  See more details in `.editorconfig` and `.gitattributes`.

