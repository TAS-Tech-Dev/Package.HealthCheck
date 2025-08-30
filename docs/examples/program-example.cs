using Package.HealthCheck.Core;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DE HEALTH CHECKS COM API FLUENTE
// ============================================================================

// Configuração básica com nome do serviço
builder.Services
    .AddMegaWishHealthChecksBuilder("UserManagementService")
    
    // Bancos de dados (dados sensíveis via código)
    .AddPostgres(
        Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") 
        ?? throw new InvalidOperationException("POSTGRES_CONNECTION not configured"),
        name: "user-database",
        tags: new[] { "database", "critical", "ready" }
    )
    .AddRedis(
        Environment.GetEnvironmentVariable("REDIS_CONNECTION") 
        ?? throw new InvalidOperationException("REDIS_CONNECTION not configured"),
        name: "user-cache",
        tags: new[] { "cache", "critical", "ready" }
    )
    .AddRabbitMq(
        Environment.GetEnvironmentVariable("RABBITMQ_CONNECTION") 
        ?? throw new InvalidOperationException("RABBITMQ_CONNECTION not configured"),
        name: "message-queue",
        tags: new[] { "queue", "critical", "ready" }
    )
    
    // Dependências HTTP (dados sensíveis via código)
    .AddHttpDependency(
        "notification-service",
        Environment.GetEnvironmentVariable("NOTIFICATION_SERVICE_URL") + "/health",
        critical: false,
        timeoutSeconds: 3,
        tags: new[] { "external", "notification" }
    )
    .AddHttpDependency(
        "payment-gateway",
        Environment.GetEnvironmentVariable("PAYMENT_GATEWAY_URL") + "/health",
        critical: true,
        timeoutSeconds: 5,
        tags: new[] { "external", "payment", "critical" }
    )
    
    // Service Mesh (se configurado)
    .AddServiceMesh(
        Environment.GetEnvironmentVariable("ISTIO_URL"),
        "Istio",
        "user-service",
        timeoutSeconds: 30,
        apiKey: Environment.GetEnvironmentVariable("ISTIO_API_KEY")
    )
    
    // Análise preditiva com ML
    .AddPredictiveAnalysis(
        analysisWindowHours: 48,
        analysisIntervalMinutes: 15,
        degradationThreshold: 0.25,
        criticalThreshold: 0.75
    )
    
    // Dashboard integrado
    .AddDashboard(
        route: "/health-ui",
        enableAutoRefresh: true,
        refreshIntervalSeconds: 15
    )
    
    // Funcionalidades avançadas
    .EnableAutoDiscovery()
    .EnableStartupProbe()
    
    // Finalizar configuração
    .Build();

// ============================================================================
// CONFIGURAÇÕES ADICIONAIS
// ============================================================================

// Configurações não sensíveis podem vir do appsettings.yaml
// HealthCheck:
//   Dashboard:
//     Enabled: true
//     Route: "/health-dashboard"
//   PredictiveAnalysis:
//     Enabled: true
//     AnalysisWindowHours: 24

// ============================================================================
// CONSTRUÇÃO DA APLICAÇÃO
// ============================================================================

var app = builder.Build();

// ============================================================================
// MAPEAMENTO DE ENDPOINTS DE HEALTH
// ============================================================================

// Health check básico
app.MapHealthChecks("/health");

// Liveness probe (Kubernetes) - sempre retorna saudável
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness probe (Kubernetes) - verifica dependências críticas
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

// Startup probe (Kubernetes) - verifica inicialização
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("startup")
});

// Health check detalhado (protegido por API key se configurado)
app.MapHealthChecks("/health/details", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                tags = e.Value.Tags,
                duration = e.Value.Duration
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

// ============================================================================
// MIDDLEWARE ADICIONAL
// ============================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

// ============================================================================
// EXECUÇÃO
// ============================================================================

Console.WriteLine("🚀 UserManagementService iniciando...");
Console.WriteLine("📊 Health checks disponíveis em:");
Console.WriteLine("   • /health - Status geral");
Console.WriteLine("   • /health/live - Liveness probe");
Console.WriteLine("   • /health/ready - Readiness probe");
Console.WriteLine("   • /health/startup - Startup probe");
Console.WriteLine("   • /health/details - Detalhes completos");
Console.WriteLine("   • /health-ui - Dashboard integrado");

app.Run();
