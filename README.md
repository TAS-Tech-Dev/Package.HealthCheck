# Package.HealthCheck

Documentação centralizada em `docs/README.md`.

- Solution: `Package.HealthCheck.sln` (inclui `src/Package.HealthCheck` e `tests/Package.HealthCheck.Tests`)
- Projeto: `src/Package.HealthCheck`
- Testes: `tests/Package.HealthCheck.Tests`
- Endpoints: `/health/live`, `/health/ready`, `/health/startup`, `/health/details`

Para rodar testes com cobertura (quando o SDK estiver disponível):

```bash
dotnet test Package.HealthCheck.sln --collect:"XPlat Code Coverage" --settings tests/Package.HealthCheck.Tests/coverage.runsettings
```
