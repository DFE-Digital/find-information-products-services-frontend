#!/usr/bin/env node
// The one entry point to a seeded content source for the browser suite. Three commands, all Node, so a developer,
// an agent, and the pipeline run the same thing:
//
//   node cms.mjs start --cms <checkout> [--port 1337] [--db .tmp/data.db] [--log cms.log]
//       Runs Strapi from the checkout on SQLite with placeholder secrets (a throwaway database) in the foreground,
//       like any development server: its output goes to this terminal and to the log file, and Ctrl+C stops it.
//       Once it answers, gives it a first admin or signs in as the existing one, mints a full-access token, prints
//       TOKEN=<token> on stdout (the only line stdout carries), and records the pid and the token beside this
//       file for the other two commands. The pipeline runs it in the background and waits for that line.
//   node cms.mjs seed --fips <url>
//       Loads the world into the running Strapi (the sequence in seed-cms.mjs). The url is an application reading
//       this Strapi, which the test-required loader counts through to size its filler products.
//   node cms.mjs stop
//       Stops the Strapi that start began.
//
// Strapi 5 needs Node 22. The placeholder secrets are for a database that never leaves the machine; a real
// environment's must never be used here.
import { spawn, spawnSync } from 'node:child_process';
import { createWriteStream, existsSync, mkdirSync, readFileSync, writeFileSync, unlinkSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const command = args[0];
const option = (name, fallback) => { const i = args.indexOf(`--${name}`); return i > -1 ? args[i + 1] : fallback; };

const port = option('port', '1337');
const cmsUrl = `http://127.0.0.1:${port}`;
const stateFile = join(here, `.cms-${port}.json`);
const admin = { email: 'seed@example.com', password: 'SeedPassw0rd!', firstname: 'Seed', lastname: 'Runner' };

const placeholders = {
  DATABASE_CLIENT: 'sqlite',
  HOST: '127.0.0.1',
  APP_KEYS: 'placeholder-key-1,placeholder-key-2',
  API_TOKEN_SALT: 'placeholder-api-token-salt',
  ADMIN_JWT_SECRET: 'placeholder-admin-jwt-secret',
  TRANSFER_TOKEN_SALT: 'placeholder-transfer-token-salt',
  JWT_SECRET: 'placeholder-jwt-secret',
  ENCRYPTION_KEY: 'placeholder-encryption-key-00000',
  // A throwaway instance has nothing to report to Strapi's usage telemetry.
  STRAPI_TELEMETRY_DISABLED: 'true',
  // Strapi opens the admin in a browser when it starts under "development" on a database with no admin user,
  // which a seed database is every time; the CLI's --open option is parsed and never read, and the config key
  // that governs it lives in the CMS repository. Any other environment name switches it off, and the CMS has no
  // per-environment configuration, so this changes nothing else.
  NODE_ENV: 'test',
};

async function post(path, body, jwt) {
  const response = await fetch(`${cmsUrl}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(jwt ? { Authorization: `Bearer ${jwt}` } : {}) },
    body: JSON.stringify(body),
  });
  return { ok: response.ok, status: response.status, json: await response.json().catch(() => ({})) };
}

// Waits for the url to answer, up to the given seconds; gives up at once when the process being waited for is
// already gone, since no amount of waiting changes that answer.
async function waitFor(url, seconds, gone = () => false) {
  for (let i = 0; i < seconds; i += 2) {
    if (gone()) return -1;
    try { if ((await fetch(url)).ok) return i; } catch { /* not yet */ }
    await new Promise((r) => setTimeout(r, 2000));
  }
  return -1;
}

async function mintToken() {
  const registered = await post('/admin/register-admin', admin);
  let jwt = registered.ok ? registered.json.data?.token : undefined;
  if (!jwt) {
    const login = await post('/admin/login', { email: admin.email, password: admin.password });
    if (!login.ok) throw new Error(`neither register-admin (${registered.status}) nor login (${login.status}) succeeded; is this the seed's own database?`);
    jwt = login.json.data?.token;
    console.error('Signed in as the existing admin.');
  } else {
    console.error('Registered the first admin.');
  }
  const token = await post('/admin/api-tokens', { name: `seed-${Date.now()}`, description: 'minted by cms.mjs', type: 'full-access', lifespan: null }, jwt);
  if (!token.ok || !token.json.data?.accessKey) throw new Error(`minting the API token failed: ${token.status} ${JSON.stringify(token.json).slice(0, 200)}`);
  return token.json.data.accessKey;
}

async function start() {
  const cms = resolve(option('cms', '.cms'));
  const db = option('db', '.tmp/data.db');
  const log = resolve(option('log', join(cms, '.tmp', 'cms.log')));
  if (!existsSync(join(cms, 'package.json'))) throw new Error(`no package.json at ${cms}; pass --cms <checkout of the CMS>`);
  mkdirSync(dirname(resolve(cms, db)), { recursive: true });
  mkdirSync(dirname(log), { recursive: true });

  const env = { ...placeholders, ...process.env, PORT: port, DATABASE_FILENAME: db, PUBLIC_URL: cmsUrl };
  // Strapi's own entry script, run by this Node directly (what `npm run develop` runs) and attached to this
  // process, so it shares this terminal and ends with it. Started through npm or a shell and detached instead,
  // it gets a console window of its own on Windows and writes there rather than to the log.
  const strapi = join(cms, 'node_modules', '@strapi', 'strapi', 'bin', 'strapi.js');
  if (!existsSync(strapi)) throw new Error(`no Strapi at ${strapi}; run npm ci in ${cms} first`);
  const child = spawn(process.execPath, [strapi, 'develop'], { cwd: cms, env, stdio: ['ignore', 'pipe', 'pipe'] });
  const logStream = createWriteStream(log, { flags: 'a' });
  for (const stream of [child.stdout, child.stderr]) {
    stream.pipe(logStream, { end: false });
    stream.pipe(process.stderr, { end: false });
  }
  console.error(`Strapi starting from ${cms} on ${port} (pid ${child.pid}), log ${log}`);

  let exited = false;
  child.on('exit', (code, signal) => {
    exited = true;
    if (existsSync(stateFile)) unlinkSync(stateFile);
    console.error(`Strapi exited (${signal ?? code}).`);
    process.exit(code ?? 1);
  });
  const stopChild = () => { if (!exited) stop(child.pid); };
  process.on('SIGINT', stopChild);
  process.on('SIGTERM', stopChild);

  const after = await waitFor(`${cmsUrl}/admin`, 180, () => exited);
  if (after < 0) { stopChild(); throw new Error(`Strapi did not answer on ${cmsUrl}/admin within 180 s; see ${log}`); }
  console.error(`Strapi answered after ${after} s.`);

  const token = await mintToken().catch((error) => { stopChild(); throw error; });
  writeFileSync(stateFile, JSON.stringify({ pid: child.pid, cms, db, port, token }, null, 2));
  process.stdout.write(`TOKEN=${token}\n`);
}

function seed() {
  const fips = option('fips');
  if (!fips) throw new Error('pass --fips <url of an application reading this Strapi>');
  const state = JSON.parse(readFileSync(stateFile, 'utf8'));
  const env = { ...process.env, CMS_BASE_URL: cmsUrl, CMS_FULL_API_KEY: state.token, CMS_WRITE_KEY: state.token, CMS_KEY: state.token, FIPS_BASE_URL: fips };
  const result = spawnSync(process.execPath, [join(here, 'seed-cms.mjs')], { stdio: 'inherit', env });
  process.exit(result.status ?? 1);
}

// Stops the Strapi with the given pid, or the one the state file records. Strapi's develop command forks a worker,
// so on Windows the whole tree goes; elsewhere Strapi takes its worker down with it on SIGTERM.
function stop(pid) {
  if (pid === undefined) {
    if (!existsSync(stateFile)) { console.error('nothing recorded as started'); return; }
    pid = JSON.parse(readFileSync(stateFile, 'utf8')).pid;
  }
  if (process.platform === 'win32') spawnSync('taskkill', ['/PID', String(pid), '/T', '/F'], { stdio: 'ignore' });
  else { try { process.kill(pid, 'SIGTERM'); } catch { /* already gone */ } }
  if (existsSync(stateFile)) unlinkSync(stateFile);
  console.error(`Stopped Strapi (pid ${pid}).`);
}

try {
  if (command === 'start') await start();
  else if (command === 'seed') seed();
  else if (command === 'stop') stop();
  else { console.error('usage: node cms.mjs start|seed|stop [options]'); process.exit(2); }
} catch (e) {
  console.error(e.message);
  process.exit(1);
}
