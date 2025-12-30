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

if echo $1 | grep -q "h" || [ $# -lt 1 ]
then
    echo "Builds the project"
    echo ""
    echo "Usage: build <operations> [deploy-directory]"
    echo "Operations:"
    echo "    -h display this text"
    echo "    -r use release configuration"
    echo "    -d deploy to a directory"
    echo "    -i ignore the path file"
    exit 0
fi

# endregion
# region build

if echo $1 | grep -q "r"
then
    region "Building release version..."
    dotnet build Jaket.csproj --configuration Release
else
    region "Building debug version..."
    dotnet build Jaket.csproj
fi

# endregion
# region deploy

if echo $1 | grep -q "d"
then
    region "Deploying the built version..."

    if [ $# -lt 2 ]
    then
        faulty "Target directory was not provided"
        exit 1
    fi

    location=$(echo $1 | grep -q "r" && echo Release || echo Debug)

    cp bin/$location/netstandard2.1/Jaket.dll $2
    cp assets/assets.bundle $2
    cp assets/bundles/*.properties $2
fi

# endregion
# region ignore

if echo $1 | grep -q "i"
then
    region "Ignoring the Path.props file..."

    git rev-parse --is-inside-work-tree &> /dev/null

    if [ $? -ne 0 ]
    then
        faulty "Couldn't find a repository in the work directory"
        exit 1
    fi

    git update-index --skip-worktree Path.props
fi

# endregion
