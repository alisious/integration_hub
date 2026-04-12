# IntegrationHub

IntegrationHub is a shared integration platform used to connect multiple external systems (e.g. SRP, CEP, KSIP, ANPRS, ZW) with internal applications (e.g. PIESP).

## Structure

- `src/Core` – core platform logic (API, Application, Domain, Infrastructure)
- `src/Clients` – client-specific modules (e.g. PIESP)
- `src/Sources` – external system integrations (SRP, CEP, KSIP, ANPRS, ZW)
- `shared` – shared libraries (CSV, Excel, Horkos, etc.)
- `tools` – standalone tools and data sync utilities

## Build

```bash
dotnet restore .\IntegrationHub.sln
dotnet build .\IntegrationHub.sln