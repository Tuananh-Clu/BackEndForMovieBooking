# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MovieTicketWebApi/MovieTicketWebApi.csproj ./MovieTicketWebApi/
COPY MovieTicketWebApi.sln ./

# Restore
RUN dotnet restore MovieTicketWebApi.sln

# Copy the rest and build
COPY . .
WORKDIR /src/MovieTicketWebApi
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MovieTicketWebApi.dll"]