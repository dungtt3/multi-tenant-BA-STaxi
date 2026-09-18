---
stepsCompleted: ['step-01-preflight', 'step-02-generate-pipeline', 'step-03-configure-quality-gates', 'step-04-validate-and-summary']
lastStep: 'step-04-validate-and-summary'
lastSaved: '2026-09-18'
---

# CI/CD Pipeline Setup — multi-tenant-BA-STaxi (WEB2)

## Step 1 — Preflight

### 1. Git repository

- `.git/` present.
- Remote: `origin https://github.com/dungtt3/multi-tenant-BA-STaxi.git`
- Branch: `main` (single commit; all platform/test code still untracked).

### 2. Test stack type

**Detected: `backend`** (config was `auto`).

- Backend indicators found: `Staxi.slnx`, `src/Staxi.Platform/Staxi.Platform.csproj`, `tests/Staxi.TenantLeakTests/Staxi.TenantLeakTests.csproj`.
- No frontend indicators: no `package.json`, no `playwright.config.*`, no `cypress.config.*`, no `vite.config.*`, no `apps/`.
- No mobile indicators.

> Note: `docs/ARCHITECTURE-SPINE.md` plans `apps/admin-web` (Vite + React + TS). When that lands, the stack becomes `fullstack` and this pipeline needs a second job for the frontend.

### 3. Test framework

**Detected: xUnit on .NET 10.**

- `tests/Staxi.TenantLeakTests/Staxi.TenantLeakTests.csproj` references `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`.
- Central package management: `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`).
- Dependencies restore cleanly.

### 4. Tests pass locally

`dotnet test Staxi.slnx -m:1 -nr:false` → **Passed! Failed: 0, Passed: 45, Skipped: 0, Total: 45** (~9 s).

> Local quirk worth carrying into CI: the plain `dotnet test` invocation hit an MSBuild node-reuse problem on this machine. `-m:1 -nr:false` makes it deterministic and is harmless on a CI runner.

### 5. CI platform

**Detected: `github-actions`** (config was `auto`).

- No existing CI config: no `.github/workflows/`, `.gitlab-ci.yml`, `Jenkinsfile`, `azure-pipelines.yml`, `.harness/`, `.circleci/`.
- Inferred from git remote host `github.com`.

> Deployment reality check (`AD-21`): the product actually ships via **Jenkins onto 6 IIS machines**. This GitHub Actions pipeline is a *quality gate on pull requests*, not the deployment pipeline. If the team wants the gate to live where deployment lives, regenerate for `jenkins` instead.

### 6. Environment context

- .NET SDK on this machine: **10.0.102**.
- No `global.json` — nothing pins the SDK band. CI must request .NET 10 explicitly or a runner with an older SDK will fail on `net10.0`.
- Cache target: NuGet global packages, keyed on `Directory.Packages.props` + `*.csproj`.
- Target framework: `net10.0`; `TreatWarningsAsErrors=true` repo-wide, so any new warning already fails the build.

### 6b. TEA config flags

| Flag | Value | Effect here |
|---|---|---|
| `tea_use_playwright_utils` | `true` | **Not applicable.** No Playwright, no `package.json`; stack is .NET. No burn-in runner wired. |
| `tea_use_pactjs_utils` | `true` | **Skipped.** No `pact/`, no `tests/contract/`, no `.pacttest.ts`, no pact dependencies. Wiring a contract job now would fail every build on a missing script. |
| `ci_platform` | `auto` | Resolved to `github-actions` (see 5). |

### Preflight verdict

**PASS** — proceed to pipeline generation.

---

## Step 2 — Pipeline generated

**Execution mode:** `sequential`. `tea_execution_mode` was `auto`; no agent-team or subagent launch was
requested by the user, so the fallback applies.

**Output:** `.github/workflows/test.yml` (GitHub Actions, per detected `ci_platform`).

### Stages

| Stage | Present | Notes |
|---|---|---|
| lint | ✅ | `dotnet format --verify-no-changes` |
| test | ✅ | `tenant-leak-tests` job, Release config, TRX logger |
| contract-test | ⛔ skipped | `tea_use_pactjs_utils` is `true`, but the repo has no `pact/`, no `tests/contract/`, no `.pacttest.ts`, no pact dependencies. Wiring the jobs would fail every build on a missing script. |
| burn-in | ✅ | 5 iterations, PR + schedule + dispatch only |
| report | ✅ | Job summary table from `needs.*.result` |

### Deliberate deviations from the template

- **No sharding / no matrix.** 45 tests, ~9 s. Four shards would pay four runner cold starts and four
  NuGet restores to save seconds. Documented in the workflow; revisit threshold is a 5-minute suite.
- **No browser install, no Node steps.** Backend-only .NET stack.
- **`-m:1 -nr:false` on every build/test command.** MSBuild node-reuse fault on the dev machine; inert
  on a clean runner, and keeps the CI command identical to the local one.

### Caching

`actions/cache@v4` → `~/.nuget/packages`, key `${{ runner.os }}-nuget-${{ hashFiles('**/Directory.Packages.props', '**/*.csproj') }}`
with a `restore-keys` fallback. Applied in `lint`, `tenant-leak-tests`, `burn-in`.

### Fix applied before the gate could be trusted

`dotnet format --verify-no-changes` failed on `tests/Staxi.TenantLeakTests/Fakes/HaiHangGia.cs`
(using directives out of order). Fixed; the gate now exits 0 locally. A lint gate that is red from the
first commit gets disabled within a week, so this had to be green before it shipped.

---

## Step 3 — Quality gates

### Burn-in: enabled, against the default for a backend stack

The step guidance says backend stacks skip burn-in because backend tests are deterministic. Overridden
here on evidence from this repo: the suite **did** flake once, when xUnit ran test classes in parallel
over a shared static `AsyncLocal`. That is fixed
(`CollectionBehavior(DisableTestParallelization = true)`), but a tenant-leak suite that flakes
occasionally trains the team to ignore its red. Burn-in is what keeps the red credible.

Config: 5 iterations, fail-fast on the first red round, `if: failure()` artifact upload.

### Evidence integrity gate (`ci-burn-in.md` / `evidence-integrity.md` requirement)

Step `Assert test count ratchet` reconciles the executed count against a checked-in floor:

- `failed != 0` → fail
- `passed != total` → fail (a skipped or unrun test is a coverage hole, not a pass)
- `total < MINIMUM_TENANT_LEAK_TESTS` (workflow env, currently `45`) → fail

**Verified locally in both directions:** with the real TRX
(`total=45 executed=45 passed=45 failed=0`) the gate passes; with the floor raised to 99 it fails with
the expected message.

`continue-on-error` appears nowhere in the workflow. Artifact upload uses `if: always()` /
`if: failure()`, never `continue-on-error` on a test-running step.

### Quality thresholds

Every test in this suite is P0 — each one is an isolation proof — so the threshold is 100 % pass,
0 skipped. There is no P1 tier to grade at 95 %.

### Retry: deliberately absent

The checklist asks for retry on transient failures. Not configured, on purpose: auto-retry on a
tenant-isolation suite conceals exactly what burn-in exists to expose.

### Notifications

None wired. The repo has no Slack webhook secret; a step pointing at a non-existent secret reddens
every run. GitHub's built-in failure email to the actor covers the current team size. Adding Slack is
an opt-in follow-up: create the secret first, then add the step.

---

## Step 4 — Validation against `checklist.md`

### Passed

- Git repo + remote configured
- Test framework detected (xUnit / .NET 10), local run green (45/45)
- CI platform detected (`github-actions`), config at the platform-correct path
- YAML syntactically valid (parsed with `js-yaml`)
- Stack-conditional steps applied: no browser install, no Node setup
- Burn-in: exit-on-failure correct, triggers correct, failure artifacts uploaded
- Cache configured with hash-based key + restore-keys
- Artifact retention 30 days; no sensitive data in artifacts
- **Security:** no `${{ inputs.* }}` and no `${{ github.event.* }}` anywhere in a `run:` block. The
  workflow declares no inputs, so the injection surface does not exist. `needs.*.result` values are
  routed through `env:` and referenced as `"$VAR"`. `permissions: contents: read` at workflow level.
- No credentials in the configuration; no secrets required at all

### Not applicable (stack), with reason

- Sharding / 4-shard matrix / `fail-fast: false` — single 9-second suite, see Step 2
- Node version, `.nvmrc`, npm cache, browser cache — no Node in this repo
- Playwright burn-in utility (`runBurnIn`), `--only-changed` selection — not a Playwright stack
- Pact determinism gate, `can-i-deploy`, broker webhooks, staleness monitoring — no contract tests
- `scripts/test-changed.sh`, `scripts/ci-local.sh`, `scripts/burn-in.sh` — the local command is one
  line (`dotnet test Staxi.slnx -m:1 -nr:false`) and is documented in `docs/ci.md`. Scaffolding a
  `scripts/` folder at the root of a .NET repo would add a directory the source tree in
  `docs/ARCHITECTURE-SPINE.md` does not define.

### Open, owned by the user

- **First CI run.** All platform and test code is still untracked; nothing has been pushed. The
  pipeline cannot have run yet.
- **Performance targets** (lint < 2 min, total < 45 min) — unmeasurable until the first run. Expected
  well inside the targets: the whole suite is ~9 s plus restore.
- **Documentation** — `docs/ci.md` written (Vietnamese, matching the rest of `docs/`). The secrets
  checklist reduces to one line: none required.

### Verdict

**PASS**, with the three deliberate deviations above recorded rather than silently applied.
