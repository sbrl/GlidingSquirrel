#!/usr/bin/env bash

project_name="GlidingSquirrel";

current_folder="$(pwd)";
temp_folder=$(mktemp --directory "/tmp/tmp-${project_name}-nuget_pack-XXXXXXXXX");

mkdir "${temp_folder}/lib";

cp "${project_name}/bin/Debug/${project_name}.dll" "${temp_folder}/lib";
cp .nuspec "${temp_folder}";
cp packages.config "${temp_folder}";

pushd "${temp_folder}";

nuget pack;

find -name "${project_name}*.nupkg" -exec cp '{}' "${current_folder}" ';';

popd;

rm -r "${temp_folder}";
