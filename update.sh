#!/bin/bash -e

help()
{
    echo "pull cs files, coffee.internal -> package: ./update.sh pull <package_dir>"
    echo "push cs files, package -> coffee.internal: ./update.sh push <package_dir>"
}

internal_dir=$(dirname "$0")/Packages/src
cmd=$1
pkg_dir=$2

[ "$cmd" != "pull" ] && [ "$cmd" != "push" ] && help && exit 0
[ ! -d "$pkg_dir" ] && echo 'Package directory is not found.' && help && exit 1
[ ! -f "$pkg_dir/package.json" ] && echo 'Package directory is not found.' && help && exit 1

[ "$cmd" = "pull" ] && src="$internal_dir" || src="$pkg_dir"
[ "$cmd" = "pull" ] && dst="$pkg_dir" || dst="$internal_dir"

sed_replace="$dst/.coffee.internal.sed"
touch "$sed_replace"

echo "==== coffee.internal update tool ===="
echo "cmd: $cmd"
echo "src: $src"
echo "dst: $dst"
echo "sed: $(cat "$sed_replace")"
echo "====================================="


for dir in $(cd "$internal_dir";find . -type d);
do
  [ -z "$(echo "$dir" | grep '/Internal')" ] && continue
  mkdir -p "$src/$dir"
  mkdir -p "$dst/$dir"
  
  echo "$dir"
  
  for f in $(cd "$src/$dir";find . -maxdepth 1 -type f -name "*.cs"); do
    sed -f "$sed_replace" "$src/$dir/$f" > "$dst/$dir/$f"
    echo "  -> $f"
  done

  # find "$dst/$dir/" -type f -name "*.dll" | sed -f "$sed_replace" | xargs -n2 mv
  for f in $(cd "$src/$dir";find . -maxdepth 1 -type f -name "*.dll"); do
    echo "$f" | sed -f "$sed_replace" | xargs -I{} cp "$src/$dir/$f" "$dst/$dir/{}"
    echo "  -> $f"
  done

done

echo '==== Complete ===='
