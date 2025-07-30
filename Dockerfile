# ------------ STAGE 1: BUILD ------------
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /src

# Copy solution and project file
COPY MovieTicketWebApi.sln ./
COPY MovieTicketWebApi/MovieTicketWebApi.csproj ./MovieTicketWebApi/

# Restore dependencies
RUN dotnet restore MovieTicketWebApi.sln

# Copy toàn bộ source vào image
COPY . .

# Build và publish ra thư mục riêng
WORKDIR /src/MovieTicketWebApi
RUN dotnet publish -c Release -o /app/publish


# ------------ STAGE 2: RUNTIME ------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS runtime
WORKDIR /app

# Copy app đã build từ stage trước
COPY --from=build /app/publish .

# Biến môi trường để app ASP.NET lắng nghe đúng cổng
ENV ASPNETCORE_URLS=http://+:5000

# Mở cổng cho bên ngoài truy cập vào app
EXPOSE 5000

# Lệnh khởi động ứng dụng
ENTRYPOINT ["dotnet", "MovieTicketWebApi.dll"]
