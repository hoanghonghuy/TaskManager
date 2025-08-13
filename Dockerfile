# Stage 1: Build the application
# Sử dụng base image .NET SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file solution và các file dự án
COPY TaskManager.sln .
COPY TaskManager.Data/*.csproj ./TaskManager.Data/
COPY TaskManager.Web/*.csproj ./TaskManager.Web/

# Restore dependencies cho toàn bộ solution
RUN dotnet restore

# Sao chép toàn bộ source code
COPY . .

# Build và publish ứng dụng
WORKDIR /src/TaskManager.Web
RUN dotnet publish -c release -o /app

# Stage 2: Run the application
# Sử dụng runtime image .NET để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

# Expose port để ứng dụng có thể nhận request
EXPOSE 80

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "TaskManager.Web.dll"]