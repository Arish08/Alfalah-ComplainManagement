$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK is not installed or dotnet is not on PATH. Install .NET 10 SDK first."
}
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    throw "Node.js is not installed or node is not on PATH. Install Node.js 22 LTS (recommended)."
}
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "npm is not available on PATH."
}

if (Test-Path "BankingPlatform.sln") { Remove-Item "BankingPlatform.sln" -Force }
dotnet new sln -n BankingPlatform

dotnet sln BankingPlatform.sln add src/BankingPlatform.Domain/BankingPlatform.Domain.csproj
dotnet sln BankingPlatform.sln add src/BankingPlatform.Application/BankingPlatform.Application.csproj
dotnet sln BankingPlatform.sln add src/BankingPlatform.Infrastructure/BankingPlatform.Infrastructure.csproj
dotnet sln BankingPlatform.sln add src/BankingPlatform.Api/BankingPlatform.Api.csproj
dotnet sln BankingPlatform.sln add src/BankingPlatform.WorkflowHost/BankingPlatform.WorkflowHost.csproj

dotnet restore BankingPlatform.sln

Push-Location frontend
npm install
Pop-Location

Write-Host "Backend restore and frontend npm install completed." -ForegroundColor Green
Write-Host "Next: configure SQL connection string, create the InitialBusinessSchema migration, then update the database." -ForegroundColor Yellow
