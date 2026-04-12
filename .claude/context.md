This repository contains IntegrationHub, a shared integration platform.

Structure:
- src/Core: platform core
- src/Clients: client-specific modules
- src/Sources: external integrations
- shared: reusable libraries
- tools: standalone tools

Rules:
- Treat SRP, CEP, KSIP, ANPRS and ZW as external sources.
- Do not couple Sources with Clients directly.
- Keep integration logic inside Sources.
- Keep Core independent from specific clients.
- Tools are not part of runtime.
- Avoid external internet dependencies (offline-first environment).
