#!/bin/bash
# region constants

r="\033[1;31m"
g="\033[1;32m"
n="\033[0;39m"

function faulty
{
    echo -e "$r!> $1$n"
}

function region
{
    echo -e "$g=> $1$n"
}

# endregion
# region help

if [[ $1 == *h* || $# == 0 ]]
then
    echo "Builds the project"
    echo ""
    echo "Usage: build <operations> [deploy-directory]"
    echo "Operations:"
    echo "    -h display this text"
    echo "    -r use release configuration"
    echo "    -d deploy to a directory"
    echo "    -z deploy as an archive (requires -d)"
    echo "    -i ignore the path file"
    exit 0
fi

# endregion
# region build

if [[ $1 == *r* ]]
then
    region "Building release version..."
    dotnet build Jaket.csproj --configuration Release --no-incremental
else
    region "Building debug version..."
    dotnet build Jaket.csproj
fi

# endregion
# region deploy

if [[ $1 == *d* ]]
then
    region "Deploying the built version..."

    if [[ $# < 2 ]]
    then
        faulty "Target directory was not provided"
        exit 1
    fi

    location=$(echo $1 | grep -q "r" && echo Release || echo Debug)

    if [[ $1 == *z* ]]
    then
        zip -qj $2/Jaket.zip bin/$location/netstandard2.1/Jaket.dll obj/$location/netstandard2.1/manifest.json README.md assets/assets.bundle assets/bundles/*.properties
    else
        cp bin/$location/netstandard2.1/Jaket.dll $2
        cp assets/assets.bundle $2
        cp assets/bundles/*.properties $2
    fi
fi

# endregion
# region ignore

if [[ $1 == *i* ]]
then
    region "Ignoring the Path.props file..."

    if ! git rev-parse --is-inside-work-tree &> /dev/null
    then
        faulty "Couldn't find a repository in the work directory"
        exit 1
    fi

    git update-index --skip-worktree Path.props
fi

# endregion
