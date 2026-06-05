FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY MezonQuiz.slnx ./
COPY src/WebApp/WebApp.csproj src/WebApp/
COPY src/Mezon-sdk/Mezon-sdk.csproj src/Mezon-sdk/

RUN dotnet restore src/WebApp/WebApp.csproj

COPY src/ ./src/

RUN dotnet publish src/WebApp/WebApp.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Production config must come from Render environment variables, not files baked into the image.
RUN rm -f /app/publish/appsettings.json /app/publish/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "WebApp.dll"]
