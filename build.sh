#!/usr/bin/env bash

export PATH="$PATH:~/.dotnet/tools"

dotnet tool install -g Cake.Tool
dotnet tool install -g dotnet-warp
dotnet cake build.cake

