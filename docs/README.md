# LaunchBox RomM Plugin - Documentation

Welcome to the developer documentation for the **LaunchBox RomM Plugin**. This folder contains everything you need to understand, develop, test, and contribute to the project.

## Table of Contents

| Document | Description |
|----------|-------------|
| [Architecture](architecture.md) | System architecture, project structure, design patterns, and dependency graph |
| [Development Setup](development-setup.md) | Prerequisites, cloning, building, and IDE configuration |
| [Contributing](contributing.md) | Contribution guidelines, commit conventions, code style, and PR process |
| [Testing](testing.md) | How to run and write unit tests, test project structure, mocking patterns |
| [CI/CD](ci-cd.md) | GitHub Actions workflows: build, test, release, auto-tagging, PR labeling |
| [Configuration](configuration.md) | Complete reference for all `settings.json` fields with types and defaults |
| [Deployment](deployment.md) | Building for release, package structure, manual installation, and auto-update |
| [Release Process](release-process.md) | How versioning, tagging, and GitHub Releases work end-to-end |
| [Localization](localization.md) | i18n system: locale JSON files, adding new languages, fallback chain |
| [API Integration](api-integration.md) | RomM REST API endpoints, authentication, data models, and error handling |

## Quick Start

New to the project? Start here:

1. **[Development Setup](development-setup.md)** - Get your environment ready
2. **[Architecture](architecture.md)** - Understand how the codebase is organized
3. **[Contributing](contributing.md)** - Learn the conventions before submitting a PR

## Project at a Glance

```
LaunchBoxRommPlugin.slnx
├── RommPlugin/           # Main plugin (loaded by LaunchBox)
├── RommPlugin.Core/      # Shared core: models, services, storage, API client
├── RommPlugin.UI/        # Windows Forms dialogs and helpers
├── RommPlugin.CLI/       # Command-line tool for Parents.xml hierarchy
├── RommPlugin.Tests/     # Unit tests (xUnit + Moq)
├── lib/LaunchBox/        # LaunchBox plugin SDK (vendored DLL)
├── Images/               # Plugin icons
└── .github/workflows/    # CI/CD pipelines
```

**Target Framework:** .NET Framework 4.8 (all projects)
