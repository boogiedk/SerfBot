FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SerfBot/SerfBot.fsproj", "SerfBot/"]
RUN dotnet restore "SerfBot/SerfBot.fsproj"
COPY . .
WORKDIR "/src/SerfBot"
RUN dotnet build "SerfBot.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SerfBot.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ./home/root/appsettings.json /app/appsettings.json
ENTRYPOINT ["dotnet", "SerfBot.dll"]