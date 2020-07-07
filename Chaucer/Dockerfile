FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /src

# COPY ./Chaucer/nuget.config ./Chaucer

# Copy csproj and restore as distinct layers
# Copy the main source project files
COPY ./Chaucer.sln ./Chaucer.sln
COPY ./Chaucer.Backend/Chaucer.Backend.csproj ./Chaucer.Backend/Chaucer.Backend.csproj
COPY ./Chaucer.Common/Chaucer.Common.csproj ./Chaucer.Common/Chaucer.Common.csproj
COPY ./Chaucer.Common.Tests/Chaucer.Common.Tests.csproj ./Chaucer.Common.Tests/Chaucer.Common.Tests.csproj
COPY ./Chaucer.OpenLibraryService/Chaucer.OpenLibraryService.csproj ./Chaucer.OpenLibraryService/Chaucer.OpenLibraryService.csproj
RUN dotnet restore Chaucer.sln

# Copy everything else, build, test, publish
COPY . .
RUN dotnet build Chaucer.sln -c Release

    FROM build-env as test
    WORKDIR /src
    RUN dotnet test Chaucer.sln -c Release

    FROM build-env as publish
    WORKDIR /src
    RUN dotnet publish Chaucer.sln -c Release -o /publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime-env
WORKDIR /app

COPY --from=publish /publish .
# EXPOSE 443
EXPOSE 80
ENTRYPOINT ["dotnet", "Chaucer.Backend.dll"]
