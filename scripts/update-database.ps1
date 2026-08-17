$ErrorActionPreference = "Stop"
dotnet ef database update `
  --project src/BankingPlatform.Infrastructure `
  --startup-project src/BankingPlatform.Api
