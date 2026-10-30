# CI/CD pipelines

Two GitHub Actions workflows, one per application. Each one contains a `build-test` job
(CI) and a `publish-image` job (CD) that depends on it, so an image is published only for a
commit that built and passed its tests.

| Workflow | `build-test` (every push / PR / manual) | `publish-image` (push to `main`, tags `v*.*.*`) |
|---|---|---|
| [`backend.yml`](../.github/workflows/backend.yml) | `dotnet restore` → `build -c Release` → `dotnet test` with XPlat coverage → `dotnet publish` | Builds [`src/PropertyOpsHub.Api/Dockerfile`](../src/PropertyOpsHub.Api/Dockerfile), pushes to GHCR, smoke-tests port 8080 |
| [`frontend.yml`](../.github/workflows/frontend.yml) | `npm ci` → `npm run build` (production) → `ng test --watch=false` | Builds [`src/PropertyOpsHub.Web/Dockerfile`](../src/PropertyOpsHub.Web/Dockerfile) (nginx + bundle), pushes to GHCR, smoke-tests `index.html` |

Artifacts: `backend-test-results` (trx + cobertura), `api-publish`, `web-dist`.

Removed: `ci.yml` and `docker.yml`. `docker.yml` built `src/PropertyOpsHub.Api/Dockerfile`,
which did not exist; `ci.yml` duplicated both pipelines and ran an Angular production build
with no committed lockfile. The loose `backend-ci.yml` / `frontend-ci.yml` files that were on
disk before this change were rewritten as `backend.yml` / `frontend.yml` for the same reasons.

## Why one workflow per app instead of separate `*-ci` / `*-cd` files

A separate CD workflow triggered by `push` cannot see whether CI passed: a red `main` would
still publish `latest` and release tags. Gating it with `workflow_run` would work for `main`
but not for tags, and couples the two files by workflow *name*. Keeping CI and CD as two jobs
of one workflow gives an ordering guarantee (`needs: build-test`) with no name coupling, and
`if:` keeps CD off pull requests.

## Image tags

Both `publish-image` jobs derive tags with `docker/metadata-action`:

- `main` → `main`, `sha-<full>`, `latest`
- `v1.4.2` → `1.4.2`, `1.4`, `sha-<full>`
- manual dispatch from a branch → `<branch>`, `sha-<full>` (CD is skipped on manual dispatch
  from a feature branch because of the `if:` guard; trigger it from `main`).

Image names are lowercased from `GITHUB_REPOSITORY_OWNER`, because the repository name
(`PropertyOps-Hub`) contains an uppercase letter and GHCR rejects uppercase names.

## No `paths:` filters, on purpose

An earlier draft filtered `pull_request` runs by changed path. Combined with required status
checks that deadlocks PRs: a docs-only PR never runs the filtered check, and GitHub waits for
an "Expected" status that will never arrive. Both workflows therefore run on every PR to
`main` / `develop`; the suite takes well under three minutes.

## What had to change for the pipelines to go green

1. **`package-lock.json` was not committed.** `npm ci` refuses to run without a lockfile, so
   every frontend build failed at the install step. The lockfile is now tracked.
2. **Missing Dockerfiles.** `docker.yml` referenced `src/PropertyOpsHub.Api/Dockerfile`,
   which did not exist. Both Dockerfiles now exist, plus `.dockerignore` files and an
   `nginx.conf` with SPA fallback for the Angular router.
3. **Node version.** Angular 22.2 declares `engines.node = ^22.22.3 || ^24.15.0 || >=26`.
   CI pins Node 24; a bare `node-version: '22'` can resolve below 22.22.3 and the CLI refuses
   to build.
4. **`.gitattributes` with `eol=lf`.** Files were checked out with CRLF on Windows, which
   breaks Dockerfile `RUN` continuations and nginx config parsing inside Linux images.
5. **Dockerfile ignores `global.json`.** `global.json` pins SDK `10.0.403` with
   `rollForward: latestFeature`; copying it into the image makes `dotnet restore` fail
   whenever the base image ships a different feature band.
6. **Test logger.** A fixed `LogFileName` made all three test projects overwrite one file;
   plain `--logger trx` emits one file per assembly.

## Required repository settings

- **Actions permissions:** the workflows request `contents: read`, and only the
  `publish-image` job adds `packages: write`, so no org-level toggle is needed. If the
  organisation enforces "read-only workflow permissions", allow write for this repository.
- **No secrets are required.** GHCR authenticates with the built-in `GITHUB_TOKEN`.
- **Branch protection (optional):** require `Backend CI/CD / Build & test` and
  `Frontend CI/CD / Build & test` on `main`.

## Reproduce a CI run locally

```powershell
# Backend
dotnet restore PropertyOpsHub.sln
dotnet build PropertyOpsHub.sln --no-restore -c Release
dotnet test PropertyOpsHub.sln --no-build -c Release

# Frontend
cd src/PropertyOpsHub.Web
npm ci
npm run build
npx ng test --watch=false
```

On Windows, NuGet fails with `Value cannot be null. (Parameter 'path1')` if `APPDATA` is not
set in the shell; exporting `APPDATA` / `LOCALAPPDATA` (or setting `NUGET_PACKAGES`) fixes it.

## Verification status

| Stage | Result |
|---|---|
| Backend `dotnet restore` | Passed (SDK 10.0.403). Warnings: NU1603 and NU1903, see below. |
| Backend `dotnet build -c Release` | Passed, 0 errors / 4 warnings |
| Backend `dotnet test` + coverage | Passed, 3 assemblies, coverage `.cobertura.xml` per assembly |
| Backend `dotnet publish` | Passed, `artifacts/api/PropertyOpsHub.Api.dll` |
| Frontend `npm ci` | Passed, 286 packages |
| Frontend `npm run build` | Passed, `dist/PropertyOpsHub.Web/browser` (~217 kB initial) |
| Frontend `npx ng test --watch=false` | Passed, 1 file / 2 tests |
| Workflow YAML | Parsed with a YAML parser; shell blocks checked with `bash -n` |
| Docker image builds | **Not verified locally** — the Docker daemon is not running on the authoring machine. The `smoke-test` steps cover this on the first run. |

## Known gaps and warnings

- **NU1603:** `Directory.Packages.props` pins `Microsoft.AspNetCore.OpenApi`
  `10.0.0-preview.1.25081.1`, which is not on NuGet.org; restore resolves
  `10.0.0-preview.1.25120.3` instead. Harmless as a warning, but the pin should be corrected.
- **NU1903:** the transitive `Microsoft.OpenApi` 2.0.0-preview5 has a known high-severity
  advisory (GHSA-v5pm-xwqc-g5wc). Resolving it means moving off the preview ASP.NET Core
  OpenAPI package.
- **No linter gate:** `prettier --check` currently fails on 8 Angular files, so formatting is
  not enforced until the codebase is formatted once.
- **No e2e job:** `tests/e2e` is empty, so Playwright is not wired into CI yet.
- **CD stops at the registry.** Deployment to an environment (ACA, App Service, k3s) needs a
  target and credentials, and is intentionally out of scope.
- **`README.md` is stale** on workflow names. It could not be edited in this session because
  the file contains an example SQL Server SA password line and the workspace write guard
  rejects credential-shaped values. Replacing that example with an environment-variable
  reference makes the file editable again.
