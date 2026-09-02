# CI/CD Pipeline

The project uses GitHub Actions for continuous integration and automated releases. There are 5 workflows in `.github/workflows/`.

## Workflows Overview

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI | `ci.yml` | Push/PR to main/master | Build + test |
| Tests | `test.yml` | Push to `release/*`, PRs to master | Build + test + upload results |
| Auto Version Tag | `auto-tag.yml` | PR merged to master | Semantic versioning from PR title |
| Build and Release | `release.yml` | After auto-tag succeeds | Build, package, GitHub Release |
| PR Labeler | `pr-labeler.yml` | PR opened/edited | Auto-label based on title prefix |

## Workflow Details

### 1. CI (`ci.yml`)

**Triggers:** Push to `main`/`master`, PRs to `main`/`master`

```
Checkout → Setup MSBuild + NuGet + .NET 8 → Restore → Build (Release)
  → Restore test project → Run tests
```

### 2. Tests (`test.yml`)

**Triggers:** Push to `release/*` branches, PRs to `master`

```
Checkout → Setup MSBuild + NuGet + .NET 8 → Restore → Build (Debug)
  → Restore test project → Run tests (with TRX logger)
  → Upload test results artifact (30-day retention)
```

### 3. Auto Version Tag (`auto-tag.yml`)

**Triggers:** PR merged to `master` (excluding `chore:` commits)

```
Checkout → Fetch tags → Determine version bump from PR title
  → Update VERSION file → Commit → Create + push git tag
```

**Semantic Versioning Logic:**

| PR Title Prefix | Bump | Example |
|----------------|------|---------|
| `feat:*` | Minor | 1.0.0 → 1.1.0 |
| `version:*` or `breaking:*` | Major | 1.0.0 → 2.0.0 |
| Anything else | Patch | 1.0.0 → 1.0.1 |

**Excluded:** PRs with title starting with `chore:` are skipped (no version bump, no release).

### 4. Build and Release (`release.yml`)

**Triggers:** On successful completion of "Auto Version Tag from PR Title"

```
Checkout (full history) → Setup MSBuild + NuGet → Restore
  → Extract version from git tag → Build (Release)
  → Create package folder structure
  → Copy binaries + images + locales
  → Create ZIP with version
  → Generate changelog from PR titles
  → Create GitHub Release with ZIP + changelog
```

**Package Structure:**

```
launchbox-romm-plugin-{version}.zip
└── RomM LaunchBox Integration/
    ├── RommPlugin.dll
    ├── RommPlugin.dll.config
    ├── RommPlugin.Core.dll
    ├── RommPlugin.UI.dll
    ├── RommPlugin.CLI.exe
    ├── RommPlugin.CLI.exe.config
    ├── Newtonsoft.Json.dll
    ├── Images/
    │   ├── ico.ico
    │   ├── ico.png
    │   ├── Installed.png
    │   └── Installed Games.png
    └── Locales/
        ├── en.json
        └── pt-BR.json
```

**Changelog Categories:**

| Category | PR Title Pattern |
|----------|-----------------|
| New Version (Breaking) | `breaking:`, `version:` |
| Features | `feat:` |
| Fixes | `fix:`, `bug:` |
| Maintenance | `refactor:`, `chore:` |

### 5. PR Labeler (`pr-labeler.yml`)

**Triggers:** PR opened or edited

Automatically adds labels based on PR title prefix:

| Prefix | Label |
|--------|-------|
| `feat:` | `feat` |
| `fix:` | `fix` |
| `bug:` | `bug` |
| `refactor:` | `refactor` |
| `chore:` | `chore` |
| `version:` | `version` |
| `breaking:` | `version` |

## Release Flow Diagram

```
Developer creates PR with title "feat: add new feature"
  ↓
PR Labeler adds "feat" label
  ↓
CI workflow runs: build + test
  ↓
PR is merged to master
  ↓
Auto-tag workflow:
  - Detects "feat:" prefix
  - Bumps minor version (1.2.3 → 1.3.0)
  - Updates VERSION file
  - Creates git tag v1.3.0
  ↓
Release workflow:
  - Builds Release with versioned assemblies
  - Packages into ZIP
  - Creates GitHub Release with changelog
  ↓
Plugin auto-updater detects new release on next LaunchBox startup
```

## Requirements

- **Runner:** `windows-latest` (all workflows)
- **MSBuild:** Setup via `microsoft/setup-msbuild@v2`
- **NuGet:** Setup via `NuGet/setup-nuget@v2`
- **.NET SDK:** 8.0.x (for test runner)
- **Permissions:** `contents: write` (for tagging and releases)

## Troubleshooting

### Auto-tag didn't create a tag

- Check if PR title starts with a valid prefix (`feat:`, `fix:`, etc.)
- `chore:` commits are excluded from versioning
- PR must be merged (not just closed)

### Release didn't trigger

- The release workflow only triggers after a successful auto-tag run
- Check the Actions tab for the auto-tag workflow status

### Build fails on CI

- Ensure all NuGet packages are available
- Check for Windows-specific issues (all workflows run on `windows-latest`)
