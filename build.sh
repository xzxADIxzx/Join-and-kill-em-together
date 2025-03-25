#!/bin/bash

b="\033[1;32m"
n="\033[0;39m"

project="Jaket"



[ "$1" != "release" ] && [ "$1" != "debug" ] && echo -e "Use either$b release$n or$b debug$n as an argument" && exit 1

if [ "$1" == "release" ]



then
    echo -e "=>$b Building release dll...$n"
    dotnet build $project.csproj --configuration Release
else
    echo -e "=>$b Building debug dll...$n"
    dotnet build $project.csproj
fi



[ "$2" != "deploy" ] && exit 0

echo -e "=>$b Deploying the builded project...$n"



mv bin/Debug/netstandard2.1/$project.dll $3
cp assets/bundles/*.properties $3
