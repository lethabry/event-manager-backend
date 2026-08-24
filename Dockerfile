FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ARG PROJECT_PATH

COPY . .

RUN dotnet restore "$PROJECT_PATH"
RUN dotnet publish "$PROJECT_PATH" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet"]
