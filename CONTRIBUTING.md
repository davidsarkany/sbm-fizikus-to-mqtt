# Contributing to SbmFizikusToMqtt

Thank you for your interest in contributing! This document explains how to report issues, propose changes, and submit code to the project.

## Table of contents
- Reporting bugs
- Feature requests
- Development setup
- Branching & pull requests
- Coding style
- Tests
- CI / Validation
- Security

## Reporting bugs
- Search existing issues before opening a new one.
- Provide a clear title, steps to reproduce, expected vs actual behavior, and relevant logs or configuration excerpts.

## Feature requests
- Describe the problem, the proposed solution, and the expected benefit.
- If possible, include examples or a short design sketch.

## Development setup
1. Fork the repository and create a feature branch from `main`.
2. Build the solution with the .NET SDK (recommended version: use what's in global.json or the CI). Example:

```
dotnet build
```

3. Run tests locally:

```
dotnet test
```

4. Tools and formatting
- Formatting uses `dotnet format`, which ships with the .NET SDK (no local tool restore is needed). Check formatting with:

```
dotnet format src/SbmFizikusToMqtt.slnx --verify-no-changes
```

If the check reports formatting issues, run `dotnet format src/SbmFizikusToMqtt.slnx` to apply fixes locally.

## Branching & pull requests
- Branch names: `feature/<short-description>`, `fix/<short-description>`, or `chore/<short-description>`.
- Keep commits focused and atomic. Use descriptive commit messages.
- Open a pull request against `main` with a clear description of the change and any testing performed.
- Include links to related issues when applicable.

## Coding style
- Follow existing project conventions and the .NET coding guidelines.
- Prefer clear, readable code over clever one-liners.
- Keep public API changes minimal and documented.

## Tests
- Add unit tests for new functionality and bug fixes.
- Ensure existing tests pass before submitting a PR.

## CI / Validation
- The repository uses automated CI (see the `.github/workflows` folder). PRs should pass all checks before merge.
- A `dotnet-format` check runs on pull requests to enforce repository formatting.

## Thank you
We appreciate all contributions — from bug reports to fully implemented features. If you'd like help getting started, open an issue and we'll assist.
