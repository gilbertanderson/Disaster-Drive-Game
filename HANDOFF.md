# Handoff: Disaster-Drive-Game CI/CD → Unity Play deployment

Continuing work from a remote Claude Code session. Repo:
`gilbertanderson/Disaster-Drive-Game`. **Read `CLAUDE.md` (repo root) first** —
all session learnings live there.

## Goal

Ship the game to the browser via **Unity Play**, built by **Unity Build
Automation** (Unity Cloud dashboard). GitHub Actions is for **tests + CI
artifacts only** — it is NOT the deployment path. (Two earlier PRs that
deployed via Actions/Pages, #37 and #43, were closed as misaligned.)

## Current state (2026-07-16)

- `main` and `dev` both exist. `main` is currently **ahead of `dev`** after a
  squash merge — run `/merge-sync` (repo skill in `.claude/skills/`) to
  re-level them.
- Edit Mode tests are green (98/98).
- `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` secrets are set and working.
- **WebGL free-disk-space fix just merged (PR #57).** The WebGL build on `main`
  was failing at `no space left on device` (exit 125) during Docker image
  extraction; the fix frees ~25 GB before the build. Confirm the latest
  `build-webgl.yml` run on `main` now gets past that step.

## Blocked task — set 3 GitHub secrets (needs local `gh`, which the remote session lacked)

```bash
gh secret set CLOUD_BUILD_ORG_ID     -R gilbertanderson/Disaster-Drive-Game   # your org slug
gh secret set CLOUD_BUILD_PROJECT_ID -R gilbertanderson/Disaster-Drive-Game   # from ProjectSettings
gh secret set CLOUD_BUILD_API_KEY    -R gilbertanderson/Disaster-Drive-Game   # paste at hidden prompt
```

- `CLOUD_BUILD_PROJECT_ID` = the `cloudProjectId` field in
  `ProjectSettings/ProjectSettings.asset` (grep for `cloudProjectId:`).
- `CLOUD_BUILD_ORG_ID` = your org slug — read it from the
  `organizations/<id>` segment of your cloud.unity.com URL. Some orgs use a
  numeric/GUID id instead of a slug; use whichever the URL shows.
- `CLOUD_BUILD_API_KEY` = **org-level API key** from
  [cloud.unity.com](https://cloud.unity.com) → **Organization settings →
  API Keys** (may be under "Service Accounts" or "DevOps → Build Automation →
  Settings" depending on dashboard version). Copy the raw value — the workflow
  sends it as `Authorization: Basic <key>`, so **do not** base64-encode it or
  add a username.

Then trigger and check the log:

```bash
gh workflow run cloud-build-trigger.yml --ref main
gh run watch   # or: gh run list --workflow cloud-build-trigger.yml
```

The workflow now **fails loudly** on a bad org id / key (earlier it silently
skipped when secrets were empty), so a wrong value gives a definitive error to
act on.

> ⚠️ The workflow hits the **legacy** `build-api.cloud.unity3d.com` endpoint.
> It still works for many orgs but Unity is migrating Build Automation to a
> newer API. If it returns 401/404 with correct values, prefer the dashboard's
> own **Auto-build on push** (webhook-based, no secrets) instead.
> If your org uses a **Service Account** (key id + secret) rather than a single
> API key, the workflow's auth header needs adjusting to base64 `key_id:secret`.

## Dashboard checks (only doable by you, in cloud.unity.com)

This is the actual deployment path and the likely reason an earlier `dev` push
did not build:

1. **DevOps → Build Automation** → repo connected via the **GitHub app**
   (verify: repo **Settings → Webhooks** shows a `unity3d.com` / cloudbuild
   entry — no webhook = auto-build never fires).
2. A **WebGL** build target exists on the intended branch (`main`, and/or
   `dev` for previews).
3. **Auto-build** is toggled **on**.
4. Unity version: **6000.4.7f1** (or "Latest 6000.4" to match
   `ProjectVersion.txt`).

## Sharing / publishing the build

- **Share link:** on a successful WebGL build in the Build Automation
  dashboard → **Share** → hosted, playable-in-browser link (can auto-point at
  the latest build).
- **Unity Play page:** in the Editor, **Publish → WebGL Project** (WebGL
  Publisher package `com.unity.connect.share` is already in the manifest) →
  uploads to [play.unity.com](https://play.unity.com), permanent link, 500 MB
  zipped cap.

## Handy repo skills (in `.claude/skills/`)

- `/merge-sync` — merge `main`→`dev` then `dev`→`main` so both match. Guardrails:
  clean tree required, aborts on conflicts (never auto-resolves Unity YAML),
  never force-pushes.
- `/retrospective` — fold durable session learnings into `CLAUDE.md` (secret
  *names* only, never values).

## Environment notes

- CI build pins `unityVersion: 6000.4.8f1` (project is 6000.4.7f1) — .7f1
  shadergraph has a CS0246 GUID bug on fresh imports, fixed in .8f1.
- `webGLDecompressionFallback` is intentionally 0 (Unity Play/Build Automation
  hosting handles Brotli; only needs to be 1 for static hosts like GitHub Pages
  that don't send `Content-Encoding`).
