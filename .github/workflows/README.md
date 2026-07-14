# GitHub Actions Workflows

[![Tests](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/tests.yml/badge.svg?branch=dev)](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/tests.yml)
[![Build WebGL](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/build-webgl.yml/badge.svg?branch=main)](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/build-webgl.yml)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `tests.yml` | Push/PR on `main`, `dev` | Edit Mode tests |
| `build-webgl.yml` | Push to `main`, tags, manual | WebGL build + release |
| `cloud-build-trigger.yml` | Push to `main`, `dev`, manual | Trigger Unity Build Automation |

Deployment to players happens via **Unity Build Automation → Unity Play**, not
these workflows — see [CICD_SETUP.md](../CICD_SETUP.md#deploying-to-unity-play-primary-path).

Setup (secrets required before workflows pass): see [CICD_SETUP.md](../CICD_SETUP.md).

Manual triggers:

```bash
gh workflow run tests.yml --ref dev
gh workflow run build-webgl.yml --ref main
```
