# CLAUDE.md

Project guidance lives in [AGENTS.md](AGENTS.md); CI/deploy docs in
[.github/CICD_SETUP.md](.github/CICD_SETUP.md). This file holds
session-learned facts maintained by /retrospective.

## Session learnings

- (2026-07-14) Deployment target is Unity Play via Unity Build Automation (Unity Cloud dashboard), NOT GitHub Actions — GH Actions is for tests and CI artifacts only. Two earlier PRs (#37, #43) that deployed via Actions/Pages were closed as misaligned.
- (2026-07-14) The project is already linked to Unity Cloud: `cloudProjectId` in `ProjectSettings/ProjectSettings.asset`, org `gilbertandersonwork`. Build Automation targets are configured at cloud.unity.com (dashboard-only; no API access from sessions without `CLOUD_BUILD_*` secrets).
- (2026-07-14) The `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD` repo secrets are configured and working — CI license activation succeeds as of 2026-07-13.
- (2026-07-14) Four Edit Mode tests fail in headless CI but not locally (pre-existing on main, suspected resolution/physics environment sensitivity): `GameplayCameraDirectorTests.PlayIntroSequence_SyncsCameraShakeRestPosition`, `SpawnManagerTests.CacheSpawnX_PlacesSpawnLineOffScreenPastTopEdge`, `SpawnManagerTests.TryDetectPlayerOverlap_NoHitWhenPlayerDrivesCloseBeside`, `VehicleExitTests.ActiveRockCount_ResetsOnSubsystemRegistration`. Don't blame PRs that can't affect gameplay code.
- (2026-07-14) `build-webgl.yml` pins `unityVersion: 6000.4.8f1` (not the project's 6000.4.7f1) because .7f1 shadergraph has a CS0246 GUID bug on fresh imports.
- (2026-07-14) `webGLDecompressionFallback` is intentionally 0: Unity Play/Build Automation hosting handles Brotli. It only needs to be 1 for static hosts that don't send `Content-Encoding` headers (e.g. GitHub Pages).
- (2026-07-14) Remote Claude Code sessions here have no `gh` CLI — use the GitHub MCP tools (`mcp__github__*`); job logs via `get_job_logs`, runs via `actions_list`.
- (2026-07-14) WebGL Publisher package (`com.unity.connect.share`) is in the manifest: Editor menu **Publish → WebGL Project** uploads to play.unity.com (permanent link, 500 MB zipped cap).
