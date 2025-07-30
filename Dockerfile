# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# ✅ Copy file .csproj từ thư mục con
COPY MovieTicketWebApi/MovieTicketWebApi.csproj ./

# ✅ Restore trong đúng thư mục
WORKDIR /src/MovieTicketWebApi
RUN dotnet restore

# ✅ Copy toàn bộ source code
WORKDIR /src
COPY . .

# ✅ Build xuất bản vào thư mục out
WORKDIR /src/MovieTicketWebApi
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Railway yêu cầu expose port 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# ✅ Chạy đúng tên .dll (trùng với .csproj)
ENTRYPOINT ["dotnet", "MovieTicketWebApi.dll"]