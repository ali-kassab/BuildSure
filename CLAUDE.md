# Instructions for Claude Code sessions in this repository

1. Before doing any work, read `docs/handoff/PROJECT_HANDOFF.md` and `docs/handoff/SESSION_HANDOFF.md`.
2. At the end of every significant work session, update:
   - `docs/handoff/SESSION_HANDOFF.md`: what was done, files changed, commands run, build/test results,
     branch and commit, unresolved issues, exact next steps.
   - `docs/handoff/PROJECT_HANDOFF.md`: when features, structure, decisions or limitations change.
   - `docs/handoff/CHANGELOG.md`: meaningful changes, in chronological order.
   - `docs/architecture.md` and `project-overview.md`: when the architecture or overview changes.
3. Never record something as completed or tested unless it was actually verified.
4. Keep the agreed scope (see PROJECT_HANDOFF.md §2). Do not add features the owner hasn't asked for.
5. Build and test: `dotnet build BuildSure.sln` and `dotnet test`. Stop the running web app first, because it locks
   `BuildSure.Web.exe`. Web Forms builds only with Visual Studio's MSBuild.
