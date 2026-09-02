# Contributing

Thank you for your interest in contributing to the LaunchBox RomM Plugin! This guide explains the conventions and process for submitting contributions.

## Getting Started

1. Fork the repository on GitHub
2. Clone your fork locally
3. Create a branch for your changes
4. Make your changes following the conventions below
5. Submit a pull request

## Branch Naming

Use descriptive branch names:

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feat/` | New feature | `feat/add-batch-sync` |
| `fix/` | Bug fix | `fix/download-resume-failure` |
| `refactor/` | Code refactoring | `refactor/sync-service-cleanup` |
| `docs/` | Documentation | `docs/update-api-guide` |
| `chore/` | Maintenance | `chore/update-dependencies` |

## Commit Conventions

This project uses **Conventional Commits** for automatic versioning and changelog generation.

### Format

```
<type>: <description>

[optional body]

[optional footer(s)]
```

### Types

| Type | Description | Version Bump |
|------|-------------|-------------|
| `feat:` | New feature | Minor (x.Y.0) |
| `fix:` | Bug fix | Patch (x.y.Z) |
| `bug:` | Bug fix (alias) | Patch (x.y.Z) |
| `refactor:` | Code refactoring (no feature/fix) | Patch (x.y.Z) |
| `chore:` | Maintenance, dependencies, CI | Patch (x.y.Z) |
| `docs:` | Documentation only | Patch (x.y.Z) |
| `test:` | Adding/fixing tests | Patch (x.y.Z) |
| `version:` | Breaking change | Major (X.0.0) |
| `breaking:` | Breaking change (alias) | Major (X.0.0) |

### Examples

```bash
git commit -m "feat: add batch download support"
git commit -m "fix: resolve sync resume state corruption"
git commit -m "refactor: extract metadata mapper to separate class"
git commit -m "docs: update configuration reference"
git commit -m "chore: update Newtonsoft.Json to 13.0.4"
```

### Important Notes

- **`chore:` commits are excluded from versioning** - They won't trigger a new release
- **PR title determines the version bump** - The title must follow the convention (e.g., `feat: ...`)
- **Breaking changes** use `version:` or `breaking:` prefix for major version bumps

## Pull Request Process

### 1. Create Your PR

- **Title**: Must follow conventional commit format (this drives auto-versioning)
- **Description**: Explain what changed and why
- **Labels**: Applied automatically based on title prefix

### 2. Automated Checks

The following workflows run automatically:

- **CI** (`ci.yml`) - Build + test on push/PR to main/master
- **PR Labeler** (`pr-labeler.yml`) - Auto-labels PRs based on title prefix

### 3. Code Review

- All PRs require review before merge
- Address review feedback promptly
- Keep PRs focused - one feature/fix per PR

### 4. Merge and Release

When merged to `master`:

1. **Auto-tag** (`auto-tag.yml`) creates a version tag based on PR title
2. **Release** (`release.yml`) builds, packages, and creates a GitHub Release
3. The plugin's auto-updater picks up the new release

## Code Style

### General

- Follow the `.editorconfig` rules (4-space indent for C#, 2-space for JSON/YAML)
- Use LF line endings
- UTF-8 encoding
- No trailing whitespace (except markdown)

### C# Conventions

- Allman brace style (new line before `{`)
- No `this.` qualifier
- Use `var` when the type is obvious
- Interface-based design for services
- Async/await for I/O operations
- Use `SafeFileWriter` for file writes (atomic writes)

### Naming

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `RommSyncService` |
| Interfaces | I + PascalCase | `IRommSyncService` |
| Methods | PascalCase | `SyncAsync()` |
| Properties | PascalCase | `RommBaseUrl` |
| Private fields | _camelCase | `_httpClient` |
| Constants | PascalCase | `MaxRetryAttempts` |
| Local variables | camelCase | `platformList` |

## Testing Requirements

- All new features should include unit tests
- Bug fixes should include a regression test
- Tests must pass before merge
- See [Testing](testing.md) for details

## Adding New Features

### New Service

1. Create interface in `RommPlugin.Core/Services/` or `RommPlugin/Services/`
2. Create implementation
3. Register in `ServiceLocator.Initialize()` (`RommPlugin.cs`)
4. Add unit tests in `RommPlugin.Tests/Services/`

### New Menu Item

1. Create class in `RommPlugin/MenuItems/Buttons/`
2. Inherit from `RommMenuItem`
3. Implement `ISystemMenuItemPlugin`
4. Add icon to `Images/` if needed

### New Settings Field

1. Add property to `RommPluginSettings` model (`RommPlugin.Core/Models/`)
2. Add to `settings.example.json`
3. Update `RommSettingsForm` UI
4. Update documentation in `docs/configuration.md`

### New Locale String

1. Add key to `RommPlugin.Core/Locales/en.json`
2. Add translation to `RommPlugin.Core/Locales/pt-BR.json`
3. Use `LocaleManager.GetString("key")` in code

## Reporting Bugs

When reporting bugs, please include:

1. **Steps to reproduce** the issue
2. **Expected behavior** vs actual behavior
3. **Environment** details (LaunchBox version, Windows version, plugin version)
4. **Log files** if available (from `Logs/` folder)
5. **Screenshots** if applicable

## License

By contributing, you agree that your contributions will be licensed under the GPL-3.0 License.
