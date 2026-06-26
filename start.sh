#!/usr/bin/env bash
cd "$(dirname "$0")/src/NetSecLab.App"
dotnet restore
dotnet run
