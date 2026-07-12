#!/bin/bash
# Interactive helper to add GitHub secrets for CI/CD workflows.
# Run from repo root: ./.github/setup-secrets.sh
set -e

REPO="gilbertanderson/Disaster-Drive-Game"

command -v gh >/dev/null || { echo "GitHub CLI (gh) not found: https://cli.github.com"; exit 1; }
gh auth status >/dev/null || { echo "Not authenticated. Run: gh auth login"; exit 1; }

echo "Adding CI/CD secrets to $REPO"
echo "Press Enter to skip any secret."
echo ""

add_secret() {
  local name="$1" hint="$2" silent="$3"
  echo "→ $name  ($hint)"
  if [ "$silent" = "silent" ]; then
    read -s -p "  value: " val; echo ""
  else
    read -p "  value: " val
  fi
  if [ -n "$val" ]; then
    # stdin, not --body: keeps secret values out of the process arg list
    printf '%s' "$val" | gh secret set "$name" -R "$REPO"
    echo "  ✅ set"
  else
    echo "  ⏭  skipped"
  fi
  echo ""
}

add_secret UNITY_LICENSE "contents of Unity_lic.ulf — see .github/CICD_SETUP.md" silent
add_secret UNITY_EMAIL "Unity account email"
add_secret UNITY_PASSWORD "Unity account password" silent
add_secret CLOUD_BUILD_ORG_ID "from cloud.unity.com URL: organizations/<id>"
add_secret CLOUD_BUILD_PROJECT_ID "from cloud.unity.com URL: projects/<id>"
add_secret CLOUD_BUILD_API_KEY "cloud.unity.com → Settings → API keys" silent

echo "Done. Check: https://github.com/$REPO/settings/secrets/actions"
