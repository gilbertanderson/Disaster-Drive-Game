# Disaster Drive — Claude Code Instructions

Project guidance lives in [AGENTS.md](AGENTS.md); CI/deploy docs in
[.github/CICD_SETUP.md](.github/CICD_SETUP.md). This file holds
standing rules plus session-learned facts maintained by /retrospective.

## No hardcoded IDs or secrets in docs or scripts

Never write a literal value for any of the following into documentation, scripts, or workflow files:

- Cloud project IDs / GUIDs (e.g. Unity `cloudProjectId`)
- Org IDs or org slugs
- Secret names' **values** (write the secret *name*, never the value)
- API keys, tokens, or credentials of any kind

Instead, point to the authoritative source:

| Value | Where to find it |
|---|---|
| `cloudProjectId` | `grep cloudProjectId ProjectSettings/ProjectSettings.asset` |
| Org slug / ID | `organizations/<id>` segment of your cloud.unity.com URL |
| Secret values | Enter interactively via `gh secret set … # paste at hidden prompt` |

**Exception:** unit-test fixtures that require a specific value to exercise a code path (document the exception inline with a comment explaining why).

## Session learnings

- (2026-07-14) Deployment target is Unity Play via Unity Build Automation (Unity Cloud dashboard), NOT GitHub Actions — GH Actions is for tests and CI artifacts only. Two earlier PRs (#37, #43) that deployed via Actions/Pages were closed as misaligned.
- (2026-07-14) The project is already linked to Unity Cloud: `cloudProjectId` in `ProjectSettings/ProjectSettings.asset`, org `gilbertandersonwork`. Build Automation targets are configured at cloud.unity.com (dashboard-only; no API access from sessions without `CLOUD_BUILD_*` secrets).
- (2026-07-14) The `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD` repo secrets are configured and working — CI license activation succeeds as of 2026-07-13.
- (2026-07-16) The `CLOUD_BUILD_ORG_ID`/`CLOUD_BUILD_PROJECT_ID`/`CLOUD_BUILD_API_KEY` secrets ARE set and working — a manual dispatch of `cloud-build-trigger.yml` successfully queued a Unity Build Automation build (target `default-webgl`). Every push to `main`/`dev` now triggers a cloud build. `CLOUD_BUILD_PROJECT_ID` = the `cloudProjectId` in `ProjectSettings/ProjectSettings.asset` (no literals in docs); org slug is `gilbertandersonwork`; API key is dashboard-only (cloud.unity.com → Settings → API keys). Gotcha: `gh secret set` via the in-session `!` prefix reads empty stdin and silently sets an EMPTY secret — interactive paste must happen in a real terminal or the GitHub UI.
- (2026-07-15) WebGL build on `main` can fail at `no space left on device` while extracting the Unity Docker image (runner disk exhaustion, exit 125) — not a code/license error. Fix: add a free-disk-space step (rm dotnet/android/ghc/CodeQL toolchains) before `game-ci/unity-builder`.
- (2026-07-14) Edit Mode suite is fully green in CI (98/98) as of 2026-07-14 — the four headless-only failures from 2026-07-13 (camera director, spawn manager ×2, rock counter) were fixed on main. Treat any new CI test failure as real, not "known flaky".
- (2026-07-14) `build-webgl.yml` pins `unityVersion: 6000.4.8f1` (not the project's 6000.4.7f1) because .7f1 shadergraph has a CS0246 GUID bug on fresh imports.
- (2026-07-14) `webGLDecompressionFallback` is intentionally 0: Unity Play/Build Automation hosting handles Brotli. It only needs to be 1 for static hosts that don't send `Content-Encoding` headers (e.g. GitHub Pages).
- (2026-07-14) Remote Claude Code sessions here have no `gh` CLI — use the GitHub MCP tools (`mcp__github__*`); job logs via `get_job_logs`, runs via `actions_list`.
- (2026-07-15) `actions_list` `list_workflow_runs` for busy workflows (tests.yml, build-webgl.yml, cloud-build-trigger.yml) returns output exceeding the token limit — it's auto-saved to a file. Parse with `jq -r '.workflow_runs[] | [.head_branch,.head_sha[0:8],.status,.conclusion] | @tsv'` instead of reading raw.
- (2026-07-14) WebGL Publisher package (`com.unity.connect.share`) is in the manifest: Editor menu **Publish → WebGL Project** uploads to play.unity.com (permanent link, 500 MB zipped cap).
- (2026-07-14) `main` has a branch-protection rule requiring PRs, but pushes from these sessions bypass it with only a warning — `/merge-sync` relies on that bypass when it pushes `main` directly. If bypass rights ever go away, fall back to a dev→main PR.
- (2026-07-14) Squash-merging a PR into `main` puts `main` ahead of `dev`; run `/merge-sync` afterwards to keep the branches level. Merge convention here: mark draft ready (`update_pull_request` draft:false), then `merge_pull_request` with squash, on the user's say-so.
- (2026-07-14) `main` and `dev` can fully diverge, not just drift main-ahead — the same source branch has been PR-merged into both separately (#51 into main, #52 into dev). `/merge-sync` handles this: expect a real merge commit on dev, then a dev→main fast-forward.
- (2026-07-14) In this environment `git rev-parse --short revA revB` with two revs fails with "Needed a single revision" — call it once per rev.
