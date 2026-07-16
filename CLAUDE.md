# Disaster Drive — Claude Code Instructions

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
