# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy project files first and restore, so this layer is cached as long as
# the project files don't change (independent of source edits).
COPY src/TripSplit.Shared/TripSplit.Shared.csproj src/TripSplit.Shared/
COPY src/TripSplit.Server/TripSplit.Server.csproj src/TripSplit.Server/
COPY src/TripSplit.Client/TripSplit.Client.csproj src/TripSplit.Client/
RUN dotnet restore src/TripSplit.Server/TripSplit.Server.csproj

COPY src/TripSplit.Shared/ src/TripSplit.Shared/
COPY src/TripSplit.Server/ src/TripSplit.Server/
COPY src/TripSplit.Client/ src/TripSplit.Client/

# Publishing the Server project (which references the Client project) also
# builds the Blazor WebAssembly client and copies its static web assets
# (the _framework/*.wasm bundle, css, etc.) into the publish output's
# wwwroot - this is what UseBlazorFrameworkFiles()/UseStaticFiles() serve
# at runtime. This only happens reliably on `publish`, not `dotnet run`.
RUN dotnet publish src/TripSplit.Server/TripSplit.Server.csproj -c Release -o /app/publish --no-restore

# Publishing the Server alone propagates the Client's *build* output (not
# its publish output) via the project reference's static web assets, which
# leaves wwwroot/index.html with its `#[.{fingerprint}]` placeholder
# unresolved (Blazor never boots - confirmed by running the published
# output directly). Publishing the Client on its own applies that
# placeholder substitution correctly, so replace the Server's wwwroot with
# the Client's own publish output - the Server has no wwwroot content of
# its own, it all originates from the Client.
RUN dotnet publish src/TripSplit.Client/TripSplit.Client.csproj -c Release -o /app/client-publish --no-restore
RUN rm -rf /app/publish/wwwroot && cp -r /app/client-publish/wwwroot /app/publish/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# SQLite data directory, mounted as a volume in docker-compose so the
# database survives container recreation. World-writable because the base
# image may run as a non-root user whose exact uid varies by image/tag.
RUN mkdir -p /app/data && chmod 777 /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Default="Data Source=/app/data/tripsplit.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "TripSplit.Server.dll"]
