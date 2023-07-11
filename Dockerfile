FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 7030

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
RUN mkdir "outp"
COPY ["./." , "outp/"]

RUN dotnet restore "outp/OneGuard/OneGuard.csproj"

RUN dotnet build "outp/OneGuard/OneGuard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "outp/OneGuard/OneGuard.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneGuard.dll"]