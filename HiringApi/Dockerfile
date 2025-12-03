FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY DevOpsExcellenceDemo.slnx ./
COPY HiringApi/*.csproj ./HiringApi/
COPY HiringApi.Tests/*.csproj ./HiringApi.Tests/

RUN dotnet restore HiringApi/HiringApi.csproj

COPY HiringApi/ ./HiringApi/

RUN dotnet publish HiringApi/HiringApi.csproj -c Release -o /app --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN chown -R nobody:nobody /app
USER nobody

EXPOSE 8080

ENTRYPOINT ["dotnet", "HiringApi.dll"]
