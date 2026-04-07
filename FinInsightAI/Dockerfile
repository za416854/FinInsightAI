# 1. 使用 .NET 10 SDK 進行編譯 (Build Stage)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 複製專案檔並還原 NuGet 套件
COPY ["FinInsightAI.csproj", "./"]
RUN dotnet restore "FinInsightAI.csproj"

# 複製所有檔案並進行編譯
COPY . .
RUN dotnet publish "FinInsightAI.csproj" -c Release -o /app/publish

# 2. 使用 ASP.NET 10 執行環境 (Run Stage)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 設定應用程式啟動指令
ENTRYPOINT ["dotnet", "FinInsightAI.dll"]