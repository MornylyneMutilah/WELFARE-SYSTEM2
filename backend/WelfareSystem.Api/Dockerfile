FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY WelfareSystem.Api.csproj ./
RUN dotnet restore WelfareSystem.Api.csproj
COPY . .
RUN dotnet publish WelfareSystem.Api.csproj -c Release --no-restore -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Documents__StoragePath=/app/App_Data/Documents
COPY --from=build /publish ./
RUN mkdir -p /app/App_Data/Documents /app/App_Data/MailPending \
    && chown -R "$APP_UID:$APP_UID" /app/App_Data
USER $APP_UID
EXPOSE 8080
CMD ["dotnet", "WelfareSystem.Api.dll"]
