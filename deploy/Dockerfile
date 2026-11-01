# Baut API oder Website aus dem Quellcode (z. B. auf dem Entwicklungsrechner oder in CI).
#   docker build -f deploy/Dockerfile --build-arg PROJECT=StoryPlatform.Api -t story-api .
#   docker build -f deploy/Dockerfile --build-arg PROJECT=StoryPlatform.Web -t story-web .
ARG PROJECT=StoryPlatform.Api

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /src
COPY Directory.Build.props StoryPlatform.slnx ./
COPY src/ src/
RUN dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROJECT
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_gcServer=0 APP_DLL=${PROJECT}.dll
WORKDIR /app
COPY --from=build /out .
# /data gehört dem App-Benutzer, damit das Docker-Volume beschreibbar ist.
RUN mkdir -p /data && chown "$APP_UID" /data
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]
