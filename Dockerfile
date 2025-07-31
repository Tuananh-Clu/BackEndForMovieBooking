# ------------ STAGE 1: BUILD ------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và file project
COPY MovieTicketWebApi.sln ./
COPY MovieTicketWebApi/MovieTicketWebApi.csproj ./MovieTicketWebApi/

# Restore dependencies
RUN dotnet restore MovieTicketWebApi.sln

# Copy toàn bộ source vào image
COPY . .

# Build và publish ứng dụng
WORKDIR /src/MovieTicketWebApi
RUN dotnet publish -c Release -o /app/publish


# ------------ STAGE 2: RUNTIME ------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ✅ Cài thêm libssl-dev & ca-certificates để fix lỗi kết nối MongoDB Atlas qua SSL
RUN apt-get update && \
    apt-get install -y libssl-dev ca-certificates && \
    update-ca-certificates && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy app đã build từ stage trước
COPY --from=build /app/publish .

# ✅ Lắng nghe cổng 5000 (Render hoặc local)
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# ✅ Bắt buộc: cấu hình cho TLS mới (nếu môi trường thiếu OpenSSL hiện đại)
ENV DOTNET_SYSTEM_NET_SECURITY_ALLOWLEGACYTLS=false

# Chạy app
ENTRYPOINT ["dotnet", "MovieTicketWebApi.dll"]
