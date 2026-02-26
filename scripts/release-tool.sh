#!/usr/bin/env bash

set -euo pipefail

TOOL="${1:-}"

case "$TOOL" in
  dotnet-open)
    CSPROJ="src/DotNetOpen/DotNetOpen.csproj"
    ;;
  dotnet-overview)
    CSPROJ="src/DotNetOverview/DotNetOverview.csproj"
    ;;
  *)
    echo "Usage: $0 <dotnet-open|dotnet-overview>"
    exit 1
    ;;
esac

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI (gh) is required."
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "Please authenticate first: gh auth login"
  exit 1
fi

REPO="oskar/dotnet-tools"

if [[ "$(git branch --show-current)" != "main" ]]; then
  echo "Current branch is not main. Switch to main before releasing."
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Working tree is not clean. Commit or stash changes first."
  exit 1
fi

git pull --ff-only

VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$CSPROJ" | head -n1)

if [[ -z "$VERSION" ]]; then
  echo "Failed to parse version from $CSPROJ"
  exit 1
fi

TAG="${TOOL}-v${VERSION}"
PREV_TAG=$(git tag --list "${TOOL}-v*" --sort=-v:refname | grep -v "^${TAG}$" | head -n1 || true)

if [[ -z "$PREV_TAG" ]]; then
  echo "Could not find a previous release tag for ${TOOL}."
  exit 1
fi

echo "$PREV_TAG -> $TAG"

if ! git rev-parse -q --verify "refs/tags/${TAG}" >/dev/null; then
  git tag -a "$TAG" -m "$TOOL v${VERSION}"
else
  echo "Tag already exists locally: $TAG"
fi

git push origin "$TAG"

if gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
  echo "Release already exists: $TAG"
else
  gh release create "$TAG" \
    --repo "$REPO" \
    --title "$TOOL v${VERSION}" \
    --draft \
    --generate-notes \
    --notes-start-tag "$PREV_TAG"
fi

echo "Draft release ready for manual edits:"
echo "  https://github.com/${REPO}/releases/tag/${TAG}"

echo "NuGet publish not triggered (expected for review-first flow)."
echo "When ready to publish all tools from main: gh workflow run publish.yml --repo ${REPO} --ref main"
echo "If you only want to publish ${TOOL}, run the publish workflow with appropriate inputs or update publish.yml to support per-tool publishing."
