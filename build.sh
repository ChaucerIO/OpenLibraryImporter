#!/bin/bash

publish="publish"
solution="Chaucer/Chaucer.sln"

echo "Clearing publish directory: $publish"
rm -rf $publish
echo "Publish directory cleared"

echo "Restoring dependencies for $solution"
dotnet restore $solution
echo "Dependency restore complete"

echo "Building $solution"
echo "dotnet build $solution -c $1"
dotnet build $solution -c $1
echo "Build complete"

echo "Running tests for $solution"
echo "dotnet test $solution -c $1"
dotnet test $solution -c $1
echo "Tests complete"

backend="Chaucer/Chaucher.Backend/Chaucer.Backend.csproj"

echo "Publishing backend app $backend"
echo "dotnet publish $backend -c $1 -o $publish"
dotnet publish Chaucer/Chaucer.Backend/Chaucer.Backend.csproj -c "$1" -o $publish
echo "Publish complete"
