FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app

COPY publish ./
EXPOSE 443
ENTRYPOINT ["dotnet", "Chaucer.Backend.dll"]
