#!/usr/bin/env bash

set -euo pipefail
IFS=$'\n\t'

script_dir="$(dirname "$(realpath "$0")")"
package_version="${1:?Missing package version parameter.}"

function cleanup_on_exit() {
    git restore "$script_dir/../directory.build.props"
    popd
}

pushd "$script_dir"
trap cleanup_on_exit EXIT
cd ..

echo "Cleaning..."
dotnet clean
rm -f "toml-to-object/bin/Release/*.nupkg"
echo

echo "Setting package version to: $package_version"
dotnet run --file "$script_dir/set-versions.cs" -- -v="$package_version" "$script_dir/../directory.build.props"
echo

echo "Building toml-to-object..."
dotnet build toml-to-object --configuration Release
echo

echo "Running tests..."
dotnet run --project toml-to-object.tests/toml-to-object.tests.csproj --configuration=Release --framework=net10.0
echo

echo "Generating NuGet package..."
dotnet pack toml-to-object --configuration Release
echo

echo "Finished."


