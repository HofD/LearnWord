#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LearnWord.Identity/LearnWord.Identity.csproj", "LearnWord.Identity/"]
COPY ["LearnWord.BL.Models/LearnWord.BL.Models.csproj", "LearnWord.BL.Models/"]
COPY ["LearnWord.Models/LearnWord.DAL.Models.csproj", "LearnWord.Models/"]
COPY ["LearnWord.Identity.DAL/LearnWord.Identity.DAL.csproj", "LearnWord.Identity.DAL/"]
COPY ["LearnWord.Identity.DAL.Models/LearnWord.Identity.DAL.Models.csproj", "LearnWord.Identity.DAL.Models/"]
COPY ["LearnWord.Identity.Migrations/LearnWord.Identity.Migrations.csproj", "LearnWord.Identity.Migrations/"]
RUN dotnet restore "./LearnWord.Identity/LearnWord.Identity.csproj"
COPY . .
WORKDIR "/src/LearnWord.Identity"
RUN dotnet build "./LearnWord.Identity.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LearnWord.Identity.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LearnWord.Identity.dll"]