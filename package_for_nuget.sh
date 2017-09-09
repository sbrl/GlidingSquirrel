#!/usr/bin/env bash

################
### Settings ###
################
solution_file="GlidingSquirrel.sln";

package_version="$(cat "${solution_file}" | grep -i 'version = ' | sed -e 's/\s*//g' | cut -d'=' -f2)";
nupkg_name="GlidingSquirrel-${package_version}";

################

echo -e "Checklist: (press enter to check off an item)";
echo -ne "    1. Bump version (in code, solution, and nuspec files)"; read; echo -e "done";
echo -ne "    2. Update release notes in nuspec file"; read; echo -e "done";


echo;
echo "Preparing to release version ${package_version} of ${solution_file}";

if [ -f "${nupkg_name}" ]; then
	echo "Error: A file with the name ${nupkg_name} already exists! Exiting.";
	exit 1;
fi

zip 
