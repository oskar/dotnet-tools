#!/usr/bin/env bash

set -euo pipefail

TOOL="${1:-}"
VERSION="${2:-}"

usage() {
  echo "Usage: $0 <dotnet-open|dotnet-overview> <version>"
  echo "Example: $0 dotnet-open 1.5.0"
  exit 1
}

case "$TOOL" in
  dotnet-open|dotnet-overview) ;;
  *) usage ;;
esac

[[ -z "$VERSION" ]] && usage

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

echo ""
echo "NuGet publish triggered. Draft release ready for editing:"
echo "  https://github.com/${REPO}/releases/tag/${TAG}"
