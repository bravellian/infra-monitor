# Repository Guidelines

## Project Structure & Module Organization

This repo is a single ASP.NET Core Razor Pages app named `Bravellian.InfraMonitor` under `Bravellian.InfraMonitor/` with the solution at `infra-monitor.sln`.

- `Bravellian.InfraMonitor/Bravellian.InfraMonitor.csproj`: Main web project.
- `Bravellian.InfraMonitor/Program.cs`: App entry point and service/pipeline setup (Razor Pages + static assets).
- `Bravellian.InfraMonitor/appsettings*.json`: App configuration.
- `Bravellian.InfraMonitor/Pages/`: Razor Pages, including shared layout files under `Pages/Shared/`.
- `Bravellian.InfraMonitor/Models/`: Lightweight DTOs such as `PostmarkEmail`.
- `Bravellian.InfraMonitor/wwwroot/`: Static assets (Bootstrap/jQuery, site CSS/JS, favicon).
- `postmark.llms.txt`: LLM-friendly Postmark API reference for prompt grounding.
- `postmark-dotnet.wiki/`: Snapshot of the Postmark .NET client wiki documentation.

Keep related modules together and add a short README in any new top-level directory explaining its purpose.

## Build, Test, and Development Commands

Primary commands for this repo:

- `dotnet run --project Bravellian.InfraMonitor/Bravellian.InfraMonitor.csproj`: Run the Razor Pages app.
- `dotnet watch --project Bravellian.InfraMonitor/Bravellian.InfraMonitor.csproj`: Run with hot reload.
- `dotnet build infra-monitor.sln`: Build the solution.
- `dotnet test infra-monitor.sln`: Run tests (add when a `tests/` project exists).
- `dotnet format infra-monitor.sln`: Apply .NET formatting rules.

## Coding Style & Naming Conventions

This project follows standard C# and ASP.NET Core coding conventions.

- **Formatting**: Use the configuration defined in `.editorconfig`. Run `dotnet format` to apply styles.
- **Naming**: Use `PascalCase` for classes, methods, and properties. Use `camelCase` for local variables and method parameters.
- **Async**: Suffix asynchronous methods with `Async` (e.g., `GetDataAsync`).

## Testing Guidelines

There are currently no test projects in this repo. If tests are added:

- Place them under `tests/` and keep one project per tested area.
- Name files with a `Tests` suffix (e.g., `PostmarkEmailTests.cs`).
- Run with `dotnet test infra-monitor.sln`.

## Commit & Pull Request Guidelines

No repository-specific conventions are recorded yet. Use concise, imperative commit messages (e.g., "Add user authentication endpoint"). For pull requests, include:

- A short summary of the change
- Links to relevant issues or context
- Screenshots or logs when behavior changes

## Repo Skills (Codex)

Use the repo skills under `.codex/skills/` when they match the task:

- `dotnet-repo-discovery`: map the solution and projects.
- `dotnet-build-diagnostics`: capture build diagnostics and binlogs.
- `dotnet-format-analyzers`: check formatting/analyzers without reformatting.
- `dotnet-test-triage`: rerun only failed tests and generate a failure summary.
- `dotnet-symbol-grep-recipes`: quick `rg` recipes for common C# navigation.

## Agent-Specific Instructions

If you add automation or agents, document them in `AGENTS.md` and keep instructions short and actionable.

### Formatting (Required)

- Always run `dotnet format infra-monitor.sln` after making code changes.
- Fix any formatting or analyzer issues reported by `dotnet format` before finalizing changes.
