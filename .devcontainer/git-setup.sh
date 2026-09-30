#!/usr/bin/env bash
# Runs on every attach: keep git on the repo-local config and drop credential helpers injected by VS Code.
set -euo pipefail
mkdir -p "$(dirname "$GIT_CONFIG_GLOBAL")" "$GH_CONFIG_DIR"
touch "$GIT_CONFIG_GLOBAL"
git config --global --unset-all credential.helper || true
git config --global credential.https://github.com.helper '!gh auth git-credential'
git config --global --get user.email > /dev/null \
    || echo 'git: set your identity with git config --global user.name/user.email; sign in with gh auth login'
