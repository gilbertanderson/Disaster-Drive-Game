# GitHub Actions Workflows

[![Tests](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/tests.yml/badge.svg?branch=dev)](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/tests.yml)
[![Build WebGL](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/build-webgl.yml/badge.svg?branch=main)](https://github.com/gilbertanderson/Disaster-Drive-Game/actions/workflows/build-webgl.yml)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `tests.yml` | Push/PR on `main`, `dev` | Edit Mode tests |
| `build-webgl.yml` | Push to `main`, tags, manual | WebGL build + release |
| `cloud-build-trigger.yml` | Push to `dev`, manual | Trigger Unity Cloud Build |

Setup (secrets required before workflows pass): see [CICD_SETUP.md](../CICD_SETUP.md).

Manual triggers:

```bash
gh workflow run tests.yml --ref dev
gh workflow run build-webgl.yml --ref main
```
