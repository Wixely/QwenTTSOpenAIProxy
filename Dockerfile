# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QwenTtsOpenAIProxy.csproj ./
RUN dotnet restore ./QwenTtsOpenAIProxy.csproj

COPY . ./
RUN dotnet publish ./QwenTtsOpenAIProxy.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8092
EXPOSE 8092

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "QwenTtsOpenAIProxy.dll"]
