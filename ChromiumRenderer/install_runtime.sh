#!/bin/bash
set -e

os=$(uname -s)
arch=$(uname -m)

# Darwin-specific syntax
if [[ "$os" == "Darwin" &&  "$arch" == "arm64" ]]; then
    url="https://storage.googleapis.com/chrome-for-testing-public/133.0.6943.126/mac-arm64/chrome-headless-shell-mac-arm64.zip"
    download_path="/tmp/chrome-headless-shell-mac-arm64.zip"
    extract_dir="ChromiumRenderer/runtimes-cache/osx-arm64"
    rename_dir="ChromiumRenderer/runtimes-cache/osx-arm64/native"
    target_rename_dir="ChromiumRenderer/runtimes-cache/osx-arm64/chrome-headless-shell-mac-arm64"
elif [ "$os" = "Linux" ] && [ "$arch" = "x86_64" ]; then
    url="https://storage.googleapis.com/chrome-for-testing-public/133.0.6943.126/linux64/chrome-headless-shell-linux64.zip"
    download_path="/tmp/chrome-headless-shell-linux64.zip"
    extract_dir="ChromiumRenderer/runtimes-cache/linux-x64"
    rename_dir="ChromiumRenderer/runtimes-cache/linux-x64/native"
    target_rename_dir="ChromiumRenderer/runtimes-cache/linux-x64/chrome-headless-shell-linux64"
    is_linux=true
else
    echo "System not supported: $os $arch"
    exit 1
fi

echo "runtimes-cache" > ChromiumRenderer/.gitignore
echo "runtimes-cache/" > ChromiumRenderer/.dockerignore

# If the extraction directory exists and is non-empty, exit.
if [ -d "$rename_dir" ] && [ "$(ls -A "$rename_dir")" ]; then
    echo "Runtime cache exists in $rename_dir, nothing to do."
    exit 0
fi

echo "Downloading from $url..."
curl -L -o "$download_path" "$url"

echo "Extracting to $extract_dir..."
mkdir -p "$extract_dir"

if [ $is_linux ]; then
    if [ ! -f /tmp/busybox ]; then
        echo "Installing busybox"
        curl -L -o "/tmp/busybox" "https://busybox.net/downloads/binaries/1.35.0-x86_64-linux-musl/busybox"
        chmod +x /tmp/busybox
    fi
    /tmp/busybox unzip -o "$download_path" -d "$extract_dir"
else
    unzip -o "$download_path" -d "$extract_dir"
fi

echo "Renaming..."
mv "$target_rename_dir" "$rename_dir"

echo "Cleaning up..."
rm "$download_path"
