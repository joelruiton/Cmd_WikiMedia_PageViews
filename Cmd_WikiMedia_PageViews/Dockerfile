#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Cmd_WikiMedia_PageViews/Cmd_WikiMedia_PageViews.csproj", "Cmd_WikiMedia_PageViews/"]
COPY ["ClassLibrary_Business/ClassLibrary_Business.csproj", "ClassLibrary_Business/"]
COPY ["ClassLibrary_Entity/ClassLibrary_Entity.csproj", "ClassLibrary_Entity/"]
RUN dotnet restore "Cmd_WikiMedia_PageViews/Cmd_WikiMedia_PageViews.csproj"
COPY . .
WORKDIR "/src/Cmd_WikiMedia_PageViews"
RUN dotnet build "Cmd_WikiMedia_PageViews.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cmd_WikiMedia_PageViews.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cmd_WikiMedia_PageViews.dll"]