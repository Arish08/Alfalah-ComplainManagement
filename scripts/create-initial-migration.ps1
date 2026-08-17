$ErrorActionPreference = "Stop"
dotnet ef migrations add InitialBusinessSchema `
  --project src/BankingPlatform.Infrastructure `
  --startup-project src/BankingPlatform.Api `
  --output-dir Persistence/Migrations
