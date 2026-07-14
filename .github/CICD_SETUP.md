# CI/CD Setup Guide

Automated testing and build pipeline for Disaster Drive Game.

## Workflows

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `tests.yml` | Push/PR to `main`, `dev` | Runs Edit Mode tests via game-ci |
| `build-webgl.yml` | Push to `main`, tag `v*`, manual | Builds WebGL player; releases on tags |
| `cloud-build-trigger.yml` | Push to `main`, `dev`, manual | Triggers Unity Build Automation (skips if secrets missing) |

## Deploying to Unity Play (primary path)

The game ships to the browser through **Unity Build Automation** (Unity's cloud
build service, formerly Cloud Build) and **Unity Play** — not through GitHub
Actions. The GitHub workflows above only run tests and produce CI artifacts.

The project is already linked to Unity Cloud (`cloudProjectId` in
`ProjectSettings/ProjectSettings.asset`, org `gilbertandersonwork`).

### One-time Build Automation setup (Unity Cloud dashboard)

1. Go to [cloud.unity.com](https://cloud.unity.com) → select the
   **My Game - Disaster** project → **DevOps → Build Automation**.
2. Connect the source: choose **GitHub** and authorize the
   `gilbertanderson/Disaster-Drive-Game` repository.
3. **New build target**:
   - Platform: **WebGL**
   - Branch: `main` (add a second target for `dev` if you want preview builds)
   - Unity version: **6000.4.7f1** (or "Latest 6000.4" to match `ProjectVersion.txt`)
   - Enable **Auto-build** so every push to the branch builds automatically.
4. Run the first build from the dashboard.

### Sharing / publishing the build

- **Share link (automatic):** on any successful WebGL build in the Build
  Automation dashboard choose **Share** — Unity hosts the build and gives you a
  link that plays in the browser. The link can be updated to always point at
  the latest build of the target.
- **Unity Play page (persistent showcase):** the project includes the
  **WebGL Publisher** package (`com.unity.connect.share`). In the Editor:
  **Publish → WebGL Project** → build (or select an existing WebGL build) →
  it uploads to [play.unity.com](https://play.unity.com) and returns a
  permanent link that stays the same across re-publishes. Max zipped build
  size: 500 MB.

### `cloud-build-trigger.yml` (optional API trigger)

Build Automation's own auto-build makes this workflow redundant once the repo
is connected in the dashboard. It exists as a fallback (e.g. if auto-build is
off) and needs the `CLOUD_BUILD_*` secrets below; without them it skips
gracefully.

## Required GitHub Secrets

Add at **Settings → Secrets and variables → Actions**, or run `./.github/setup-secrets.sh`.

| Secret | Required | Where to get it |
|--------|----------|-----------------|
| `UNITY_LICENSE` | Yes (tests + builds) | Contents of your `.ulf` license file — see below |
| `UNITY_EMAIL` | Yes (Unity 6 personal license activation) | Your Unity account email |
| `UNITY_PASSWORD` | Yes (with UNITY_EMAIL) | Your Unity account password |
| `CLOUD_BUILD_ORG_ID` | Optional | Unity Cloud dashboard URL (`organizations/<id>`) |
| `CLOUD_BUILD_PROJECT_ID` | Optional | Unity Cloud dashboard URL (`projects/<id>`) |
| `CLOUD_BUILD_API_KEY` | Optional | https://cloud.unity.com → Settings → API keys |

### Getting your Unity license (.ulf)

Follow game-ci's activation guide: https://game.ci/docs/github/activation

Short version (personal license):
1. Run the [Activate workflow](https://game.ci/docs/github/activation#personal-license) or activate locally.
2. On macOS the license file is at `/Library/Application Support/Unity/Unity_lic.ulf`.
3. Copy the entire file content into the `UNITY_LICENSE` secret.

## Local Headless Build

```bash
/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath . \
  -executeMethod DisasterBuildAutomation.BuildWebGL \
  -logFile -
```

Or in the Editor: **Disaster → Build → WebGL (Headless)**.

## Local Headless Tests

```bash
/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath . \
  -runTests -testPlatform editmode \
  -testResults TestResults/editmode-results.xml \
  -logFile -
```

## Monitoring

```bash
gh run list --workflow tests.yml
gh run view <run-id> --log-failed
```

## Troubleshooting

- **"No valid Unity license"** — `UNITY_LICENSE` missing/malformed; must be the raw `.ulf` file content. Unity 6 personal licenses also need `UNITY_EMAIL`/`UNITY_PASSWORD`.
- **First run is slow (~30+ min)** — the Library cache is cold; subsequent runs reuse it.
- **WebGL build fails in CI but works locally** — check the run log for IL2CPP/Brotli errors; the WebGL module is included in game-ci images.
- **Cloud Build step skipped** — expected when Cloud Build secrets aren't set; it exits 0 with a notice.

## Recommended Branch Protection (main)

Settings → Branches → Add rule for `main`:
- Require status checks: **Test Results**
- Require branches to be up to date before merging
