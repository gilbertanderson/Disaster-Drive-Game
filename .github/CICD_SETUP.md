# CI/CD Setup Guide

Automated testing and build pipeline for Disaster Drive Game.

## Workflows

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `tests.yml` | Push/PR to `main`, `dev` | Runs Edit Mode tests via game-ci |
| `build-webgl.yml` | Push to `main`, tag `v*`, manual | Builds WebGL player; releases on tags |
| `cloud-build-trigger.yml` | Push to `dev`, manual | Triggers Unity Cloud Build (skips if secrets missing) |

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
