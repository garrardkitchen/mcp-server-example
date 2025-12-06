FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

COPY ["mcp-server-example.csproj", "."]
RUN dotnet restore "mcp-server-example.csproj"

COPY . .
RUN dotnet build "mcp-server-example.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "mcp-server-example.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "mcp-server-example.dll"]
