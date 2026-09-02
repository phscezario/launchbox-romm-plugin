# Release Process

This document explains how versioning, tagging, and GitHub Releases work end-to-end.

## Overview

The release process is fully automated using GitHub Actions. When a PR is merged to `master`, the following happens:

```
PR merged to master
  ↓
auto-tag.yml: Determine version bump from PR title
  ↓
Update VERSION file + create git tag
  ↓
release.yml: Build, package, create GitHub Release
  ↓
Plugin auto-updater detects new release
```

## Versioning

The project uses **Semantic Versioning** (MAJOR.MINOR.PATCH):

| Component | When Incremented |
|-----------|-----------------|
| **MAJOR** | Breaking changes (`version:` or `breaking:` prefix) |
| **MINOR** | New features (`feat:` prefix) |
| **PATCH** | Bug fixes, refactoring, maintenance (`fix:`, `refactor:`, `chore:`) |

### Version Source

The current version is stored in the `VERSION` file at the repository root:

```
1.0.3
```

This file is read at build time by the `SetGitVersion` MSBuild target in `Directory.Build.props` and injected into assembly attributes.

### Auto-Tag Logic

When a PR is merged to `master` (excluding `chore:` commits):

1. Fetch the latest git tag
2. Parse the PR title for type prefix
3. Determine version bump:
   - `feat:*` → minor bump (1.0.0 → 1.1.0)
   - `version:*` or `breaking:*` → major bump (1.0.0 → 2.0.0)
   - anything else → patch bump (1.0.0 → 1.0.1)
4. Update `VERSION` file
5. Commit with message `chore: update VERSION to X.Y.Z`
6. Create and push git tag `vX.Y.Z`

### Excluded Commits

PRs with title starting with `chore:` are excluded from versioning. They won't trigger a new release.

## Release Workflow

### Trigger

The release workflow (`release.yml`) triggers on successful completion of the auto-tag workflow.

### Steps

1. **Checkout** with full git history (needed for version extraction)
2. **Setup** MSBuild, NuGet, .NET SDK
3. **Extract version** from git tag
4. **Build** the solution in Release configuration
5. **Create package folder** structure
6. **Copy binaries** (DLLs, EXEs, configs)
7. **Copy assets** (Images, Locales)
8. **Create ZIP** with version in filename
9. **Generate changelog** from PR titles (categorized)
10. **Create GitHub Release** with ZIP and changelog

### Changelog Generation

The changelog is automatically generated from PR titles using `mikepenz/release-changelog-builder-action`:

| Category | PR Title Pattern |
|----------|-----------------|
| New Version (Breaking) | `breaking:`, `version:` |
| Features | `feat:` |
| Fixes | `fix:`, `bug:` |
| Maintenance | `refactor:`, `chore:` |

Each entry is formatted as: `- PR Title (#PR Number)`

## Creating a Release

### Normal Flow

1. Create a branch: `git checkout -b feat/my-feature`
2. Make changes and commit
3. Push and create PR with title `feat: my feature description`
4. Merge PR to `master`
5. Auto-tag creates version tag
6. Release workflow builds and publishes

### Breaking Changes

For breaking changes, use `version:` or `breaking:` prefix:

```
version: redesign sync API to use async streams
```

This triggers a **major** version bump.

### Hotfixes

For urgent fixes:

1. Create branch: `git checkout -b fix/critical-bug`
2. Fix the issue
3. Create PR with title `fix: resolve critical bug`
4. Merge to `master`
5. Patch version is bumped automatically

## Manual Release

If you need to create a release manually:

```bash
# Update VERSION file
echo "1.2.3" > VERSION
git add VERSION
git commit -m "chore: prepare release 1.2.3"
git tag v1.2.3
git push origin master --tags
```

Then trigger the release workflow by pushing a tag, or create a release manually on GitHub.

## Version History

Versions are tracked in:
- `VERSION` file (current version)
- Git tags (`v1.0.0`, `v1.0.1`, etc.)
- GitHub Releases (with changelogs)

## Troubleshooting

### Version wasn't bumped

- Check PR title format - must start with `feat:`, `fix:`, etc.
- `chore:` commits are excluded from versioning
- PR must be merged (not just closed)

### Release wasn't created

- Check if auto-tag workflow succeeded
- Release only triggers after successful auto-tag
- Check Actions tab for workflow status

### Wrong version was released

- The version is determined by the PR title at merge time
- If multiple PRs are merged quickly, the last one determines the version
- You can manually edit `VERSION` file and push a tag if needed
