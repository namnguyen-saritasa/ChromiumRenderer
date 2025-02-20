#!/bin/bash
set -e

BUILD_VER="133.0.6943.126"

os=$(uname -s)
arch=$(uname -m)

# Darwin-specific syntax
if [[ "$os" == "Darwin" &&  "$arch" == "arm64" ]]; then
    url="https://storage.googleapis.com/chrome-for-testing-public/$BUILD_VER/mac-arm64/chrome-headless-shell-mac-arm64.zip"
    download_path="/tmp/chrome-headless-shell-mac-arm64.zip"
    extract_dir="runtimes-cache/osx-arm64"
    rename_dir="runtimes-cache/osx-arm64/native"
    target_rename_dir="runtimes-cache/osx-arm64/chrome-headless-shell-mac-arm64"
    is_linux=0
elif [ "$os" = "Linux" ] && [ "$arch" = "x86_64" ]; then
    url="https://storage.googleapis.com/chrome-for-testing-public/$BUILD_VER/linux64/chrome-headless-shell-linux64.zip"
    download_path="/tmp/chrome-headless-shell-linux64.zip"
    extract_dir="runtimes-cache/linux-x64"
    rename_dir="runtimes-cache/linux-x64/native"
    target_rename_dir="runtimes-cache/linux-x64/chrome-headless-shell-linux64"
    is_linux=1
else
    echo "System not supported: $os $arch"
    exit 1
fi

if [ -f /usr/bin/wget ]; then
    use_wget=1
elif [ -f /usr/bin/curl ]; then
    use_wget=0
else
    echo "No downloader found. Aborting..."
    exit 1
fi

download_with_curl() {
    local url="$1"
    local output="$2"
    curl -L -o "$output" "$url"
}

download_with_wget() {
    local url="$1"
    local output="$2"
    wget -O "$output" "$url"
}

download(){
    local linux="$1"
    local has_wget="$2"
    local url="$3"
    local output="$4"
  
    # Darwin specific
    if [[ "$linux" == 0 ]]; then
        download_with_curl "$url" "$output"
    elif [ "$linux" == 0 ]; then
        download_with_curl "$url" "$output"
    else
        if [ "$has_wget" == 1 ]; then
            download_with_wget "$url" "$output"
        else
            download_with_curl "$url" "$output" 
        fi
    fi
}

echo "runtimes-cache" > .gitignore
echo "runtimes-cache/" > .dockerignore

# If the extraction directory exists and is non-empty, exit.
if [ -d "$rename_dir" ] && [ "$(ls -A "$rename_dir")" ]; then
    echo "Runtime cache exists in $rename_dir, nothing to do."
    exit 0
fi

echo "Downloading from $url..."
download "$is_linux" "$use_wget" "$url" "$download_path"

echo "Extracting to $extract_dir..."
mkdir -p "$extract_dir"

if [ $is_linux == 1 ]; then
    if [ ! -f /tmp/busybox ]; then
        echo "Installing busybox"
        download "$is_linux" "$use_wget" "https://busybox.net/downloads/binaries/1.35.0-x86_64-linux-musl/busybox" "/tmp/busybox"
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
