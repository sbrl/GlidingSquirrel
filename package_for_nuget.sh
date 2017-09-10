#!/usr/bin/env bash

################
### Settings ###
################
solution_file="GlidingSquirrel.sln";

dll_path="GlidingSquirrel/bin/Debug/GlidingSquirrel.dll";

package_version="$(cat "${solution_file}" | grep -i 'version = ' | sed -e 's/\s*//g' | cut -d'=' -f2)";
nupkg_name="GlidingSquirrel-${package_version}.nupkg";

################

echo -e "Checklist: (press enter to check off an item)";
echo -ne "    1. Bump version (in code, solution, and nuspec files)"; read >/dev/null 2>&1; echo -e "done";
echo -ne "    2. Update release notes in nuspec file"; read >/dev/null 2>&1; echo -e "done";


echo;
echo "Preparing to release version ${package_version} of ${solution_file}";

if [ -f "${nupkg_name}" ]; then
	echo "Error: A file with the name ${nupkg_name} already exists! Exiting.";
	exit 1;
fi

zip "${nupkg_name}" .nuspec "${dll_path}";
7za rn -tzip "${nupkg_name}" "${dll_path}" "lib/$(basename ${dll_path})";
