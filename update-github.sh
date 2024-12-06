#!/bin/bash -e

help()
{
    echo "pull .github files, coffee.internal -> repo: ./update-github.sh pull <repo_dir>"
    echo "push .github files, repo -> coffee.internal: ./update-github.sh push <repo_dir>"
}

internal_dir=$(dirname "$0")/.github
cmd=$1
external_dir=$2

[ "$cmd" != "pull" ] && [ "$cmd" != "push" ] && help && exit 0
[ ! -d "$external_dir" ] && echo 'External directory is not found.' && help && exit 1

[ "$cmd" = "pull" ] && src="$internal_dir" || src="$external_dir"
[ "$cmd" = "pull" ] && dst="$external_dir" || dst="$internal_dir"

echo "==== coffee.github update tool ===="
echo "cmd: $cmd"
echo "src: $src"
echo "dst: $dst"
echo "====================================="

rm -rf "$dst"
cp -rf "$src" "$dst"

echo '==== Complete ===='
