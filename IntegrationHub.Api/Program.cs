using IntegrationHub.Api.Middleware;
using IntegrationHub.Application.ANPRS;
using IntegrationHub.Application.RequestValidators.ZW;
using IntegrationHub.Application.ZW;
using IntegrationHub.Common.Config;
using IntegrationHub.Common.Interfaces;
using IntegrationHub.Common.Providers;
using IntegrationHub.Common.RequestValidation;
using IntegrationHub.Domain.Contracts.ZW;
using IntegrationHub.Infrastructure.Audit;
using IntegrationHub.Infrastructure.Cepik;
using IntegrationHub.Infrastructure.Sql;
using IntegrationHub.PIESP.Data;
using IntegrationHub.PIESP.Services;
using IntegrationHub.Sources.ANPRS.Config;
using IntegrationHub.Sources.ANPRS.Extensions;
using IntegrationHub.Sources.CEP.Config;
using IntegrationHub.Sources.CEP.Services;
using IntegrationHub.Sources.CEP.Udostepnianie.Services;
using IntegrationHub.Sources.KSIP.Config;
using IntegrationHub.Sources.KSIP.Services;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore; // Add this using directive for 'UseSqlServer'
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Trentum.Horkos;


// Serilog – bootstrap logger, ¿eby logowaæ od samego pocz¹tku
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// KONFIGURACJA SERILOG – musi byæ przed builder.Build() U¿ywaj konfiguracji Seriloga z appsettings.*.json
builder.Logging.ClearProviders(); // usuñ domyœlnego ConsoleLoggera itp.
builder.Host.UseSerilog((ctx, services, cfg) =>
cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services));

//Konfiguracja CORS – PROD + localhost (dev)
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("PIESP", p =>
        p
        // zezwól na originy z appsettings (PROD)
        .WithOrigins(allowedOrigins)
        // i dodatkowo zezwól na localhost (dev)
        .SetIsOriginAllowed(origin =>
        {
            if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback) return true;
            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        // .AllowCredentials() // w³¹cz TYLKO jeœli faktycznie u¿ywacie cookies/credentials
        .SetPreflightMaxAge(TimeSpan.FromHours(1))
    );
});



// Add services to the container.
// ====== DB CONTEXT ======
builder.Services.AddDbContext<PiespDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IntegrationHubDB")));

//Rejestracja ClientCertificateProvider
builder.Services.AddSingleton<IClientCertificateProvider, ClientCertificateProvider>();

var integrationHubDbConnectionString = builder.Configuration.GetConnectionString("IntegrationHubDB")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:IntegrationHubDB.");
var horkosDbConnectionString = builder.Configuration.GetConnectionString("HorkosDB")
             ?? throw new InvalidOperationException("Missing ConnectionStrings:HorkosDB.");

builder.Services.AddHorkosDapper(horkosDbConnectionString);
builder.Services.AddIntegrationHubSqlInfrastructure(integrationHubDbConnectionString);



/**************************************************************/
// ====== SRP CLIENT ======
// Rejestracja konfiguracji SRP
builder.Services.Configure<SrpConfig>(builder.Configuration.GetSection("ExternalServices:SRP"));
var srpConfig = builder.Configuration.GetSection("ExternalServices:SRP").Get<SrpConfig>();

builder.Services.AddTransient<SrpHttpLoggingHandler>();
builder.Services.AddHttpClient("SrpServiceClient", c =>
{
    // Ca³kowity timeout HttpClient wy³¹czamy – kontrolujemy czas przez pipeline (Attempt/Total)
    c.Timeout = Timeout.InfiniteTimeSpan;

    // Spróbuj HTTP/2 (wenn dostêpny), z fallbackiem w dó³ – lepsze mno¿enie strumieni
    c.DefaultRequestVersion = HttpVersion.Version20;
    c.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
// Twój logger wiadomoœci – zostaje
.AddHttpMessageHandler<SrpHttpLoggingHandler>()

// Handler gniazd – klucz do wydajnoœci równoleg³ych wywo³añ
.ConfigurePrimaryHttpMessageHandler(sp =>
{

    // Uwaga: NIE ³adujemy certyfikatu w trybie testowym
    if (srpConfig?.TestMode == true)
    {
        
        Log.Warning("SRP dzia³a w TRYBIE TESTOWYM. Nie u¿ywam certyfikatu klienta.");
        return new HttpClientHandler();

    }


    var config = sp.GetRequiredService<IOptions<SrpConfig>>().Value;
    var certProvider = sp.GetRequiredService<IClientCertificateProvider>();
    var clientCert = certProvider.GetClientCertificate(config);

    // ZLATA ZASADA: MaxConnectionsPerServer >= 2 * maxParallel (zapas)
    var maxConn = Math.Max(16, (config.HttpMaxConnectionsPerServer ?? 0)); // opcjonalnie z appsettings
    if (maxConn <= 0) maxConn = 32; // domyœlnie pod bulk

    var h = new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        MaxConnectionsPerServer = maxConn,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5), // rotacja po³¹czeñ (DNS/zdrowie)
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        ConnectTimeout = TimeSpan.FromSeconds(5),
        // W .NET 8 dostêpne: utrzymanie po³¹czeñ H2 przy d³u¿szej bezczynnoœci
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
        KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
    };

    h.SslOptions = new SslClientAuthenticationOptions
    {
        ClientCertificates = new X509CertificateCollection { clientCert },
        RemoteCertificateValidationCallback = config.TrustServerCertificate
            ? new RemoteCertificateValidationCallback((_, _, _, _) => true)
            : null
    };

    return h;
})

// Polly v8 – gotowy „standard” z dopracowaniem czasów i progów
.AddStandardResilienceHandler(opt =>
{
    // 1) Timeout pojedynczej próby (wa¿ne przy retrach)
    opt.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

    // 2) £¹czny limit czasu ca³ego ¿¹dania (przez wszystkie retry)
    opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

    // 3) Retry – exponential + jitter (domyœlnie na 5xx/408/transport; 429 te¿ jest sensowny na odczytach)
    opt.Retry.MaxRetryAttempts = 3;

    // 4) Circuit Breaker – ¿eby nie „mêczyæ” us³ugi gdy ewidentnie ma k³opot
    opt.CircuitBreaker.FailureRatio = 0.2;                // 20% pora¿ek w oknie => przerwa
    opt.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
    opt.CircuitBreaker.MinimumThroughput = 20;            // minimalna liczba prób do oceny
    opt.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});


/**************************************************************/
// ====== CEP CLIENT ======
// Rejestracja konfiguracji CEP
builder.Services.Configure<CEPConfig>(builder.Configuration.GetSection("ExternalServices:CEP"));
var cepConfig = builder.Configuration.GetSection("ExternalServices:CEP").Get<CEPConfig>();






// ====== DEPENDENCY INJECTION ======
builder.Services.AddSingleton<SqlAuditSink>();
builder.Services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<SqlAuditSink>());
builder.Services.AddHostedService<SqlAuditSink.Worker>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DutyService>();
builder.Services.AddScoped<SupervisorService>();

builder.Services.AddSingleton<IRequestValidator<WPMRequest>, WPMRequestValidator>();

builder.Services.AddTransient<ISrpSoapInvoker, SrpSoapInvoker>();
builder.Services.AddTransient<ICepSoapInvoker, CepSoapInvoker>();
builder.Services.AddSingleton<ISourceCallAuditor, SourceCallAuditor>();
builder.Services.AddScoped<IANPRSDictionaryFacade, ANPRSDictionaryFacade>();
builder.Services.AddScoped<IANPRSReportsFacade, ANPRSReportsFacade>();
builder.Services.AddScoped<IANPRSSourceFacade, ANPRSSourceFacade>();
builder.Services.AddScoped<IANPRSReportWriter, ANPRSReportWriter>();
builder.Services.AddScoped<IZWSourceFacade, ZWSourceFacade>();

if (srpConfig!.TestMode)
{
    
    // Jeœli TestMode, zarejestruj PeselService jako PeselServiceTest
    builder.Services.AddTransient<IPeselService, PeselServiceTest>();
    builder.Services.AddTransient<IRdoService, RdoServiceTest>();
    Log.Warning("SRP dzia³a w trybie testowym.");
}
else
{
   builder.Services.AddTransient<IRdoService, RdoService>();
   builder.Services.AddTransient<IPeselService, PeselService>();
   Log.Information("SRP w trybie produkcyjnym.");
}

if (cepConfig!.TestMode)
{
    builder.Services.AddScoped<ICEPSlownikiService, CEPSlownikiServiceTest>();
    builder.Services.AddScoped<ICEPUdostepnianieService, CEPUdostepnianieServiceTest>();
    Log.Warning("CEP dzia³a w trybie testowym.");
}
else
{
    builder.Services.AddScoped<ICEPSlownikiService, CEPSlownikiService>();
    builder.Services.AddScoped<ICEPUdostepnianieService, CEPUdostepnianieService>();
    Log.Information("CEP dzia³a w trybie produkcyjnym.");
}

/**************************************************************/
// ====== KSIP CLIENT ======
// Rejestracja konfiguracji KSIP
builder.Services.Configure<KSIPConfig>(builder.Configuration.GetSection("ExternalServices:KSIP"));
var ksipConfig = builder.Configuration.GetSection("ExternalServices:KSIP").Get<KSIPConfig>();
switch (ksipConfig.SourceMode)
{
    case SourceMode.Test:
        Log.Warning("KSIP dzia³a w trybie testowym.");
        builder.Services.AddTransient<IKSIPService, KSIPService>();
        break;
    case SourceMode.Production:
        Log.Information("KSIP dzia³a w trybie produkcyjnym.");
        builder.Services.AddTransient<IKSIPService, KSIPService>();
        break;
    case SourceMode.Development:
        Log.Information("KSIP dzia³a w trybie deweloperskim bez po³¹czenia ze Ÿród³em.");
        break;
    default:
        Log.Warning("KSIP dzia³a bez okreœlonego trybu (brak konfiguracji).");
        break;
}

/**************************************************************/
// ====== ANPRS CLIENT ======
builder.Services.AddANPRS(builder.Configuration,out var logMessage);
Log.Information(logMessage);


// ====== CONTROLLERS ======
builder.Services.AddControllers();

// ====== AUTHENTICATION (JWT + revocation via jti) ======
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var auth = ctx.HttpContext.RequestServices.GetRequiredService<AuthService>();

                // 1) logout pojedynczego tokenu (JTI blacklist)
                var jti = ctx.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(jti) || await auth.IsTokenRevokedAsync(jti))
                { ctx.Fail("Token revoked or missing jti."); return; }

                // 2) wersjonowanie tokenu (force-logout / zmiana ról)
                var uidStr = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var verStr = ctx.Principal?.FindFirst("ver")?.Value;
                if (!Guid.TryParse(uidStr, out var uid) || !int.TryParse(verStr, out var ver))
                { ctx.Fail("Missing user id or token version."); return; }

                if (await auth.IsTokenVersionStaleAsync(uid, ver))
                { ctx.Fail("Token version is stale."); return; }
            }
        };
    });



// ====== SWAGGER ======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Integration Hub API",
        Version = "v1",
        Description = @"System API dla aplikacji patrolowej ¯andarmerii Wojskowej.

    W bazie danych znajduj¹ siê:

    - **U¿ytkownicy**: 'kpr. Jan Kowalski' (badge: 1111, PIN: 1111), 'mjr Tomasz Nowak' (badge: 2222, PIN: 2222), z przypisanymi rolami takimi jak `User` i `Supervisor`.
    - **PIN-y**: przechowywane jako hashe, logowanie wymaga numeru odznaki i PIN-u.
    - **S³u¿by**: przypisane do u¿ytkownika 1111, typy: 'Patrol pieszy', 'Patrol zapobiegawczy', 'Kontrola ruchu', 'Zabezpieczenie wydarzenia', w dniach od 18 do 21.06.2025 r.
    - **Kody bezpieczeñstwa**: generowane na 10 minut w celu resetu PIN-u.
    - **Role**: `User`, `Supervisor`, `PowerUser`.

    **Autoryzacja**: Wymagany token JWT przesy³any w nag³ówku:
    **Autoryzacja JWT – Jak korzystaæ w Swaggerze:**

    1. Wywo³aj endpoint POST /piesp/auth/login z treœci¹:

       badgeNumber: 1111
       pin: 1111

       OdpowiedŸ zawiera token JWT (w polu ''token'').

    2. Kliknij przycisk Authorize (k³ódka w prawym górnym rogu).
    3. Wklej token w formacie:

       Bearer [wklej_token_tutaj]

    4. Kliknij Authorize, a nastêpnie Close.
    5. Teraz mo¿esz wywo³ywaæ wszystkie zabezpieczone endpointy.

    Token JWT jest wa¿ny 1 dzieñ. Po jego wygaœniêciu zaloguj siê ponownie, aby uzyskaæ nowy token.

    Zalecane testowe dane logowania:

    - Badge: 1111, PIN: 1111 (Dowódca patrolu)
    - Badge: 2222, PIN: 2222 (Oficer dy¿urny)"
    });
    options.MapType<IFormFile>(() => new OpenApiSchema 
    { 
        Type = "string", 
        Format = "binary" 
    });
    options.MapType<IFormFileCollection>(() => new OpenApiSchema
    {
        Type = "array",
        Items = new OpenApiSchema { Type = "string", Format = "binary" }
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "WprowadŸ JWT w formacie 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    options.EnableAnnotations();// ¿eby dzia³a³ [SwaggerOperation] itd.
    options.ExampleFilters();// ¿eby dzia³a³y [SwaggerResponseExample]
});

// Wskazanie assembly, w którym s¹ klasy przyk³adów wyników dzia³ania metod konrolerów:
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.SRP.SearchPerson200Example>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.PIESP.Login401Example>();
builder.Services.AddSwaggerExamplesFromAssemblyOf<
    IntegrationHub.Api.Swagger.Examples.ANPRS.Code400InvalidParameterExample>();

// ====== BUILD ======
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // opcjonalnie dodaj znane proxy/sieci:
    // KnownProxies = { IPAddress.Parse("10.0.0.1") }
});

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            

            using var scope = app.Services.CreateScope();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("SrpServiceClient");

            var rdoUrl = cfg["ExternalServices:SRP:RdoShareServiceUrl"];
            if (string.IsNullOrWhiteSpace(rdoUrl)) return;

            var warmupEnabled = cfg.GetValue<bool?>("ExternalServices:SRP:WarmUpEnabled") ?? false;
            if (!warmupEnabled) return; // <-- wy³¹cza warm-up


            // równoleg³oœæ ~ po³owa MaxConnectionsPerServer (bezpiecznie dla dziesi¹tek userów)
            var maxConns = cfg.GetValue<int?>("ExternalServices:SRP:HttpMaxConnectionsPerServer") ?? 32;
            var parallelism = Math.Clamp(maxConns / 2, 4, 32);
            var attempts = 2; // po 2 lekkie strza³y

            using var sem = new SemaphoreSlim(parallelism);
            var tasks = new List<Task>();

            for (int i = 0; i < attempts; i++)
            {
                await sem.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        foreach (var candidate in new[] { $"{rdoUrl}?wsdl", rdoUrl })
                        {
                            try
                            {
                                using var req = new HttpRequestMessage(HttpMethod.Get, candidate)
                                {
                                    Version = HttpVersion.Version20,
                                    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
                                };
                                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                                break; // handshake/ALPN/H2 i po³¹czenia w puli s¹ gotowe
                            }
                            catch { /* spróbuj kolejny candidate */ }
                        }
                    }
                    finally { sem.Release(); }
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch { /* warm-up nie mo¿e blokowaæ startu */ }
    });
});



// ====== MIDDLEWARE ======
app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ApiAuditMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("PIESP");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging(); // przed MapControllers
app.MapControllers();

// ====== OPTIONAL: DATABASE SEEDING ======
//if (app.Environment.IsDevelopment())
//{
    
//    using (var scope = app.Services.CreateScope())
//    {
//        var context = scope.ServiceProvider.GetRequiredService<PiespDbContext>();
//        //It checks if the Users table is empty, and if so, it adds two users with roles.
//        if (!context.Users.Any()) // Only seed if no users exist
//        {
//            context.Database.EnsureCreated(); // Ensure database is created
//            DbInitializer.Seed(context);
//        }
//    }
//}
//else
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var context = scope.ServiceProvider.GetRequiredService<PiespDbContext>();
//        context.Database.Migrate(); // Apply migrations in production
//    }
//}


// ====== RUN APPLICATION ======
app.Run();




